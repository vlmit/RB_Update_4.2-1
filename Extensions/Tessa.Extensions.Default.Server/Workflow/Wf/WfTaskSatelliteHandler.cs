#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB;
using Tessa.Cards;
using Tessa.Cards.ComponentModel;
using Tessa.Cards.Extensions;
using Tessa.Cards.Extensions.Templates;
using Tessa.Cards.Normalization;
using Tessa.Extensions.Default.Server.Files;
using Tessa.Extensions.Default.Server.Workflow.KrPermissions;
using Tessa.Extensions.Default.Shared;
using Tessa.Extensions.Default.Shared.Workflow.KrPermissions;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Extensions.Default.Shared.Workflow.Wf;
using Tessa.Extensions.Platform.Server.Cards.Satellites.Handlers;
using Tessa.Platform.Data;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.Workflow.Wf
{
    /// <summary>
    /// Обработчик сателлита для задания процесса "Типовая задача".
    /// </summary>
    public sealed class WfTaskSatelliteHandler : TaskSatelliteHandlerBase
    {
        #region Fields

        private readonly IKrTypesCache krTypesCache;
        private readonly IKrPermissionsManager permissionsManager;
        private readonly ICardRepository cardRepository;

        private static readonly string[] documentCommonInfoFields =
        {
            "DocTypeID",
            "DocTypeTitle",
            "Number",
            "FullNumber",
            "Sequence",
            "Subject",
            "DocDate",
            "CreationDate",
            "AuthorID",
            "AuthorName",
            "RegistratorID",
            "RegistratorName",
        };

        #endregion

        #region Constructors

        public WfTaskSatelliteHandler(
            IKrTypesCache krTypesCache,
            IKrPermissionsManager permissionsManager,
            ICardGetStrategy cardGetStrategy,
            ICardTaskAccessProvider cardTaskAccessProvider,
            ICardRepository cardRepository,
            ICardNormalizationService cardNormalizationService)
            : base(cardGetStrategy, cardTaskAccessProvider, cardNormalizationService)
        {
            this.krTypesCache = NotNullOrThrow(krTypesCache);
            this.permissionsManager = NotNullOrThrow(permissionsManager);
            this.cardRepository = NotNullOrThrow(cardRepository);
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        protected override bool LoadMainCardFiles => true;

        /// <inheritdoc/>
        public override ValueTask<bool> IsMainCardTypeAsync(
            CardType mainCardType,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(mainCardType);

            return WfHelper.TypeSupportsWorkflowAsync(this.krTypesCache, mainCardType, cancellationToken);
        }

        /// <inheritdoc/>
        public override async ValueTask CheckFileAccessAsync(
            ICardGetFileContentExtensionContext context,
            ISatelliteHandlerContext satelliteContext)
        {
            ThrowIfNull(context);
            ThrowIfNull(satelliteContext);

            try
            {
                // проверяем доступ по основной карточке
                await KrFileAccessHelper.CheckAccessAsync(
                    context.Request,
                    context,
                    satelliteContext.MainCardID,
                    this.permissionsManager,
                    context.CancellationToken);
            }
            finally
            {
                // чтобы типовое расширение на проверку прав не проверяло токен не от своей карточки
                KrToken.Remove(context.Request.Info);
            }
        }

        /// <inheritdoc/>
        public override async ValueTask CheckFileVersionsAccessAsync(
            ICardGetFileVersionsExtensionContext context,
            ISatelliteHandlerContext satelliteContext)
        {
            ThrowIfNull(context);
            ThrowIfNull(satelliteContext);

            try
            {
                // проверяем доступ по основной карточке
                await KrFileAccessHelper.CheckAccessAsync(
                    context.Request,
                    context,
                    satelliteContext.MainCardID,
                    this.permissionsManager,
                    context.CancellationToken);
            }
            finally
            {
                // чтобы типовое расширение на проверку прав не проверяло токен не от своей карточки
                KrToken.Remove(context.Request.Info);
            }
        }

        /// <inheritdoc/>
        public override async ValueTask PrepareSatelliteForCreateAsync(ICardGetExtensionContext context, ISatelliteHandlerContext satelliteContext)
        {
            var satellite = await satelliteContext.GetSatelliteAsync(context.ValidationResult, context.CancellationToken);
            if (satellite is not null
                && satelliteContext.TaskRowID.HasValue)
            {
                // Идентификатор отложено создаваемого сателлита генерируем как идентификатора задания.
                satellite.ID = satelliteContext.TaskRowID.Value;
            }
        }

        /// <inheritdoc/>
        public override async ValueTask<Card?> PrepareSatelliteForStoreAsync(ICardStoreExtensionContext context, ISatelliteHandlerContext satelliteContext)
        {
            ThrowIfNull(context);
            ThrowIfNull(satelliteContext);

            var satellite = await satelliteContext.GetSatelliteAsync(context.ValidationResult, context.CancellationToken);
            if (satellite is null)
            {
                return null;
            }

            if (satellite.ID == satelliteContext.TaskRowID)
            {
                // Если у сателлита идентификатор задания, то генерируем ему новый идентификатор
                satellite.ID = Guid.NewGuid();
            }

            return await base.PrepareSatelliteForStoreAsync(context, satelliteContext);
        }

        /// <inheritdoc/>
        public override async ValueTask PrepareSatelliteForGetAsync(
            ICardGetExtensionContext context,
            ISatelliteHandlerContext satelliteContext)
        {
            ThrowIfNull(context);
            ThrowIfNull(satelliteContext);

            await base.PrepareSatelliteForGetAsync(
                context,
                satelliteContext);

            var satellite = await satelliteContext.GetSatelliteAsync(context.ValidationResult, context.CancellationToken);
            if (satellite is null)
            {
                return;
            }

            var mainCard = await satelliteContext.GetMainCardAsync(context.ValidationResult, context.CancellationToken);
            // Если не удалось получить основную карточку, значит что-то пошло не так.
            if (mainCard is null)
            {
                return;
            }

            PrepareSatelliteWithMainCardInfo(
                satellite,
                mainCard);

            var digest = context.Request.TryGetDigest()
                ?? await this.cardRepository.GetDigestAsync(mainCard, cancellationToken: context.CancellationToken);
            satellite.Sections.GetOrAdd("WfTaskCardsVirtual").RawFields["MainCardDigest"] = digest;
            satellite.SetDigest(digest);

            if (context.Request.Info.TryGetValue(KrPermissionsHelper.PermissionsCalculatedMark, out var permissionsCalculated))
            {
                satellite.Info[KrPermissionsHelper.PermissionsCalculatedMark] = permissionsCalculated;
            }

            // права на файлы получаем только в случае, если или задание взято в работу (или отложено),
            // или если это автостартуемое задание "Постановка задачи", или текущий сотрудник является автором задания, но не является исполнителем
            if (WfHelper.CanModifyTaskCard(satellite))
            {
                // есть права на задание: запрещаем удаление карточки и действия для файлов основной карточки
                CardPermissionInfo permissions = satellite.Permissions;
                permissions.SetCardPermissions(CardPermissionFlags.ProhibitDeleteCard);

                ListStorage<CardFile>? files = satellite.TryGetFiles();
                if (files is not null && files.Count > 0)
                {
                    GuidDictionaryStorage<CardPermissionFlags> filePermissions = permissions.FilePermissions;
                    foreach (CardFile file in files)
                    {
                        if (file.ExternalSource is not null)
                        {
                            filePermissions[file.RowID] = CardPermissionFlagValues.ProhibitAllFile;
                        }
                    }
                }
            }
            else
            {
                // нет прав на задание - нет никаких прав
                CardHelper.ProhibitAllPermissions(satellite, removeOtherPermissions: true);
            }
        }

        /// <inheritdoc/>
        protected override async ValueTask<IEnumerable<(Guid cardID, Guid typeID)>> GetExternalFileSourcesAsync(
            ICardGetExtensionContext context,
            ISatelliteHandlerContext satelliteContext)
        {
            if (!satelliteContext.TaskRowID.HasValue)
            {
                return Array.Empty<(Guid, Guid)>();
            }

            context.DbScope!.Db
                .SetCommand(
                    context.DbScope.BuilderFactory
                        .With("TasksCTE", b => b
                            .Select().C("th", "ParentRowID")
                            .From("TaskHistory", "th").NoLock()
                            .Where()
                                .C("th", "RowID").Equals().P("CurrentTaskRowID")
                                .And().C("th", "ParentRowID").IsNotNull()
                            .UnionAll()
                            .Select().C("th", "ParentRowID")
                            .From("TasksCTE", "t")
                            .InnerJoin("TaskHistory", "th").NoLock()
                                .On().C("th", "RowID").Equals().C("t", "RowID")
                            .Where().C("th", "ParentRowID").IsNotNull(),
                            columnNames: new[] { "RowID" },
                            recursive: true)
                        .Select().C("wf", "ID")
                        .From("TasksCTE", "t")
                        .InnerJoin(CardSatelliteHelper.SatellitesSectionName, "wf").NoLock()
                            .On().C("wf", CardSatelliteHelper.TaskRowIDColumn).Equals().C("t", "RowID")
                        .Where()
                            .C("wf", CardSatelliteHelper.SatelliteTypeIDColumn).Equals().V(DefaultCardTypes.WfTaskCardTypeID)
                            .And()
                            .Exists(e => e
                                .Select().V(1)
                                .From("Files", "f").NoLock()
                                .Where().C("f", "ID").Equals().C("wf", "ID"))
                        .Build(),
                    context.DbScope.Db.Parameter("CurrentTaskRowID", satelliteContext.TaskRowID.Value, DataType.Guid))
                .LogCommand();

            List<(Guid, Guid)> result = new();
            await using (var reader = await context.DbScope.Db.ExecuteReaderAsync(context.CancellationToken))
            {
                while (await reader.ReadAsync(context.CancellationToken))
                {
                    result.Add((
                        reader.GetGuid(0),
                        DefaultCardTypes.WfTaskCardTypeID));
                }
            }

            return result;
        }

        /// <inheritdoc/>
        protected override ValueTask<bool> IsMainCardFileAsync(
            ICardStoreExtensionContext context,
            ISatelliteHandlerContext satelliteContext,
            CardFile file)
        {
            return new ValueTask<bool>(file.CategoryID == WfHelper.MainCardCategoryID);
        }

        /// <inheritdoc/>
        protected override ValueTask PrepareMainCardFileToStoreAsync(
            ICardStoreExtensionContext context,
            ISatelliteHandlerContext satelliteContext,
            CardFile file)
        {
            // файлы основной карточки будут добавлены с пустой категорией
            file.CategoryID = null;
            file.CategoryCaption = null;
            return ValueTask.CompletedTask;
        }

        /// <inheritdoc/>
        protected override ValueTask SetupSatelliteFileAsync(ICardGetExtensionContext context, ISatelliteHandlerContext satelliteContext, CardFile file, bool isMainCard)
        {
            if (isMainCard)
            {
                file.CategoryID = WfHelper.MainCardCategoryID;
                file.CategoryCaption = WfHelper.MainCardCategoryCaption;
            }

            return ValueTask.CompletedTask;
        }

        #endregion

        #region Private Methods

        private static void PrepareSatelliteWithMainCardInfo(
            Card satellite,
            Card mainCard)
        {
            Dictionary<string, object?> virtualFields = satellite.Sections["WfTaskCardsVirtual"].RawFields;

            StringDictionaryStorage<CardSection>? mainSections = mainCard.TryGetSections();
            if (mainSections is not null)
            {
                if (mainSections.TryGetValue("DocumentCommonInfo", out var mainSection))
                {
                    Dictionary<string, object?> fields = mainSection.RawFields;
                    for (int i = 0; i < documentCommonInfoFields.Length; i++)
                    {
                        virtualFields[documentCommonInfoFields[i]] = fields.TryGet<object>(documentCommonInfoFields[i]);
                    }
                }

                if (mainSections.TryGetValue("KrApprovalCommonInfoVirtual", out mainSection))
                {
                    Dictionary<string, object?> fields = mainSection.RawFields;
                    virtualFields["StateID"] = fields.TryGet<object>("StateID");
                    virtualFields["StateName"] = fields.TryGet<object>("StateName");
                    virtualFields["StateModified"] = fields.TryGet<object>("StateChangedDateTimeUTC");
                }
            }

            // Перекидываем KrToken в карточку сателлита, т.к. знаем, что данная карточка сателита не добавлена в типовое решение
            var cardToken = KrToken.TryGet(mainCard.Info);
            if (cardToken is not null)
            {
                cardToken.Set(satellite.Info);

                if (cardToken.HasPermission(KrPermissionFlagDescriptors.ModifyAllTaskAssignedRoles))
                {
                    foreach (var task in satellite.Tasks)
                    {
                        task.Flags |= CardTaskFlags.CanModifyTaskAssignedRoles;
                    }
                }

                if (cardToken.HasPermission(KrPermissionFlagDescriptors.ModifyOwnTaskAssignedRoles))
                {
                    foreach (var task in satellite.Tasks)
                    {
                        if (task.TaskSessionRoles.Count > 0)
                        {
                            task.Flags |= CardTaskFlags.CanModifyTaskAssignedRoles;
                        }
                    }
                }
            }
        }

        #endregion
    }
}
