using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB;
using Tessa.BusinessCalendar;
using Tessa.Cards;
using Tessa.Cards.ComponentModel;
using Tessa.Extensions.Default.Server.Workflow.KrObjectModel;
using Tessa.Extensions.Default.Server.Workflow.KrProcess;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow.Handlers;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Extensions.Platform.Server.Cards;
using Tessa.Platform;
using Tessa.Platform.Collections;
using Tessa.Platform.Data;
using Tessa.Platform.Runtime;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Unity;

namespace Tessa.Extensions.Default.Server.Workflow.KrCompilers.UserAPI
{
    /// <summary>
    /// Предоставляет статические методы, используемые в скриптах подсистемы маршрутов.
    /// </summary>
    public static class UserAPIHelper
    {
        #region Public Methods

        /// <inheritdoc cref="IKrScript.GetCurrentTaskHistoryGroupAsync"/>
        /// <param name="script"><inheritdoc cref="IKrScript" path="/summary"/></param>
        public static ValueTask<Guid?> GetCurrentTaskHistoryGroupAsync(
            IKrScript script)
        {
            ThrowIfNull(script);

            if (script.Stage is not null
                && HandlerHelper.TryGetOverriddenTaskHistoryGroup(script.Stage, out var overridenTaskHistoryGroupID))
            {
                return new ValueTask<Guid?>(overridenTaskHistoryGroupID);
            }

            return script.KrScope.GetCurrentHistoryGroupAsync(
                script.CardID,
                cancellationToken: script.CancellationToken);
        }

        /// <inheritdoc cref="IKrScript.CardRowsAsync(string)"/>
        /// <param name="script"><inheritdoc cref="IKrScript" path="/summary"/></param>
        public static async ValueTask<ListStorage<CardRow>> CardRowsAsync(
            IKrScript script,
            string sectionName)
        {
            ThrowIfNull(script);
            ThrowIfNullOrWhiteSpace(sectionName);

            return (await script.GetCardObjectAsync()).Sections[sectionName].Rows;
        }

        /// <inheritdoc cref="IKrScript.IsMainProcess"/>
        /// <param name="script"><inheritdoc cref="IKrScript" path="/summary"/></param>
        public static bool IsMainProcess(IKrScript script)
        {
            ThrowIfNull(script);

            return script.ProcessTypeName == KrConstants.KrProcessName;
        }

        /// <inheritdoc cref="IKrScript.IsMainProcessStartedAsync"/>
        /// <param name="script"><inheritdoc cref="IKrScript" path="/summary"/></param>
        public static async ValueTask<bool> IsMainProcessStartedAsync(IKrScript script)
        {
            ThrowIfNull(script);

            if (IsMainProcess(script))
            {
                return true;
            }

            var contextualSatellite = await script.GetContextualSatelliteAsync();

            if (contextualSatellite is null)
            {
                return false;
            }

            return await script.Db.SetCommand(
                    script.DbScope.BuilderFactory
                        .Select().Top(1)
                        .V(1)
                        .From("WorkflowProcesses").NoLock()
                        .Where().C("ID").Equals().P("ID")
                        .And().C("TypeName").Equals().V(KrConstants.KrProcessName)
                        .Limit(1)
                        .Build(),
                    script.Db.Parameter("ID", contextualSatellite.ID, DataType.Guid))
                .LogCommand()
                .ExecuteAsync<bool>(script.CancellationToken);
        }

        /// <inheritdoc cref="IKrScript.IsMainProcessInactiveAsync"/>
        /// <param name="script"><inheritdoc cref="IKrScript" path="/summary"/></param>
        /// <param name="contextualSatellite">Карточка контекстуального сателлита.</param>
        public static async ValueTask<bool> IsMainProcessInactiveAsync(
            IKrScript script,
            Card contextualSatellite)
        {
            ThrowIfNull(script);

            if (script.IsMainProcess())
            {
                return false;
            }

            if (contextualSatellite is not null)
            {
                return contextualSatellite
                    .GetStagesSection()
                    .Rows
                    .All(static p => (p.TryGet<int?>(KrConstants.KrStages.StateID) ?? KrStageState.Inactive.ID) == KrStageState.Inactive);
            }

            var hasAtLeastNonInactiveStage = await script
                .Db
                .SetCommand(script.DbScope.BuilderFactory
                    .Select().Top(1)
                    .V(true)
                    .From(KrConstants.KrStages.Name, "s").NoLock()
                    .InnerJoin(KrConstants.KrApprovalCommonInfo.Name, "aci").NoLock()
                        .On().C("aci", KrConstants.KrApprovalCommonInfo.ID).Equals().C("s", KrConstants.KrStages.ID)
                    .Where()
                        .C("aci", KrConstants.KrProcessCommonInfo.MainCardID).Equals().P("ID")
                        .And().C("s", KrConstants.KrStages.StateID).NotEquals().V(KrStageState.Inactive.ID)
                    .Limit(1)
                    .Build(),
                    script.Db.Parameter("ID", script.CardID, DataType.Guid))
                .LogCommand()
                .ExecuteAsync<bool>(script.CancellationToken);
            return !hasAtLeastNonInactiveStage;
        }

        /// <inheritdoc cref="IKrScript.Resolve{T}(string)"/>
        /// <param name="unityContainer">Контейнер, из которого запрашивается зависимость.</param>
        public static T Resolve<T>(
            IUnityContainer unityContainer,
            string name = null)
        {
            ThrowIfNull(unityContainer);

            return unityContainer.Resolve<T>(name);
        }

        /// <inheritdoc cref="IKrScript.ForEachStage(Action{CardRow}, bool)"/>
        /// <param name="script"><inheritdoc cref="IKrScript" path="/summary"/></param>
        public static void ForEachStage(
            IKrScript script,
            Action<CardRow> rowAction,
            bool withNesteds)
        {
            ThrowIfNull(script);
            ThrowIfNull(rowAction);

            IEnumerable<CardRow> rows = script
                .ProcessHolderSatellite
                .GetStagesSection()
                .Rows;

            if (!withNesteds)
            {
                rows = rows.Where(static p => !p.TryGet<Guid?>(KrConstants.KrStages.NestedProcessID).HasValue);
            }

            foreach (var row in rows)
            {
                rowAction(row);
            }
        }

        /// <inheritdoc cref="IKrScript.ForEachStageInMainProcessAsync(Func{CardRow, Task}, bool)"/>
        /// <param name="script"><inheritdoc cref="IKrScript" path="/summary"/></param>
        public static async Task ForEachStageInMainProcessAsync(
            IKrScript script,
            Func<CardRow, Task> rowActionAsync,
            bool withNesteds)
        {
            ThrowIfNull(script);
            ThrowIfNull(rowActionAsync);

            IEnumerable<CardRow> rows = (await script.GetContextualSatelliteAsync()).GetStagesSection().Rows;
            if (!withNesteds)
            {
                rows = rows.Where(static p => p.TryGet<Guid?>(KrConstants.KrStages.NestedProcessID) is null);
            }

            foreach (var row in rows)
            {
                await rowActionAsync(row);
            }
        }

        /// <inheritdoc cref="IKrScript.SetStageStateAsync(CardRow, KrStageState)"/>
        /// <param name="script"><inheritdoc cref="IKrScript" path="/summary"/></param>
        public static async Task SetStageStateAsync(
            IKrScript script,
            CardRow stage,
            KrStageState stageState)
        {
            ThrowIfNull(script);
            ThrowIfNull(stage);

            var fields = stage.Fields;

            fields["StateID"] = (int) stageState;
            fields["StateName"] = await script.CardMetadata.GetStageStateNameAsync(
                stageState,
                script.CancellationToken);
        }

        /// <inheritdoc cref="IKrScript.GetOrAddStageAsync(string, StageTypeDescriptor, int)"/>
        /// <param name="script"><inheritdoc cref="IKrScript" path="/summary"/></param>
        /// <param name="ignoreManualChanges">Значение <see langword="true"/>, если должны игнорироваться добавленные вручную этапы, иначе - <see langword="false"/>.</param>
        public static ValueTask<Stage> GetOrAddStageAsync(
            IKrScript script,
            string name,
            StageTypeDescriptor descriptor,
            int pos = int.MaxValue,
            bool ignoreManualChanges = false)
        {
            return AddStageInternalAsync(
                script,
                name,
                descriptor,
                pos,
                ignoreManualChanges,
                true,
                nameof(GetOrAddStageAsync));
        }

        /// <inheritdoc cref="IKrScript.AddStageAsync(string, StageTypeDescriptor, int)"/>
        /// <param name="script"><inheritdoc cref="IKrScript" path="/summary"/></param>
        /// <param name="ignoreManualChanges">Значение <see langword="true"/>, если должны игнорироваться добавленные вручную этапы, иначе - <see langword="false"/>.</param>
        public static ValueTask<Stage> AddStageAsync(
            IKrScript script,
            string name,
            StageTypeDescriptor descriptor,
            int pos = int.MaxValue,
            bool ignoreManualChanges = false)
        {
            return AddStageInternalAsync(
                script,
                name,
                descriptor,
                pos,
                ignoreManualChanges,
                false,
                nameof(AddStageAsync));
        }

        /// <inheritdoc cref="IKrScript.RemoveStage(string)"/>
        /// <param name="script"><inheritdoc cref="IKrScript" path="/summary"/></param>
        /// <param name="ignoreManualChanges">Значение <see langword="true"/>, если должны игнорироваться добавленные вручную этапы, иначе - <see langword="false"/>.</param>
        public static bool RemoveStage(
            IKrScript script,
            string name,
            bool ignoreManualChanges = false)
        {
            ThrowIfNull(script);
            ThrowIfNullStagesContainer(script, nameof(RemoveStage));

            var cnt = script.StagesContainer.Stages
                .RemoveAll(stage =>
                    stage.TemplateID == script.TemplateID
                    && !stage.BasedOnTemplateStage
                    && stage.BasedOnTemplate
                    && stage.Name == name
                    && (!stage.RowChanged || ignoreManualChanges));
            return cnt != 0;
        }

        /// <inheritdoc cref="IKrScript.SetSinglePerformer(Guid, string, Stage, bool)"/>
        public static void SetSinglePerformer(
            Guid id,
            string name,
            Stage stage,
            bool ignoreManualChanges = false)
        {
            ThrowIf(id, id == Guid.Empty);
            ThrowIfNull(name);
            ThrowIfNull(stage);

            if (ignoreManualChanges || !stage.RowChanged)
            {
                stage.Performer = new Performer(id, name);
            }
        }

        /// <inheritdoc cref="IKrScript.ResetSinglePerformer(Stage, bool)"/>
        public static void ResetSinglePerformer(
            Stage stage,
            bool ignoreManualChanges = false)
        {
            ThrowIfNull(stage);

            if (ignoreManualChanges || !stage.RowChanged)
            {
                stage.Performer = null;
            }
        }

        /// <inheritdoc cref="IKrScript.AddPerformer(Guid, string, Stage, int, bool)"/>
        public static Performer AddPerformer(
            Guid id,
            string name,
            Stage stage,
            int pos = int.MaxValue,
            bool ignoreManualChanges = false)
        {
            ThrowIf(id, id == Guid.Empty);
            ThrowIfNull(name);
            ThrowIfNull(stage);

            pos = NormalizePos(pos, stage.Performers);

            // Если этап не изменен, то у него есть предок, в котором можно найти исполнителя
            // Если этап изменен, то он не заменяется. Можно просто взять сам этап и посмотреть там.
            var ancestor = !stage.RowChanged
                ? stage.Ancestor
                : stage;

            if (!ignoreManualChanges
                && ancestor?.RowChanged == true)
            {
                return null;
            }

            // В старом этапе может быть такая же роль.
            var oldPerformer = ancestor?.Performers
                ?.FirstOrDefault(p =>
                    p.PerformerID == id
                    && p.PerformerName == name
                    && !p.IsSql);

            var newPerformer = new MultiPerformer(
                oldPerformer?.RowID ?? Guid.NewGuid(),
                id,
                name,
                stage.RowID);

            var offset = Convert.ToInt32(
                oldPerformer is not null
                && stage.Performers.Remove(oldPerformer));

            stage.Performers.Insert(pos - offset, newPerformer);
            return newPerformer;
        }

        /// <summary>
        /// Удаляет исполнителей имеющих указанные идентификаторы.
        /// </summary>
        /// <param name="ids">Коллекция идентификаторов удаляемых исполнителей.</param>
        /// <param name="stage">Этап, из которого удаляются исполнители.</param>
        /// <param name="ignoreManualChanges">Удалить исполнителя, даже если этап изменен пользователем.</param>
        public static void RemovePerformer(
            ICollection<Guid> ids,
            Stage stage,
            bool ignoreManualChanges = false)
        {
            ThrowIfNull(ids);
            ThrowIfNull(stage);

            if (ignoreManualChanges || !stage.RowChanged)
            {
                stage.Performers.RemoveAll(p => ids.Contains(p.RowID));
            }
        }

        /// <summary>
        /// Добавляет запись в текущую группу истории заданий.
        /// </summary>
        /// <param name="script"><inheritdoc cref="IKrScript" path="/summary"/></param>
        /// <param name="taskHistoryGroupID">Идентификатор группы истории заданий.</param>
        /// <param name="typeID">Идентификатор типа задания.</param>
        /// <param name="typeName">Название типа задания.</param>
        /// <param name="typeCaption">Отображаемое название типа задания.</param>
        /// <param name="optionID">Вариант завершения.</param>
        /// <param name="result">Текстовое описание результата завершения задания или <see langword="null"/>, если текстовое описание не доступно.</param>
        /// <param name="performerID">Идентификатор роли автора/роли/исполнителя.</param>
        /// <param name="performerName">Имя роли автора/роли/исполнителя.</param>
        /// <param name="cycle">Опционально: номер цикла. Если не указать, будет взят текущий номер цикла.</param>
        /// <param name="timeZoneID">ID временной зоны.</param>
        /// <param name="timeZoneUtcOffset">Смещение временной зоны.</param>
        /// <param name="modifyActionAsync">Функция для модификации записи истории заданий.</param>
        /// <param name="calendarID">ID календаря</param>
        /// <returns>Асинхронная задача.</returns>
        public static async Task AddTaskHistoryRecordAsync(
            IKrScript script,
            Guid? taskHistoryGroupID,
            Guid typeID,
            string typeName,
            string typeCaption,
            Guid optionID,
            string result = null,
            Guid? performerID = null,
            string performerName = null,
            int? cycle = null,
            int? timeZoneID = null,
            TimeSpan? timeZoneUtcOffset = null,
            Guid? calendarID = null,
            Func<CardTaskHistoryItem, ValueTask> modifyActionAsync = null)
        {
            ThrowIfNull(script);

            var cardMetadata = script.CardMetadata;
            var card = await script.GetCardObjectAsync();

            if (card is null)
            {
                return;
            }

            var contextualSatellite = await script.GetContextualSatelliteAsync();
            var cycleInternal = cycle ?? await script.GetCycleAsync();
            var perfIDInternal = performerID ?? script.Session.User.ID;
            var perfNameInternal = performerName ?? script.Session.User.Name;
            var option = (await cardMetadata.GetEnumerationsAsync(script.CancellationToken)).CompletionOptions[optionID];
            var utcNow = DateTime.UtcNow;

            if (!timeZoneID.HasValue || !timeZoneUtcOffset.HasValue)
            {
                var timeZonesCard = (await script.CardCache.Cards.GetAsync("TimeZones", script.CancellationToken)).GetValue();
                var defaultTimeZoneSection = timeZonesCard.Sections[TimeZonesHelper.DefaultTimeZoneSection];

                timeZoneID = TimeZonesHelper.DefaultZoneID;
                timeZoneUtcOffset = TimeSpan.FromMinutes(defaultTimeZoneSection.Fields.Get<int>("UtcOffsetMinutes"));
            }

            if (!calendarID.HasValue)
            {
                var timeZonesCard = (await script.CardCache.Cards.GetAsync(CardHelper.ServerInstanceTypeName, script.CancellationToken)).GetValue();
                var serverInstancesSction = timeZonesCard.Sections["ServerInstances"];

                calendarID = serverInstancesSction.Fields.Get<Guid>("DefaultCalendarID");
            }

            var item = new CardTaskHistoryItem
            {
                GroupRowID = taskHistoryGroupID,
                State = CardTaskHistoryState.Inserted,
                RowID = Guid.NewGuid(),
                TypeID = typeID,
                TypeName = typeName,
                TypeCaption = typeCaption,
                Created = utcNow,
                Planned = utcNow,
                InProgress = utcNow,
                Completed = utcNow,
                CompletedByID = perfIDInternal,
                CompletedByName = perfNameInternal,
                CompletedByRole = perfNameInternal,
                UserID = perfIDInternal,
                UserName = perfNameInternal,
                AuthorID = perfIDInternal,
                AuthorName = perfNameInternal,
                OptionID = option.ID,
                OptionName = option.Name,
                OptionCaption = option.Caption,
                Result = result ?? string.Empty,
                TimeZoneID = timeZoneID,
                TimeZoneUtcOffsetMinutes = (int?) timeZoneUtcOffset.Value.TotalMinutes,
                CalendarID = calendarID,
                AssignedOnRole = perfNameInternal
            };

            if (modifyActionAsync is not null)
            {
                await modifyActionAsync(item);
            }

            card.TaskHistory.Add(item);
            contextualSatellite.AddToHistory(
                item.RowID,
                cycleInternal > 0 ? cycleInternal : 1);
        }

        /// <summary>
        /// Возвращает группу истории заданий.
        /// </summary>
        /// <param name="script"><inheritdoc cref="IKrScript" path="/summary"/></param>
        /// <param name="groupTypeID">Идентификатор типа группы истории заданий.</param>
        /// <param name="parentGroupTypeID">Тип родительской группы истории заданий.</param>
        /// <param name="newIteration">Явное создание новой итерации.</param>
        /// <returns>Группа истории заданий.</returns>
        public static CardTaskHistoryGroup ResolveTaskHistoryGroup(
            IKrScript script,
            Guid groupTypeID,
            Guid? parentGroupTypeID = null,
            bool newIteration = false)
        {
            ThrowIfNull(script);

            return script.ResolveTaskHistoryGroup(
                groupTypeID,
                parentGroupTypeID,
                newIteration);
        }

        /// <summary>
        /// Возвращает номер текущего цикла.
        /// </summary>
        /// <param name="script"><inheritdoc cref="IKrScript" path="/summary"/></param>
        /// <returns>Номер текущего цикла.</returns>
        /// <remarks>
        /// Является прокси для поля <see cref="KrConstants.Keys.Cycle"/> в <see cref="IKrScript.ProcessInfoStorage"/> основного процесса.
        /// В вторичных процессах каждое обращение вызывает сериализацию/десериализацию состояния основного процесса,
        /// поэтому следует минимизировать обращения к данному методу.
        /// </remarks>
        public static async ValueTask<int> GetCycleAsync(IKrScript script)
        {
            ThrowIfNull(script);

            if (script.ProcessTypeName == KrConstants.KrProcessName)
            {
                // Для основного процесса цикл лежит в его инфо.
                return script.ProcessInfoStorage.TryGet<int?>(KrConstants.Keys.Cycle) ?? 0;
            }

            var serializer = script.StageSerializer;
            return ProcessInfoCacheHelper.Get(
                serializer,
                await script.GetContextualSatelliteAsync())
                ?.TryGet<int?>(KrConstants.Keys.Cycle) ?? 0;
        }

        /// <summary>
        /// Задаёт номер текущего цикла.
        /// </summary>
        /// <param name="script"><inheritdoc cref="IKrScript" path="/summary"/></param>
        /// <param name="newValue">Устанавливаемое значение.</param>
        /// <returns>Значение <see langword="true"/>, если значение было успешно задано, иначе - <see langword="false"/>.</returns>
        /// <remarks>
        /// Является прокси для поля <see cref="KrConstants.Keys.Cycle"/> в <see cref="IKrScript.ProcessInfoStorage"/> основного процесса.
        /// В вторичных процессах каждое обращение вызывает сериализацию/десериализацию состояния основного процесса,
        /// поэтому следует минимизировать обращения к данному методу.
        /// </remarks>
        public static async ValueTask<bool> SetCycleAsync(
            IKrScript script,
            int newValue)
        {
            ThrowIfNull(script);

            if (newValue < 1)
            {
                return false;
            }

            if (script.ProcessTypeName == KrConstants.KrProcessName)
            {
                // Для основного процесса цикл лежит в его инфо.
                script.ProcessInfoStorage[KrConstants.Keys.Cycle] = Int32Boxes.Box(newValue);
                return true;
            }

            var serializer = script.StageSerializer;
            var mainProcessInfo = ProcessInfoCacheHelper.Get(
                serializer,
                await script.GetContextualSatelliteAsync());

            mainProcessInfo[KrConstants.Keys.Cycle] = Int32Boxes.Box(newValue);
            return true;
        }

        /// <summary>
        /// Проверяет, поддерживаются ли указанные компоненты настроек типового решения для текущей карточки.
        /// </summary>
        /// <param name="script"><inheritdoc cref="IKrScript" path="/summary"/></param>
        /// <param name="components">Требуемые компоненты.</param>
        /// <returns>Значение <see langword="true"/>, если все указанные компоненты поддерживаются, иначе - <see langword="false"/>.</returns>
        public static bool HasKrComponents(
            IKrScript script,
            KrComponents[] components)
        {
            ThrowIfNull(script);

            var allComponents = KrComponents.None;
            for (var i = 0; i < components.Length; i++)
            {
                allComponents |= components[i];
            }

            return HasKrComponents(script, allComponents);
        }

        /// <summary>
        /// Проверяет, поддерживаются ли указанные компоненты настроек типового решения для текущей карточки.
        /// </summary>
        /// <param name="script"><inheritdoc cref="IKrScript" path="/summary"/></param>
        /// <param name="components">Требуемые компоненты.</param>
        /// <returns>Значение <see langword="true"/>, если все указанные компоненты поддерживаются, иначе - <see langword="false"/>.</returns>
        public static bool HasKrComponents(
            IKrScript script,
            KrComponents components)
        {
            ThrowIfNull(script);

            return (script.KrComponents & components) == components;
        }

        /// <summary>
        /// Возвращает хранилище Info для основного процесса карточки.
        /// </summary>
        /// <param name="script"><inheritdoc cref="IKrScript" path="/summary"/></param>
        /// <param name="cardID">
        /// Идентификатор карточки, Info основного процесса которой необходимо получить.
        /// Если <see langword="null"/>, тогда используется текущая карточка.
        /// </param>
        /// <returns>Хранилище Info основного процесса.</returns>
        public static async ValueTask<ISerializableObject> GetPrimaryProcessInfoAsync(
            IKrScript script,
            Guid? cardID)
        {
            ThrowIfNull(script);

            var satellite = cardID.HasValue
                ? await script.KrScope.GetKrSatelliteAsync(
                    cardID.Value,
                    validationResult: script.ValidationResult,
                    cancellationToken: script.CancellationToken)
                : await script.GetContextualSatelliteAsync();

            return ProcessInfoCacheHelper.Get(
                script.StageSerializer,
                satellite);
        }

        /// <summary>
        /// Возвращает хранилище Info для вторичного процесса карточки.
        /// </summary>
        /// <param name="script"><inheritdoc cref="IKrScript" path="/summary"/></param>
        /// <param name="secondaryProcessID">Идентификатор вторичного процесса.</param>
        /// <param name="mainCardID">
        /// Идентификатор карточки документа в которой запущен процесс.
        /// Если <see langword="null"/>, тогда используется текущая карточка.
        /// </param>
        /// <returns>Хранилище Info вторичного процесса или значение <see langword="null"/>, если произошла ошибка.</returns>
        public static async ValueTask<ISerializableObject> GetSecondaryProcessInfoAsync(
            IKrScript script,
            Guid secondaryProcessID,
            Guid? mainCardID)
        {
            ThrowIfNull(script);

            Card satellite;
            if (secondaryProcessID == script.ProcessID)
            {
                satellite = script.ProcessHolderSatellite;
            }
            else
            {
                satellite = await script.KrScope.GetSecondaryKrSatelliteAsync(
                    secondaryProcessID,
                    cancellationToken: script.CancellationToken);

                if (satellite is null)
                {
                    return null;
                }

                var satelliteCardID = satellite
                    .GetApprovalInfoSection()
                    .RawFields
                    .TryGet<Guid?>(KrConstants.KrProcessCommonInfo.MainCardID);

                if (satelliteCardID != (mainCardID ?? script.CardID))
                {
                    throw new InvalidOperationException("Secondary satellite has different main card id.");
                }
            }

            return ProcessInfoCacheHelper.Get(script.StageSerializer, satellite);
        }

        /// <summary>
        /// Возвращает карточку из <see cref="Stage.Info"/> этапа, содержащуюся по ключу <see cref="KrConstants.Keys.NewCard"/>.
        /// </summary>
        /// <param name="script"><inheritdoc cref="IKrScript" path="/summary"/></param>
        /// <returns>Карточка, содержащаяся в <see cref="Stage.Info"/> этапа по ключу <see cref="KrConstants.Keys.NewCard"/> или значение <see langword="null"/>, если произошла ошибка.</returns>
        public static ValueTask<Card> GetNewCardAsync(IKrScript script) =>
            GetNewCardAccessStrategy(script).GetCardAsync(withoutTransaction: true);

        /// <summary>
        /// Возвращает стратегию загрузки карточки, получаемой из <see cref="Stage.Info"/> этапа по ключу <see cref="KrConstants.Keys.NewCard"/>.
        /// </summary>
        /// <param name="script"><inheritdoc cref="IKrScript" path="/summary"/></param>
        /// <returns>Стратегия загрузки карточки, получаемой из <see cref="Stage.Info"/> этапа по ключу <see cref="KrConstants.Keys.NewCard"/>.</returns>
        public static IMainCardAccessStrategy GetNewCardAccessStrategy(IKrScript script)
        {
            ThrowIfNull(script);

            return script
                .Stage
                .InfoStorage
                .Get<IMainCardAccessStrategy>(KrConstants.Keys.NewCard);
        }

        /// <summary>
        /// Возвращает хранилище Info ветки вторичного процесса перед стартом.
        /// Актуально только для этапа ветвления.
        /// </summary>
        /// <param name="script"><inheritdoc cref="IKrScript" path="/summary"/></param>
        /// <param name="rowID">
        /// Идентификатор строки RowID в списке вторичных процессов <see cref="KrConstants.KrForkSecondaryProcessesSettingsVirtual.Synthetic"/>.
        /// </param>
        /// <returns>Хранилище Info ветки вторичного процесса перед стартом или значение <see langword="null"/>, если этап не содержит информации по вложенному процессу.</returns>
        public static IDictionary<string, object> GetProcessInfoForBranch(
            IKrScript script,
            Guid rowID)
        {
            ThrowIfNull(script);

            var infos = script
                .StageInfoStorage
                .TryGet<IDictionary<string, object>>(KrConstants.Keys.ForkNestedProcessInfo);

            if (infos is null)
            {
                return null;
            }

            var key = rowID.ToString("D");
            var info = infos.TryGet<IDictionary<string, object>>(key);
            if (info is not null)
            {
                return info;
            }

            info = new Dictionary<string, object>(StringComparer.Ordinal);
            infos[key] = info;
            return info;
        }

        /// <summary>
        /// Подготавливает файлы карточки диалога с временем жизни <see cref="CardTaskDialogStoreMode.Settings"/> к сохранению.
        /// </summary>
        /// <param name="dbScope"><inheritdoc cref="IDbScope" path="/summary"/></param>
        /// <param name="cardRepository"><inheritdoc cref="ICardRepository" path="/summary"/></param>
        /// <param name="mainCardAccessStrategy">Стратегия для доступа к основной карточке.</param>
        /// <param name="dialogCardAccessStrategy">Стратегия для доступа к карточке диалога.</param>
        /// <param name="taskID">Идентификатор задания диалога.</param>
        /// <param name="keepFiles">Значение <see langword="true"/>, если необходимо сохранить файлы после завершения диалога, иначе - <see langword="false"/>.</param>
        /// <param name="session"><inheritdoc cref="ISession" path="/summary"/></param>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Асинхронная задача.</returns>
        public static async ValueTask PrepareFilesInSettingsDialogCardForStoreAsync(
            IDbScope dbScope,
            ICardRepository cardRepository,
            IMainCardAccessStrategy mainCardAccessStrategy,
            IMainCardAccessStrategy dialogCardAccessStrategy,
            Guid taskID,
            bool keepFiles,
            ISession session,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(dbScope);
            ThrowIfNull(cardRepository);
            ThrowIfNull(mainCardAccessStrategy);
            ThrowIfNull(dialogCardAccessStrategy);
            ThrowIfNull(session);
            ThrowIfNull(validationResult);

            if (!dialogCardAccessStrategy.WasUsed)
            {
                return;
            }

            var dialogCard = await dialogCardAccessStrategy.GetCardAsync(
                validationResult,
                cancellationToken: cancellationToken);

            if (!validationResult.IsSuccessful())
            {
                return;
            }

            ICardFileContainer dialogCardFileContainer = null;

            if (dialogCardAccessStrategy.WasFileContainerUsed)
            {
                dialogCardFileContainer = await dialogCardAccessStrategy.GetFileContainerAsync(
                    validationResult,
                    cancellationToken: cancellationToken);

                if (!validationResult.IsSuccessful())
                {
                    return;
                }
            }

            var mainCardFileContainer = await mainCardAccessStrategy.GetFileContainerAsync(
                validationResult,
                cancellationToken: cancellationToken);

            if (!validationResult.IsSuccessful())
            {
                return;
            }

            var satelliteID = await FileSatelliteHelper.GetFileSatelliteIDAsync(
                cardRepository,
                dbScope,
                mainCardFileContainer.Card.ID,
                validationResult,
                true,
                cancellationToken);

            if (!validationResult.IsSuccessful())
            {
                return;
            }

            CardTaskDialogHelper.PrepareFilesInSettingsDialogCardForStore(
                mainCardFileContainer,
                dialogCard,
                dialogCardFileContainer?.FileContainer,
                taskID,
                satelliteID.Value,
                keepFiles,
                session);
        }

        /// <summary>
        /// Подготавливает файлы карточки диалога к сохранению.
        /// </summary>
        /// <param name="coSettings"><inheritdoc cref="CardTaskCompletionOptionSettings" path="/summary"/></param>
        /// <param name="mainCardFileContainer">Контейнер, содержащий информацию по основной карточке и её файлам.</param>
        /// <param name="dialogCardFileContainer">Контейнер, содержащий информацию по карточке диалога и её файлам.</param>
        /// <param name="taskID">Идентификатор задания диалога.</param>
        /// <param name="fileSatelliteID">Идентификатор карточки файлового сателлита, в котором должны быть сохранены файлы из карточки диалога.</param>
        /// <param name="session"><inheritdoc cref="ISession" path="/summary"/></param>
        public static void PrepareFileInDialogCardForStore(
            CardTaskCompletionOptionSettings coSettings,
            ICardFileContainer mainCardFileContainer,
            ICardFileContainer dialogCardFileContainer,
            Guid taskID,
            Guid fileSatelliteID,
            ISession session)
        {
            ThrowIfNull(coSettings);
            ThrowIfNull(mainCardFileContainer);
            ThrowIfNull(dialogCardFileContainer);
            ThrowIfNull(session);

            if (coSettings.StoreMode != CardTaskDialogStoreMode.Settings)
            {
                return;
            }

            CardTaskDialogHelper.PrepareFilesInSettingsDialogCardForStore(
                mainCardFileContainer,
                dialogCardFileContainer.Card,
                dialogCardFileContainer.FileContainer,
                taskID,
                fileSatelliteID,
                coSettings.KeepFiles,
                session);
        }

        #endregion

        #region Private Methods

        private static async ValueTask<Stage> AddStageInternalAsync(
            IKrScript script,
            string name,
            StageTypeDescriptor descriptor,
            int pos,
            bool ignoreManualChanges,
            bool returnOldStage,
            string methodName)
        {
            ThrowIfNull(script);
            ThrowIfNullStagesContainer(script, methodName);
            ThrowIfNullOrEmpty(name);
            ThrowIfNull(descriptor);

            var currentStages = script.CurrentStages;
            pos = NormalizePos(pos, currentStages);

            var oldStage = script.StagesContainer.InitialStages
                .FirstOrDefault(initialStage =>
                    initialStage.TemplateID == script.TemplateID
                    && !initialStage.BasedOnTemplateStage
                    && initialStage.BasedOnTemplate
                    && initialStage.Name == name);

            if (!ignoreManualChanges
                && oldStage is not null
                && (oldStage.RowChanged
                    || oldStage.OrderChanged))
            {
                if (returnOldStage)
                {
                    var copiedStage = new Stage(oldStage);
                    // Чтобы oldStage стал предком для copiedStage
                    copiedStage.Inherit(oldStage);
                    copiedStage.TemplateStageOrder = pos;

                    var index = script.Stages.IndexOf(oldStage);

                    if (index != -1)
                    {
                        script.StagesContainer.ReplaceStage(index, copiedStage);
                    }
                    else
                    {
                        await script.StagesContainer.MergeStagesAsync(
                            new[] { copiedStage },
                            script.CancellationToken);
                    }

                    return copiedStage;
                }

                return null;
            }

            foreach (var stage in script
                .StagesContainer
                .Stages
                .Where(x => x.TemplateID == script.TemplateID)
                .Skip(pos))
            {
                stage.TemplateStageOrder++;
            }

            var newStage = new Stage(
                oldStage?.ID ?? Guid.NewGuid(),
                name,
                descriptor.ID,
                descriptor.Caption,
                script.StageGroupID,
                script.StageGroupName,
                script.StageGroupOrder,
                script.TemplateID,
                script.TemplateName,
                script.Order,
                script.CanChangeOrder,
                script.Position,
                oldStage,
                script.IsStagesReadonly)
            {
                TemplateStageOrder = pos
            };

            var cardNewContext = new CardNewContext(script.CardTypeID, CardNewMode.Default, script.CardMetadata);
            var newSectionRows = await script.Resolve<ICardNewStrategy>().CreateSectionRowsAsync(cardNewContext, script.CancellationToken);
            var emptyRow = newSectionRows[KrConstants.KrStages.Virtual].Clone();

            await script.StageSerializer.FillStageSettingsAsync(
                emptyRow,
                newStage.SettingsStorage,
                script.CancellationToken);

            var serializerSettings = await script.StageSerializer.GetSettingsAsync(script.CancellationToken);

            foreach (var emptySettingsSection in serializerSettings.SettingsSectionNames.Where(p => !newStage.SettingsStorage.ContainsKey(p)))
            {
                newStage.SettingsStorage.Add(emptySettingsSection, Array.Empty<object>());
            }

            await script.StagesContainer.MergeStagesAsync(
                new[] { newStage },
                script.CancellationToken);

            return newStage;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int NormalizePos(
            int pos,
            ICollection collection)
        {
            if (pos < 0)
            {
                pos = 0;
            }
            else if (collection.Count < pos)
            {
                pos = collection.Count;
            }

            return pos;
        }

        private static void ThrowIfNullStagesContainer(
            IKrScript script,
            string methodName)
        {
            if (script.StagesContainer is null)
            {
                throw new InvalidOperationException(
                    $"{methodName} can be invoked only in route calculating context.");
            }
        }

        #endregion
    }
}
