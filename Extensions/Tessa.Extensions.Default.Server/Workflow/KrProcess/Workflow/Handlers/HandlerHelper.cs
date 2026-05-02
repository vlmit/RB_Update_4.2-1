#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Caching;
using Tessa.Extensions.Default.Server.Workflow.KrCompilers;
using Tessa.Extensions.Default.Server.Workflow.KrCompilers.UserAPI;
using Tessa.Extensions.Default.Server.Workflow.KrObjectModel;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Scope;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Serialization;
using Tessa.Extensions.Default.Shared.Workflow;
using Tessa.Extensions.Default.Shared.Workflow.KrCompilers;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform.Data;
using Tessa.Platform.Runtime;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.Roles;
using Unity;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow.Handlers
{
    /// <summary>
    /// Предоставляет вспомогательные методы для обработчиков этапов.
    /// </summary>
    public static class HandlerHelper
    {
        #region Constants And Static Fields

        private const string OverridenTaskHistoryGroup = StorageHelper.SystemKeyPrefix + nameof(OverridenTaskHistoryGroup);

        #endregion

        #region Public Methods

        /// <summary>
        /// Возвращает идентификатор текущей группы истории заданий из <see cref="Stage.InfoStorage"/> указанного этапа.
        /// </summary>
        /// <param name="stage">Этап из которого требуется получить идентификатор текущей группы истории заданий.</param>
        /// <param name="taskHistoryGroupID">Возвращаемое значение. Сохранённый в <see cref="Stage.InfoStorage"/> идентификатор текущей группы истории заданий или значение <see langword="null"/>, если он не найден.</param>
        /// <returns>Значение <see langword="true"/>, если идентификатор текущей группы истории заданий найден в <see cref="Stage.InfoStorage"/>, иначе - <see langword="false"/>.</returns>
        public static bool TryGetOverriddenTaskHistoryGroup(
            [NotNullWhen(true)] Stage? stage,
            [NotNullWhen(true)] out Guid? taskHistoryGroupID)
        {
            if (stage is not null
                && stage.InfoStorage.TryGetValue(OverridenTaskHistoryGroup, out var idObj)
                && idObj is Guid id)
            {
                taskHistoryGroupID = id;
                return true;
            }

            taskHistoryGroupID = null;
            return false;
        }

        /// <summary>
        /// Удаляет из <see cref="Stage.InfoStorage"/> информацию о ранее определённом идентификаторе текущей группы истории заданий.
        /// </summary>
        /// <param name="stage">Этап в котором требуется выполнить удаление информации о ранее определённом идентификаторе текущей группы истории заданий.</param>
        public static void RemoveTaskHistoryGroupOverride(
            Stage stage)
        {
            ThrowIfNull(stage);
            stage.InfoStorage.Remove(OverridenTaskHistoryGroup);
        }

        /// <summary>
        /// Возвращает идентификатор текущей группы истории заданий.
        /// </summary>
        /// <param name="context">Контекст обработчика этапа.</param>
        /// <param name="scope">Объект содержащий информацию по подсистеме маршрутов в текущей области видимости.</param>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Идентификатор текущей группы истории заданий или значение <see langword="null"/>, если при её определении произошла ошибка.</returns>
        public static async ValueTask<Guid?> GetTaskHistoryGroupAsync(
            IStageTypeHandlerContext context,
            IKrScope scope,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(context);
            ThrowIfNull(context.MainCardID);
            ThrowIfNull(context.TaskHistoryResolver);
            ThrowIfNull(scope);
            ThrowIfNull(validationResult);

            var stage = context.Stage;
            if (TryGetOverriddenTaskHistoryGroup(stage, out var overridenID))
            {
                return overridenID;
            }

            var taskHistoryGroupID =
                stage.SettingsStorage.TryGet<Guid?>(
                    KrConstants.KrHistoryManagementStageSettingsVirtual.TaskHistoryGroupTypeID);
            var parentTaskHistoryGroupID =
                stage.SettingsStorage.TryGet<Guid?>(
                    KrConstants.KrHistoryManagementStageSettingsVirtual.ParentTaskHistoryGroupTypeID);
            var newIteration =
                stage.SettingsStorage.TryGet<bool?>(
                    KrConstants.KrHistoryManagementStageSettingsVirtual.NewIteration);
            if (taskHistoryGroupID.HasValue)
            {
                var newGroup = await context.TaskHistoryResolver.ResolveTaskHistoryGroupAsync(
                    taskHistoryGroupID.Value,
                    parentTaskHistoryGroupID,
                    newIteration == true,
                    cancellationToken: cancellationToken);

                if (newGroup is null)
                {
                    return null;
                }

                var newRowID = newGroup.RowID;
                stage.InfoStorage[OverridenTaskHistoryGroup] = newRowID;
                return newRowID;
            }

            return await scope.GetCurrentHistoryGroupAsync(
                context.MainCardID.Value,
                validationResult: validationResult,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Возвращает автора текущего этапа.
        /// </summary>
        /// <param name="context">Контекст обработчика этапа.</param>
        /// <param name="roleGetStrategy">Стратегия для получения информации о ролях.</param>
        /// <param name="contextRoleManager">Обработчик контекстных ролей.</param>
        /// <param name="contextRoleCache"><inheritdoc cref="ICardContextRoleCache" path="/summary"/></param>
        /// <param name="session">Сессия пользователя.</param>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Автор текущего этапа или значение <see langword="null"/>, если его не удалось получить.</returns>
        public static async ValueTask<Author?> GetStageAuthorAsync(
            IStageTypeHandlerContext context,
            IRoleGetStrategy roleGetStrategy,
            IContextRoleManager contextRoleManager,
            ICardContextRoleCache contextRoleCache,
            ISession session,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(context);
            ThrowIfNull(roleGetStrategy);
            ThrowIfNull(contextRoleManager);
            ThrowIfNull(contextRoleCache);
            ThrowIfNull(session);
            ThrowIfNull(validationResult);

            var overridenAuthor = context.Stage.Author;

            if (overridenAuthor is not null)
            {
                var (roleID, roleName) = await WorkflowCommonHelper.TryGetPersonalRoleIDAsync(
                    overridenAuthor.AuthorID,
                    NotNullOrThrow(context.MainCardID),
                    roleGetStrategy,
                    contextRoleManager,
                    contextRoleCache,
                    validationResult,
                    cancellationToken);

                if (roleID.HasValue)
                {
                    return new Author(roleID.Value, roleName);
                }

                validationResult.AddError(
                    nameof(HandlerHelper),
                    "$KrProcess_ErrorMessage_OnlyPersonalAndContextRoles");
                return null;
            }

            var initiator = context.WorkflowProcess.Author;
            if (initiator is not null)
            {
                return initiator;
            }

            return new Author(
                session.User.ID,
                session.User.Name);
        }

        public static (Guid? ID, string? Caption) GetTaskKind(IStageTypeHandlerContext context)
        {
            ThrowIfNull(context);

            var stage = context.Stage;
            var kindID = stage.SettingsStorage.TryGet<Guid?>(KrConstants.KrTaskKindSettingsVirtual.KindID);
            var kindCaption = stage.SettingsStorage.TryGet<string>(KrConstants.KrTaskKindSettingsVirtual.KindCaption);
            return (kindID, kindCaption);
        }

        /// <summary>
        /// Инициализирует указанный объект информацией содержащейся в контексте обработчика этапа.
        /// </summary>
        /// <param name="unityContainer">Unity-контейнер.</param>
        /// <param name="instance">Инициализируемый объект.</param>
        /// <param name="context">Контекст обработчика этапа.</param>
        /// <returns>Асинхронная задача.</returns>
        public static async ValueTask InitScriptContextAsync(
            IUnityContainer unityContainer,
            IKrScript instance,
            IStageTypeHandlerContext context)
        {
            ThrowIfNull(unityContainer);
            ThrowIfNull(instance);
            ThrowIfNull(context);

            var currentStage = context.Stage;
            var processCache = unityContainer.Resolve<IKrProcessCache>();

            instance.MainCardAccessStrategy = context.MainCardAccessStrategy;
            instance.CardID = context.MainCardID ?? Guid.Empty;
            instance.CardType = context.MainCardType;
            instance.DocTypeID = context.MainCardDocTypeID ?? Guid.Empty;
            if (context.KrComponents.HasValue)
            {
                instance.KrComponents = context.KrComponents.Value;
            }

            instance.WorkflowProcessInfo = context.ProcessInfo;
            instance.ProcessID = context.ProcessInfo?.ProcessID;
            instance.ProcessTypeName = context.ProcessInfo?.ProcessTypeName;
            instance.InitiationCause = context.InitiationCause;
            instance.SetContextualSatellite(context.ContextualSatellite);
            instance.ProcessHolderSatellite = context.ProcessHolderSatellite;
            instance.SecondaryProcess = context.SecondaryProcess;
            instance.CardContext = context.CardExtensionContext;
            instance.ValidationResult = context.ValidationResult;
            instance.TaskHistoryResolver = context.TaskHistoryResolver;
            instance.Session = unityContainer.Resolve<ISession>();
            instance.DbScope = unityContainer.Resolve<IDbScope>();
            instance.UnityContainer = unityContainer;
            instance.CardMetadata = unityContainer.Resolve<ICardMetadata>();
            instance.KrScope = unityContainer.Resolve<IKrScope>();
            instance.CardCache = unityContainer.Resolve<ICardCache>();
            instance.KrTypesCache = unityContainer.Resolve<IKrTypesCache>();
            instance.StageSerializer = unityContainer.Resolve<IKrStageSerializer>();

            if (currentStage.TemplateID is null
                || !(await processCache.GetAllStageTemplatesAsync(context.CancellationToken)).TryGetValue(currentStage.TemplateID.Value, out var stageTemplate))
            {
                return;
            }

            instance.StageGroupID = currentStage.StageGroupID;
            instance.StageGroupName = currentStage.StageGroupName;
            instance.StageGroupOrder = currentStage.StageGroupOrder;
            instance.TemplateID = currentStage.TemplateID.Value;
            instance.TemplateName = currentStage.TemplateName;
            instance.Order = stageTemplate?.Order ?? -1;
            instance.Position = stageTemplate?.Position ?? GroupPosition.Unspecified;
            instance.CanChangeOrder = stageTemplate?.CanChangeOrder ?? true;
            instance.IsStagesReadonly = stageTemplate?.IsStagesReadonly ?? true;

            // На данном этапе нет контейнера, способного пересчитывать положения этапов.
            instance.StagesContainer = null;
            instance.WorkflowProcess = context.WorkflowProcess;
            instance.Stage = currentStage;

            // Необходимо сбросить информацию о переключении контекста
            instance.DifferentContextCardID = null;
            instance.DifferentContextWholeCurrentGroup = false;
            instance.DifferentContextProcessInfo = null;
            instance.DifferentContextSetupScriptType = null;

            instance.CancellationToken = context.CancellationToken;
        }

        /// <summary>
        /// Удаляет список завершённых заданий этапа из указанного этапа.
        /// </summary>
        /// <param name="stage">Этап из которого необходимо удалить список завершенных заданий.</param>
        /// <seealso cref="KrConstants.Keys.Tasks"/>
        public static void ClearCompletedTasks(Stage stage)
        {
            ThrowIfNull(stage);

            stage.InfoStorage.Remove(KrConstants.Keys.Tasks);
        }

        /// <summary>
        /// Добавляет информацию о завершённом задании в список завершённых заданий этапа предварительно выполнив подготовку в соответствии со значением свойства этапа <see cref="Stage.WriteTaskFullInformation"/>.
        /// </summary>
        /// <param name="stage">Этап, к которому относится завершенное задание.</param>
        /// <param name="task">Завершенное задание.</param>
        /// <param name="modifyTaskAction">Метод модифицирующий задание, перед его сохранением, после выполнения стандартных действий.</param>
        /// <remarks>
        /// Подготовка задания к сохранению состоит в удаление следующей информации: информации о состояниях по которым можно было бы понять, что задание изменено, <see cref="CardTask.SectionRows"/>, <see cref="CardTask.Card"/>, <see cref="CardInfoStorageObject.Info"/>.<para/>
        /// Оригинальное задание <paramref name="task"/> не изменяется, все операции выполняются над его копией.
        /// </remarks>
        /// <seealso cref="Stage.WriteTaskFullInformation"/>
        /// <seealso cref="KrConstants.Keys.Tasks"/>
        public static void AppendToCompletedTasksWithPreparing(
            Stage stage,
            CardTask task,
            Action<CardTask>? modifyTaskAction = null)
        {
            ThrowIfNull(stage);
            ThrowIfNull(task);

            var taskStorage = StorageHelper.Clone(task.GetStorage());
            var taskCopy = new CardTask(taskStorage);
            taskCopy.RemoveChanges();

            if (!stage.WriteTaskFullInformation)
            {
                taskStorage.Remove(nameof(CardTask.SectionRows));
                taskStorage.Remove(nameof(CardTask.Card));
                taskStorage.Remove(CardInfoStorageObject.InfoKey);
            }

            modifyTaskAction?.Invoke(taskCopy);

            AppendToCompletedTasks(stage, taskCopy);
        }

        /// <summary>
        /// Добавляет информацию о завершённом задании в список завершённых заданий этапа.
        /// </summary>
        /// <param name="stage">Этап, к которому относится завершенное задание.</param>
        /// <param name="task">Завершенное задание.</param>
        /// <seealso cref="KrConstants.Keys.Tasks"/>
        public static void AppendToCompletedTasks(
            Stage stage,
            CardTask task)
        {
            ThrowIfNull(stage);
            ThrowIfNull(task);

            var stageInfoStorage = stage.InfoStorage;
            var list = stageInfoStorage.TryGet<IList>(KrConstants.Keys.Tasks);
            if (list is null)
            {
                list = new List<object>();
                stageInfoStorage[KrConstants.Keys.Tasks] = list;
            }

            list.Add(task.GetStorage());
        }

        /// <summary>
        /// Определяет в текущей группе этапов:
        /// <list type="number">
        /// <item>
        ///     <description>Были ли до текущего этапа не согласованные этапы типов <paramref name="stageTypeIDList"/>. Определяется по наличию в <see cref="Stage.InfoStorage"/> флага <see cref="KrConstants.Keys.Disapproved"/>.</description>
        /// </item>
        /// <item>
        ///     <description>Есть ли этапы типов <paramref name="stageTypeIDList"/> после этого этапа.</description>
        /// </item>
        /// </list>
        /// </summary>
        /// <param name="processStages">Коллекция этапов текущего процесса.</param>
        /// <param name="currentStage">Текущий этап.</param>
        /// <param name="stageTypeIDList">Список идентификаторов типов этапов, для которых выполняется проверка.</param>
        /// <returns>Кортеж &lt;Значение <see langword="true"/>, если до текущего этапа были не согласованные этапы типов <paramref name="stageTypeIDList"/>, иначе - <see langword="false"/>; Значение <see langword="true"/>, если есть этапы типов <paramref name="stageTypeIDList"/> после текущего этапа, иначе - <see langword="false"/>&gt;.</returns>
        public static (bool HasPreviouslyDisapproved, bool HasNext) GetApprovalStagesInfo(
            IList<Stage> processStages,
            Stage currentStage,
            IReadOnlyCollection<Guid> stageTypeIDList)
        {
            ThrowIfNull(processStages);
            ThrowIfNull(currentStage);
            ThrowIfNull(stageTypeIDList);

            if (stageTypeIDList.Count == 0)
            {
                return default;
            }

            var hasPreviouslyDisapprovedClosure = false;
            var hasNextClosure = false;

            // Признак нахождения текущего этапа среди этапов искомых типов в текущей группе этапов.
            var equator = false;
            processStages.ForEachStageInGroup(
                currentStage.StageGroupID,
                iStage =>
                {
                    if (iStage.ID == currentStage.ID)
                    {
                        equator = true;
                        return;
                    }

                    bool? iStageChecked = null;

                    if (!equator
                        && iStage.State == KrStageState.Completed
                        && !(iStage.SettingsStorage.TryGet<bool?>(KrConstants.KrApprovalSettingsVirtual.Advisory) ?? false)
                        && iStage.InfoStorage.TryGet<bool?>(KrConstants.Keys.Disapproved) == true
                        && ((iStageChecked = CheckStage()) == true))
                    {
                        hasPreviouslyDisapprovedClosure = true;
                    }

                    if (!hasNextClosure
                        && equator
                        && (iStageChecked ?? CheckStage()))
                    {
                        hasNextClosure = true;
                    }

                    bool CheckStage()
                    {
                        return iStage.StageTypeID.HasValue
                            && stageTypeIDList.Contains(iStage.StageTypeID.Value);
                    }
                });

            return (hasPreviouslyDisapprovedClosure, hasNextClosure);
        }

        /// <summary>
        /// Обновляет объектную модель и основную информацию по процессу.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IStageTypeHandlerContext" path="/summary"/></param>
        /// <param name="objectModelMapper"><inheritdoc cref="IObjectModelMapper" path="/summary"/></param>
        /// <returns><inheritdoc cref="ValueTask" path="/summary"/></returns>
        public static async ValueTask UpdateWorkflowProcessAsync(
            IStageTypeHandlerContext context,
            IObjectModelMapper objectModelMapper)
        {
            ThrowIfNull(context);
            ThrowIfNull(objectModelMapper);

            await objectModelMapper.UpdateWorkflowProcessAsync(
                context.ProcessInfo!,
                null,
                context.ContextualSatellite!,
                context.ProcessHolderSatellite!,
                context.ProcessHolder,
                context.WorkflowProcess,
                context.CancellationToken);

            if (context.IsProcessHolderCreated)
            {
                context.WorkflowProcess.UpdateInitialWorkflowProcess();
            }

            // Обновление объекта текущего этапа после обновления объектной модели маршрута.
            context.Stage = context.WorkflowProcess.Stages.First(i => i.RowID == context.Stage.RowID);
        }

        #endregion
    }
}
