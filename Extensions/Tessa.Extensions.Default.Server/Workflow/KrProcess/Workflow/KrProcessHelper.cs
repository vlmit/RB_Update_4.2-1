#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Tessa.Cards;
using Tessa.Cards.Workflow;
using Tessa.Extensions.Default.Server.Workflow.KrObjectModel;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Scope;
using Tessa.Extensions.Default.Shared;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform;
using Tessa.Platform.Data;
using Tessa.Platform.Json;
using Tessa.Platform.Storage;
using Tessa.Scheme;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow
{
    public static class KrProcessHelper
    {
        #region Nested Types

        private sealed class SerializerEqualityComparer : IEqualityComparer
        {
            public static readonly IEqualityComparer Instance = new SerializerEqualityComparer();

            private SerializerEqualityComparer()
            {
            }

            /// <inheritdoc />
            bool IEqualityComparer.Equals(
                object? x,
                object? y)
            {
                if (x is WorkflowProcess && y is WorkflowProcess)
                {
                    return ReferenceEquals(x, y);
                }

                return x == y;
            }

            /// <inheritdoc />
            int IEqualityComparer.GetHashCode(
                object obj) => obj.GetHashCode();
        }

        private sealed class ContractResolverWithPrivates : DefaultContractResolver
        {
            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                var prop = base.CreateProperty(member, memberSerialization);

                if (!prop.Writable)
                {
                    var property = member as PropertyInfo;
                    if (property != null)
                    {
                        var hasPrivateSetter = property.GetSetMethod(true) != null;
                        prop.Writable = hasPrivateSetter;
                    }
                }

                return prop;
            }
        }

        #endregion

        #region Fields

        private static readonly ThreadLocal<JsonSerializer> workflowProcessSerializerLocal = new(
            () => TessaSerializer.CreateTyped(settings =>
            {
                settings.EqualityComparer = SerializerEqualityComparer.Instance;
                settings.ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor;
                settings.ContractResolver = new ContractResolverWithPrivates();
            }));

        #endregion

        #region Properties

        /// <summary>
        /// Объект, выполняющий сериализацию <see cref="WorkflowProcess"/> с использованием конвертера <see cref="TypedJsonConverter"/>.
        /// </summary>
        /// <remarks>
        /// Возвращаемый объект безопасно использовать в том потоке, в котором он был получен.
        /// Рекомендуется каждый раз после конструкции <c>await</c> получать значение заново.
        /// </remarks>
        public static JsonSerializer WorkflowProcessSerializer => NotNullOrThrow(workflowProcessSerializerLocal.Value);

        #endregion

        #region Methods

        /// <summary>
        /// Возвращает значение, показывающее, открыта ли транзакция в текущем соединении.
        /// </summary>
        /// <param name="dbScope">Объект для взаимодействия с базой данных.</param>
        /// <returns>Значение <see langword="true"/>, если в текущем соединении открыта транзакция, иначе - <see langword="false"/>.</returns>
        public static bool IsTransactionOpened(IDbScope dbScope)
        {
            try
            {
                return dbScope?.Db?.DataConnection.Transaction is not null;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        /// <summary>
        /// Возвращает значение, показывающее, поддерживает ли карточка маршруты.
        /// </summary>
        /// <param name="card">Проверяемая карточка.</param>
        /// <param name="dbScope">Объект для взаимодействия с базой данных.</param>
        /// <param name="typesCache">Кэш по типам карточек и документов, содержащих информацию по типовому решению.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Значение <see langword="true"/>, если карточка поддерживает маршруты, иначе - <see langword="false"/>.</returns>
        public static async ValueTask<bool> CardSupportsRoutesAsync(
            Card card,
            IDbScope dbScope,
            IKrTypesCache typesCache,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(card);
            ThrowIfNull(dbScope);
            ThrowIfNull(typesCache);

            var cardTypeID = card.TypeID;

            if (cardTypeID == DefaultCardTypes.KrSettingsTypeID)
            {
                return false;
            }

            var krCardType = (await typesCache.GetCardTypesAsync(cancellationToken))
                .FirstOrDefault(p => p.ID == cardTypeID);

            if (krCardType is null)
            {
                return false;
            }

            if (!krCardType.UseDocTypes)
            {
                return krCardType.UseApproving;
            }

            var docTypeIDClosure = await KrProcessSharedHelper.GetDocTypeIDAsync(card, dbScope, cancellationToken);
            var docType = docTypeIDClosure.HasValue
                ? (await typesCache.GetDocTypesAsync(cancellationToken))
                    .FirstOrDefault(p => p.ID == docTypeIDClosure.Value)
                : null;

            return docType?.UseApproving == true;
        }

        /// <summary>
        /// Устанавливает состояние <see cref="KrStageState.Inactive"/> для всех этапов маршрута.
        /// </summary>
        /// <param name="responseWithCard">Объект, содержащий карточку с этапами маршрута.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetStageDefaultValues(CardValueResponseBase responseWithCard)
        {
            ThrowIfNull(responseWithCard);

            var sectionRows = responseWithCard.TryGetSectionRows();
            if (sectionRows is not null
                && sectionRows.TryGetValue(KrConstants.KrStages.Virtual, out var stageRow))
            {
                stageRow[KrConstants.KrStages.StateID] = Int32Boxes.Box(KrStageState.Inactive.ID);
                stageRow[KrConstants.KrStages.StateName] = KrStageState.Inactive.TryGetDefaultName();
            }
        }

        /// <summary>
        /// Устанавливает состояние <see cref="KrStageState.Inactive"/> для всех этапов маршрута.
        /// </summary>
        /// <param name="card">Карточка с этапами маршрута.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetInactiveStateToStages(Card card)
        {
            ThrowIfNull(card);

            if (card.TryGetStagesSection(out var stagesSec))
            {
                foreach (var row in stagesSec.Rows)
                {
                    row[KrConstants.KrStages.StateID] = Int32Boxes.Box(KrStageState.Inactive.ID);
                    row[KrConstants.KrStages.StateName] = KrStageState.Inactive.TryGetDefaultName();
                }
            }
        }

        /// <summary>
        /// Асинхронно проверяет, существует ли карточка по записи в Instances.
        /// </summary>
        /// <param name="cardID">Идентификатор проверяемой карточки.</param>
        /// <param name="dbScope">Объект для взаимодействия с базой данных.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Асинхронная задача.</returns>
        public static async Task<bool> CardExistsAsync(
            Guid cardID,
            IDbScope dbScope,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(dbScope);

            await using (dbScope.Create())
            {
                var db = dbScope.Db;
                return await db
                    .SetCommand(
                        dbScope.BuilderFactory
                            .Select().V(true)
                            .From(Names.Instances).NoLock()
                            .Where().C("ID").Equals().P("ID")
                            .Build(),
                        db.Parameter("ID", cardID))
                    .LogCommand()
                    .ExecuteAsync<bool>(cancellationToken);
            }
        }

        /// <summary>
        /// Возвращает идентификатор основного сателлита <see cref="KrConstants.KrProcessName"/> процесса согласования.
        /// </summary>
        /// <param name="mainCardID">Идентификатор основной карточки.</param>
        /// <param name="dbScope">Объект для взаимодействия с базой данных.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Идентификатор основного сателлита <see cref="KrConstants.KrProcessName"/> процесса согласования или значение по умолчанию для типа, если сателлит не найден.</returns>
        public static async Task<Guid?> GetKrSatelliteIDAsync(
            Guid mainCardID,
            IDbScope dbScope,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(dbScope);

            await using (dbScope.Create())
            {
                var db = dbScope.Db;
                return await db
                    .SetCommand(
                        dbScope.BuilderFactory
                            .Select()
                                .C(KrConstants.KrProcessCommonInfo.ID)
                            .From(KrConstants.KrApprovalCommonInfo.Name).NoLock()
                            .Where().C(KrConstants.KrProcessCommonInfo.MainCardID).Equals().P("id")
                            .Build(),
                        db.Parameter("id", mainCardID))
                    .LogCommand()
                    .ExecuteAsync<Guid?>(cancellationToken);
            }
        }

        /// <summary>
        /// Возвращает список идентификаторов сателлитов вторичных процессов.
        /// </summary>
        /// <param name="mainCardID">Идентификатор основной карточки.</param>
        /// <param name="dbScope">Объект для взаимодействия с базой данных.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Список идентификаторов сателлитов вторичных процессов.</returns>
        public static async Task<List<Guid>> GetSecondarySatellitesIDsAsync(
            Guid mainCardID,
            IDbScope dbScope,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(dbScope);

            await using (dbScope.Create())
            {
                var db = dbScope.Db;
                return await db
                    .SetCommand(
                        dbScope.BuilderFactory
                            .Select()
                            .C(KrConstants.KrProcessCommonInfo.ID)
                            .From(KrConstants.KrSecondaryProcessCommonInfo.Name).NoLock()
                            .Where().C(KrConstants.KrProcessCommonInfo.MainCardID).Equals().P("ID")
                            .Build(),
                        db.Parameter("ID", mainCardID))
                    .LogCommand()
                    .ExecuteListAsync<Guid>(cancellationToken);
            }
        }

        /// <summary>
        /// Возвращает идентификатор типа карточки шаблона.
        /// </summary>
        /// <param name="templateID">Идентификатор карточки шаблона.</param>
        /// <param name="dbScope">Объект для взаимодействия с базой данных.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Идентификатор типа карточки шаблона или значение по умолчанию для типа, если указанный шаблон не найден.</returns>
        public static async Task<Guid?> GetTemplateCardTypeAsync(
            Guid templateID,
            IDbScope dbScope,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(dbScope);

            await using (dbScope.Create())
            {
                var db = dbScope.Db;
                var query = dbScope.BuilderFactory
                    .Select()
                    .C("TypeID")
                    .From("Templates").NoLock()
                    .Where().C("ID").Equals().P("TemplateID")
                    .Build();
                return await db
                    .SetCommand(query, db.Parameter("TemplateID", templateID))
                    .LogCommand()
                    .ExecuteAsync<Guid?>(cancellationToken);
            }
        }

        /// <summary>
        /// Асинхронно возвращает тип документа или тип карточки (если тип документа отсутствует) из карточки шаблона.
        /// </summary>
        /// <param name="templateID">Идентификатор карточки шаблона.</param>
        /// <param name="dbScope">Объект для взаимодействия с базой данных.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Идентификатор типа карточки для которой создан шаблон или идентификатор типа документа, если шаблон найден и карточка для которой создан шаблон его содержит, иначе значение по умолчанию для типа, если шаблон с указанным идентификатором не найден.</returns>
        public static async Task<Guid?> GetTemplateDocTypeAsync(
            Guid templateID,
            IDbScope dbScope,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(dbScope);

            await using (dbScope.Create())
            {
                var db = dbScope.Db;
                db
                    .SetCommand(
                        dbScope.BuilderFactory
                            .Select().C(null, "TypeID", "Card")
                            .From("Templates").NoLock()
                            .Where().C("ID").Equals().P("TemplateID")
                            .Build(),
                        db.Parameter("TemplateID", templateID))
                    .LogCommand();

                Guid cardTypeID;
                string? cardJson;
                await using (var reader = await db.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken))
                {
                    if (!await reader.ReadAsync(cancellationToken))
                    {
                        return null;
                    }

                    cardTypeID = reader.GetGuid(0);
                    cardJson = await reader.GetSequentialNullableStringAsync(1, db.Dbms, cancellationToken);
                }

                var cardStorage = string.IsNullOrEmpty(cardJson)
                    ? new Dictionary<string, object?>(StringComparer.Ordinal)
                    : StorageHelper.DeserializeFromTypedJson(cardJson);

                if (cardStorage is not null)
                {
                    var card = new Card(cardStorage);
                    if (card.TryGetSections() is { } sections
                        && sections.TryGetValue(KrConstants.DocumentCommonInfo.Name, out var dciSec)
                        && dciSec.Fields.TryGetValue(KrConstants.DocumentCommonInfo.DocTypeID, out var docTypeIDObj)
                        && docTypeIDObj is Guid docTypeID)
                    {
                        return docTypeID;
                    }
                }

                return cardTypeID;
            }
        }

        /// <summary>
        /// Сериализует объектную модель процесса.
        /// </summary>
        /// <param name="workflowProcess">Объектная модель процесса.</param>
        /// <returns>Сериализованная в JSON объектная модель процесса.</returns>
        public static string SerializeWorkflowProcess(
            WorkflowProcess workflowProcess) =>
            StorageHelper.SerializeToJson(workflowProcess, WorkflowProcessSerializer);

        /// <summary>
        /// Десериализует объектную модель процесса.
        /// </summary>
        /// <param name="json">Сериализованная в JSON объектная модель процесса.</param>
        /// <returns>Объектная модель процесса.</returns>
        public static WorkflowProcess DeserializeWorkflowProcess(
            string json) =>
            NotNullOrThrow(StorageHelper.DeserializeFromJson<WorkflowProcess>(json, WorkflowProcessSerializer));

        /// <summary>
        /// Возвращает электронную цифровую подпись для объектной модели процесса.
        /// </summary>
        /// <param name="serializedWorkflowProcess">Сериализованная объектная модель процесса.</param>
        /// <param name="cardID">Идентификатор карточки в которой запущен процесс.</param>
        /// <param name="processID">Идентификатор процесса.</param>
        /// <param name="signatureProvider">Объект, предоставляющий криптографические средства для подписания и проверки подписи.</param>
        /// <returns>Электронная цифровая подпись.</returns>
        public static byte[] SignWorkflowProcess(
            string serializedWorkflowProcess,
            Guid? cardID,
            Guid processID,
            ISignatureProvider signatureProvider)
        {
            ThrowIfNullOrEmpty(serializedWorkflowProcess);
            ThrowIfNull(signatureProvider);

            var processBytes = ConcatWorkflowProcessToByteArray(
                serializedWorkflowProcess,
                cardID,
                processID);

            return signatureProvider.Sign(processBytes);
        }

        /// <summary>
        /// Проверяет валидность сериализованной объектной модели процесса.
        /// </summary>
        /// <param name="instance">Информация об экземпляре процесса.</param>
        /// <param name="signatureProvider">Объект, предоставляющий криптографические средства для подписания и проверки подписи.</param>
        /// <returns>Значение <see langword="true"/>, если электронная цифровая подпись является валидной для указанного экземпляра процесса, иначе - <see langword="false"/>.</returns>
        public static bool VerifyWorkflowProcess(
            KrProcessInstance instance,
            ISignatureProvider signatureProvider)
        {
            ThrowIfNull(instance);
            ThrowIfNull(instance.SerializedProcess);
            ThrowIfNull(signatureProvider);

            var processBytes = ConcatWorkflowProcessToByteArray(
                instance.SerializedProcess,
                instance.CardID,
                instance.ProcessID);

            return signatureProvider.Verify(
                processBytes,
                instance.SerializedProcessSignature);
        }

        /// <summary>
        /// Возвращает объект, содержащий информацию по текущему и основному процессу (<see cref="ProcessHolder"/>).
        /// </summary>
        /// <param name="krScope"><inheritdoc cref="IKrScope" path="/summary"/></param>
        /// <param name="mainCardID">Идентификатор карточки документа.</param>
        /// <param name="info"><inheritdoc cref="IWorkflowProcessInfo" path="/summary"/></param>
        /// <param name="startingSecondaryProcess"><inheritdoc cref="StartingSecondaryProcessInfo" path="/summary"/></param>
        /// <param name="contextualSatellite">Контекстуальный сателлит - сателлит основного процесса.</param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Кортеж &lt;Карточка процессного сателлита. Содержит информацию о текущем процессе (для вложенного процесса - это сателлит родительского процесса); Идентификатор по которому можно получить процессный сателлит; Объект, предоставляющий информацию по текущему и основному процессу&gt;.</returns>
        public static async ValueTask<(Card? ProcessHolderSatellite, Guid ProcessHolderID, ProcessHolder? ProcessHolder)> GetProcessHolderAsync(
            IKrScope krScope,
            Guid mainCardID,
            IWorkflowProcessInfo info,
            StartingSecondaryProcessInfo? startingSecondaryProcess,
            Card contextualSatellite,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(krScope);
            ThrowIfNull(info);
            ThrowIfNull(contextualSatellite);

            Card? processHolderSatellite; // Процессный сателлит. Содержит информацию о текущем процессе (для вложенного процесса - это сателлит родительского процесса).

            // Идентификатор, по которому можно получить процессный сателлит.
            // Основной процесс - идентификатор карточки в которой запущен процесс.
            // Вторичный процесс - идентификатор вторичного процесса.
            // Вложенный процесс - если родительский процесс основной, то идентификатор основной карточки, если вторичный - идентификатор вторичного процесса.
            Guid processHolderID;
            ProcessHolder? processHolder;

            switch (info.ProcessTypeName)
            {
                case KrConstants.KrProcessName:
                    processHolderSatellite = contextualSatellite;
                    processHolderID = mainCardID;

                    processHolder = TryGetProcessHolder(
                        krScope,
                        processHolderID);

                    break;
                case KrConstants.KrSecondaryProcessName:
                    processHolderSatellite = await krScope.GetSecondaryKrSatelliteAsync(
                        info.ProcessID,
                        cancellationToken: cancellationToken);

                    processHolderID = info.ProcessID;

                    processHolder = TryGetProcessHolder(
                        krScope,
                        processHolderID);

                    break;
                case KrConstants.KrNestedProcessName:
                    var processHolderIDNullable = GetProcessHolderID(
                        info,
                        startingSecondaryProcess);

                    if (processHolderIDNullable is null)
                    {
                        throw new InvalidOperationException(
                            $"Process holder ID is null for process type {KrConstants.KrNestedProcessName}.");
                    }

                    processHolder = TryGetProcessHolder(
                        krScope,
                        processHolderIDNullable.Value);

                    var mainProcessType = GetMainProcessType(
                        info,
                        processHolder);

                    switch (mainProcessType)
                    {
                        case KrConstants.KrProcessName:
                            processHolderID = mainCardID;
                            processHolderSatellite = contextualSatellite;
                            break;
                        case KrConstants.KrSecondaryProcessName:
                            processHolderID = processHolderIDNullable.Value;

                            processHolderSatellite = await krScope.GetSecondaryKrSatelliteAsync(
                                processHolderID,
                                cancellationToken: cancellationToken);
                            break;
                        default:
                            throw ArgumentOutOfRange(mainProcessType, "Unsupported process type.");
                    }

                    break;
                default:
                    throw ArgumentOutOfRange(info.ProcessTypeName, "Unsupported process type.");
            }

            return (processHolderSatellite, processHolderID, processHolder);

            static ProcessHolder? TryGetProcessHolder(
                IKrScope krScope,
                Guid? processHolderID)
            {
                return processHolderID is null
                    ? null
                    : krScope.GetProcessHolder(processHolderID.Value);
            }
        }

        /// <summary>
        /// Возвращает тип родительского процесса из <paramref name="info"/>.
        /// </summary>
        /// <param name="info"><inheritdoc cref="IWorkflowProcessInfo" path="/summary"/></param>
        /// <param name="holder"><inheritdoc cref="ProcessHolder" path="/summary"/></param>
        /// <returns>Тип родительского процесса.</returns>
        public static string? GetMainProcessType(
            IWorkflowProcessInfo info,
            ProcessHolder? holder = null)
        {
            ThrowIfNull(info);

            if (holder is not null)
            {
                info.ProcessParameters[KrConstants.Keys.MainProcessType] = holder.MainProcessType;
                info.PendingProcessParametersUpdate = true;
                return holder.MainProcessType;
            }

            return info.ProcessParameters.TryGet<string>(KrConstants.Keys.MainProcessType);
        }

        #endregion

        #region Private Methods

        private static byte[] ConcatWorkflowProcessToByteArray(
            string serializedWorkflowProcess,
            Guid? cardID,
            Guid processID)
        {
            var processBytes = Encoding.UTF8.GetBytes(serializedWorkflowProcess);
            var processBytesOriginalSize = processBytes.Length;
            const int guidSize = 16;
            Array.Resize(ref processBytes, processBytesOriginalSize + 2 * guidSize); // Два гуида
            Array.Copy((cardID ?? Guid.Empty).ToByteArray(), 0, processBytes, processBytesOriginalSize, guidSize);
            Array.Copy(processID.ToByteArray(), 0, processBytes, processBytesOriginalSize + guidSize, guidSize);

            return processBytes;
        }

        private static Guid? GetProcessHolderID(
            IWorkflowProcessInfo info,
            StartingSecondaryProcessInfo? startingSecondaryProcess)
        {
            var processHolderID = startingSecondaryProcess?.ProcessHolderID;
            if (processHolderID is not null)
            {
                info.ProcessParameters[KrConstants.Keys.ProcessHolderID] = processHolderID;
                info.PendingProcessParametersUpdate = true;
                return processHolderID;
            }

            return info.ProcessParameters.TryGet<Guid?>(KrConstants.Keys.ProcessHolderID);
        }

        #endregion
    }
}
