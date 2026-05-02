#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Tessa.Cards;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform.Storage;
using Tessa.Scheme;
using Tessa.Workflow;
using Tessa.Workflow.Signals;
using Tessa.Workflow.Storage;

namespace Tessa.Extensions.Default.Shared.Workflow.WorkflowEngine
{
    /// <summary>
    /// Предоставляет константы, используемые при работе с маршрутами в WorkflowEngine.
    /// </summary>
    public static class WorkflowConstants
    {
        #region Constants
        #region Scheme

        #region KrTaskRegistrationAction
        public static class KrTaskRegistrationActionVirtual
        {
            public const string SectionName = nameof(KrTaskRegistrationActionVirtual);

            public const string Author = nameof(Author);
            public const string Performer = nameof(Performer);
            public const string Kind = nameof(Kind);
            public const string Digest = nameof(Digest);
            public const string Period = nameof(Period);
            public const string Planned = nameof(Planned);
            public const string InitTaskScript = nameof(InitTaskScript);
            public const string Result = nameof(Result);
            public const string Notification = nameof(Notification);
            public const string ExcludeDeputies = nameof(ExcludeDeputies);
            public const string ExcludeSubscribers = nameof(ExcludeSubscribers);
            public const string NotificationScript = nameof(NotificationScript);
            public const string CanEditCard = nameof(CanEditCard);
            public const string CanEditAnyFiles = nameof(CanEditAnyFiles);
        }

        public abstract class KrTaskRegistrationActionOptionLinksVirtual
            : ActionOptionLinksBase
        {
            public const string SectionName = nameof(KrTaskRegistrationActionOptionLinksVirtual);
        }

        public abstract class KrTaskRegistrationActionOptionsVirtual
            : ActionSeveralTaskTypesOptionsBase
        {
            public const string SectionName = nameof(KrTaskRegistrationActionOptionsVirtual);

            public const string Link = nameof(Link);
        }

        public static class KrTaskRegistrationActionNotificationRolesVitrual
        {
            public const string SectionName = nameof(KrTaskRegistrationActionNotificationRolesVitrual);

            public const string Role = nameof(Role);
            public const string Option = nameof(Option);
        }
        #endregion

        #region KrApprovalAction
        public static class KrApprovalActionVirtual
        {
            public const string SectionName = nameof(KrApprovalActionVirtual);

            public const string Author = nameof(Author);
            public const string Kind = nameof(Kind);
            public const string Digest = nameof(Digest);
            public const string Period = nameof(Period);
            public const string Planned = nameof(Planned);
            public const string InitTaskScript = nameof(InitTaskScript);
            public const string Result = nameof(Result);
            public const string IsParallel = nameof(IsParallel);
            public const string ReturnWhenApproved = nameof(ReturnWhenApproved);
            public const string CanEditCard = nameof(CanEditCard);
            public const string CanEditAnyFiles = nameof(CanEditAnyFiles);
            public const string ChangeStateOnStart = nameof(ChangeStateOnStart);
            public const string ChangeStateOnEnd = nameof(ChangeStateOnEnd);
            public const string IsAdvisory = nameof(IsAdvisory);
            public const string Notification = nameof(Notification);
            public const string ExcludeDeputies = nameof(ExcludeDeputies);
            public const string ExcludeSubscribers = nameof(ExcludeSubscribers);
            public const string NotificationScript = nameof(NotificationScript);
            public const string SqlPerformersScript = nameof(SqlPerformersScript);
            public const string IsDisableAutoApproval = nameof(IsDisableAutoApproval);
            public const string ExpectAllApprovers = nameof(ExpectAllApprovers);
            public const string NotCreateReturnEditTaskHistoryRecord = nameof(NotCreateReturnEditTaskHistoryRecord);
        }

        public static class KrWeEditInterjectOptionsVirtual
        {
            public const string SectionName = nameof(KrWeEditInterjectOptionsVirtual);

            public const string Role = nameof(Role);
            public const string Author = nameof(Author);
            public const string Kind = nameof(Kind);
            public const string Digest = nameof(Digest);
            public const string Period = nameof(Period);
            public const string Planned = nameof(Planned);
            public const string Notification = nameof(Notification);
            public const string ExcludeDeputies = nameof(ExcludeDeputies);
            public const string ExcludeSubscribers = nameof(ExcludeSubscribers);
            public const string NotificationScript = nameof(NotificationScript);
            public const string InitTaskScript = nameof(InitTaskScript);
        }

        public static class KrWeAdditionalApprovalOptionsVirtual
        {
            public const string SectionName = nameof(KrWeAdditionalApprovalOptionsVirtual);

            public const string Notification = nameof(Notification);
            public const string ExcludeDeputies = nameof(ExcludeDeputies);
            public const string ExcludeSubscribers = nameof(ExcludeSubscribers);
            public const string NotificationScript = nameof(NotificationScript);
            public const string InitTaskScript = nameof(InitTaskScript);
        }

        public static class KrWeRequestCommentOptionsVirtual
        {
            public const string SectionName = nameof(KrWeRequestCommentOptionsVirtual);

            public const string Notification = nameof(Notification);
            public const string ExcludeDeputies = nameof(ExcludeDeputies);
            public const string ExcludeSubscribers = nameof(ExcludeSubscribers);
            public const string NotificationScript = nameof(NotificationScript);
            public const string InitTaskScript = nameof(InitTaskScript);
        }

        public abstract class KrApprovalActionOptionsVirtual
            : ActionSeveralTaskTypesOptionsBase
        {
            public const string SectionName = nameof(KrApprovalActionOptionsVirtual);
        }

        public abstract class KrApprovalActionOptionLinksVirtual
            : ActionOptionActionLinksBase
        {
            public const string SectionName = nameof(KrApprovalActionOptionLinksVirtual);
        }

        public static class KrApprovalActionAdditionalPerformersDisplayInfoVirtual
        {
            public const string SectionName = nameof(KrApprovalActionAdditionalPerformersDisplayInfoVirtual);

            public const string RoleID = nameof(RoleID);
            public const string RoleName = nameof(RoleName);

            public const string Order = nameof(Order);
            public const string MainApproverRowID = nameof(MainApproverRowID);
            public const string IsResponsible = nameof(IsResponsible);
        }

        public static class KrApprovalActionAdditionalPerformersVirtual
        {
            public const string SectionName = nameof(KrApprovalActionAdditionalPerformersVirtual);

            public const string Role = nameof(Role);
            public const string RoleName = nameof(RoleName);

            public const string Order = nameof(Order);
            public const string MainApprover = nameof(MainApprover);
            public const string IsResponsible = nameof(IsResponsible);
        }

        public static class KrApprovalActionAdditionalPerformersSettingsVirtual
        {
            public const string SectionName = nameof(KrApprovalActionAdditionalPerformersSettingsVirtual);

            public const string IsAdditionalApprovalFirstResponsible = nameof(IsAdditionalApprovalFirstResponsible);
        }

        public abstract class KrApprovalActionNotificationRolesVirtual
            : ActionNotificationRolesBase
        {
            public const string SectionName = nameof(KrApprovalActionNotificationRolesVirtual);
        }

        public abstract class KrApprovalActionOptionsActionVirtual
            : ActionOptionsActionBase
        {
            public const string SectionName = nameof(KrApprovalActionOptionsActionVirtual);
        }

        public static class KrApprovalActionNotificationActionRolesVirtual
        {
            public const string SectionName = nameof(KrApprovalActionNotificationActionRolesVirtual);

            public const string Role = nameof(Role);
            public const string Option = nameof(Option);
        }
        #endregion

        #region KrSigningAction
        public static class KrSigningActionVirtual
        {
            public const string SectionName = nameof(KrSigningActionVirtual);

            public const string Author = nameof(Author);
            public const string Kind = nameof(Kind);
            public const string Digest = nameof(Digest);
            public const string Period = nameof(Period);
            public const string Planned = nameof(Planned);
            public const string InitTaskScript = nameof(InitTaskScript);
            public const string Result = nameof(Result);
            public const string IsParallel = nameof(IsParallel);
            public const string ReturnWhenApproved = nameof(ReturnWhenApproved);
            public const string CanEditCard = nameof(CanEditCard);
            public const string CanEditAnyFiles = nameof(CanEditAnyFiles);
            public const string ChangeStateOnStart = nameof(ChangeStateOnStart);
            public const string ChangeStateOnEnd = nameof(ChangeStateOnEnd);
            public const string IsAdvisory = nameof(IsAdvisory);
            public const string Notification = nameof(Notification);
            public const string ExcludeDeputies = nameof(ExcludeDeputies);
            public const string ExcludeSubscribers = nameof(ExcludeSubscribers);
            public const string NotificationScript = nameof(NotificationScript);
            public const string SqlPerformersScript = nameof(SqlPerformersScript);
            public const string ExpectAllSigners = nameof(ExpectAllSigners);
            public const string AllowAdditionalApproval = nameof(AllowAdditionalApproval);
            public const string NotCreateReturnEditTaskHistoryRecord = nameof(NotCreateReturnEditTaskHistoryRecord);
            public const string SignCardFiles = nameof(SignCardFiles);
            public const string NoSignFilesDialog = nameof(NoSignFilesDialog);
            public const string DoNotSignFileCopies = nameof(DoNotSignFileCopies);
            public const string NoCommentDialog = nameof(NoCommentDialog);
        }

        public abstract class KrSigningActionOptionsVirtual
            : ActionSeveralTaskTypesOptionsBase
        {
            public const string SectionName = nameof(KrSigningActionOptionsVirtual);
        }

        public abstract class KrSigningActionOptionLinksVirtual
            : ActionOptionActionLinksBase
        {
            public const string SectionName = nameof(KrSigningActionOptionLinksVirtual);
        }

        public abstract class KrSigningActionNotificationRolesVirtual
            : ActionNotificationRolesBase
        {
            public const string SectionName = nameof(KrSigningActionNotificationRolesVirtual);
        }

        public abstract class KrSigningActionOptionsActionVirtual
            : ActionOptionsActionBase
        {
            public const string SectionName = nameof(KrSigningActionOptionsActionVirtual);
        }

        public static class KrSigningActionNotificationActionRolesVirtual
        {
            public const string SectionName = nameof(KrSigningActionNotificationActionRolesVirtual);

            public const string Role = nameof(Role);
            public const string Option = nameof(Option);
        }

        public static class KrSigningActionFileCategoriesVirtual
        {
            public const string SectionName = nameof(KrSigningActionFileCategoriesVirtual);

            public const string FileCategoryID = nameof(FileCategoryID);
            public const string FileCategoryName = nameof(FileCategoryName);
        }

        public static class KrSigningActionHiddenFileCategoriesVirtual
        {
            public const string SectionName = nameof(KrSigningActionHiddenFileCategoriesVirtual);

            public const string FileCategoryID = nameof(FileCategoryID);
            public const string FileCategoryName = nameof(FileCategoryName);
        }

        #endregion

        #region KrAmendingAction
        public static class KrAmendingActionVirtual
        {
            public const string SectionName = nameof(KrAmendingActionVirtual);

            public const string Role = nameof(Role);
            public const string Author = nameof(Author);
            public const string Kind = nameof(Kind);
            public const string Digest = nameof(Digest);
            public const string Period = nameof(Period);
            public const string Planned = nameof(Planned);
            public const string InitTaskScript = nameof(InitTaskScript);
            public const string IsChangeState = nameof(IsChangeState);
            public const string IsIncrementCycle = nameof(IsIncrementCycle);
            public const string Notification = nameof(Notification);
            public const string ExcludeDeputies = nameof(ExcludeDeputies);
            public const string ExcludeSubscribers = nameof(ExcludeSubscribers);
            public const string NotificationScript = nameof(NotificationScript);
            public const string Result = nameof(Result);
            public const string CompleteOptionTaskScript = nameof(CompleteOptionTaskScript);
            public const string CompleteOptionNotification = nameof(CompleteOptionNotification);
            public const string CompleteOptionExcludeDeputies = nameof(CompleteOptionExcludeDeputies);
            public const string CompleteOptionExcludeSubscribers = nameof(CompleteOptionExcludeSubscribers);
            public const string CompleteOptionSendToPerformer = nameof(CompleteOptionSendToPerformer);
            public const string CompleteOptionSendToAuthor = nameof(CompleteOptionSendToAuthor);
            public const string CompleteOptionNotificationScript = nameof(CompleteOptionNotificationScript);
            public const string HasEditApprovalSchemeAccess = nameof(HasEditApprovalSchemeAccess);
        }
        #endregion

        #region KrUniversalTaskAction
        public static class KrUniversalTaskActionVirtual
        {
            public const string SectionName = nameof(KrUniversalTaskActionVirtual);

            public const string Role = nameof(Role);
            public const string Author = nameof(Author);
            public const string Kind = nameof(Kind);
            public const string Digest = nameof(Digest);
            public const string Period = nameof(Period);
            public const string Planned = nameof(Planned);
            public const string InitTaskScript = nameof(InitTaskScript);
            public const string Result = nameof(Result);
            public const string CanEditCard = nameof(CanEditCard);
            public const string CanEditAnyFiles = nameof(CanEditAnyFiles);
            public const string Notification = nameof(Notification);
            public const string ExcludeDeputies = nameof(ExcludeDeputies);
            public const string ExcludeSubscribers = nameof(ExcludeSubscribers);
            public const string NotificationScript = nameof(NotificationScript);
        }

        public static class KrUniversalTaskActionButtonsVirtual
        {
            public const string SectionName = nameof(KrUniversalTaskActionButtonsVirtual);

            public const string Order = nameof(Order);
            public const string OptionID = nameof(OptionID);
            public const string Caption = nameof(Caption);
            public const string Digest = nameof(Digest);
            public const string IsShowComment = nameof(IsShowComment);
            public const string IsAdditionalOption = nameof(IsAdditionalOption);
            public const string Link = nameof(Link);
            public const string Script = nameof(Script);
            public const string Notification = nameof(Notification);
            public const string SendToPerformer = nameof(SendToPerformer);
            public const string SendToAuthor = nameof(SendToAuthor);
            public const string ExcludeDeputies = nameof(ExcludeDeputies);
            public const string ExcludeSubscribers = nameof(ExcludeSubscribers);
            public const string NotificationScript = nameof(NotificationScript);
        }

        public static class KrUniversalTaskActionButtonTaskRolesVirtual
        {
            public const string SectionName = nameof(KrUniversalTaskActionButtonTaskRolesVirtual);

            public const string TaskRole = nameof(TaskRole);
            public const string TaskButton = nameof(TaskButton);
        }

        public static class KrUniversalTaskActionButtonLinksVirtual
        {
            public const string SectionName = nameof(KrUniversalTaskActionButtonLinksVirtual);

            public const string Link = nameof(Link);
            public const string Button = nameof(Button);
        }

        public static class KrUniversalTaskActionNotificationRolesVitrual
        {
            public const string SectionName = nameof(KrUniversalTaskActionNotificationRolesVitrual);

            public const string Role = nameof(Role);
            public const string Button = nameof(Button);
        }
        #endregion

        #region KrResolutionAction
        public static class KrResolutionActionVirtual
        {
            public const string SectionName = nameof(KrResolutionActionVirtual);

            public const string Author = nameof(Author);
            public const string Sender = nameof(Sender);
            public const string Kind = nameof(Kind);
            public const string Digest = nameof(Digest);
            public const string Period = nameof(Period);
            public const string Planned = nameof(Planned);
            public const string IsMajorPerformer = nameof(IsMajorPerformer);
            public const string IsMassCreation = nameof(IsMassCreation);
            public const string WithControl = nameof(WithControl);
            public const string Controller = nameof(Controller);
            public const string SqlPerformersScript = nameof(SqlPerformersScript);
        }
        #endregion

        #region KrRouteInitializationAction
        public static class KrRouteInitializationActionVirtual
        {
            public const string SectionName = nameof(KrRouteInitializationActionVirtual);

            public const string Initiator = nameof(Initiator);
            public const string InitiatorComment = nameof(InitiatorComment);
        }
        #endregion

        public abstract class ActionSeveralTaskTypesOptionsBase
            : ActionOptionsBase
        {
            public const string TaskType = nameof(TaskType);

            public const string Result = nameof(Result);
        }

        public abstract class ActionOptionsBase
        {
            public const string Option = nameof(Option);
            public const string Script = nameof(Script);
            public const string Order = nameof(Order);
            public const string Notification = nameof(Notification);
            public const string SendToPerformer = nameof(SendToPerformer);
            public const string SendToAuthor = nameof(SendToAuthor);
            public const string ExcludeDeputies = nameof(ExcludeDeputies);
            public const string ExcludeSubscribers = nameof(ExcludeSubscribers);
            public const string NotificationScript = nameof(NotificationScript);
        }

        public abstract class ActionOptionsActionBase
        {
            public const string ActionOption = nameof(ActionOption);
            public const string Link = nameof(Link);
            public const string Script = nameof(Script);
            public const string Order = nameof(Order);
            public const string Notification = nameof(Notification);
            public const string ExcludeDeputies = nameof(ExcludeDeputies);
            public const string ExcludeSubscribers = nameof(ExcludeSubscribers);
            public const string NotificationScript = nameof(NotificationScript);
        }

        public static class KrWeRolesVirtual
        {
            public const string SectionName = nameof(KrWeRolesVirtual);

            public const string Role = nameof(Role);
            public const string Order = nameof(Order);
        }

        public static class KrWeActionCompletionOptions
        {
            public const string SectionName = nameof(KrWeActionCompletionOptions);

            public const string ID = Names.Table_ID;
            public const string Name = Table_Field_Name;
            public const string Caption = Table_Field_Caption;
        }

        public abstract class ActionNotificationRolesBase
        {
            public const string Role = nameof(Role);
            public const string Option = nameof(Option);
        }

        public abstract class ActionOptionLinksBase
        {
            public const string Link = nameof(Link);
            public const string Option = nameof(Option);
        }

        public abstract class ActionOptionActionLinksBase
        {
            public const string Link = nameof(Link);
            public const string ActionOption = nameof(ActionOption);
        }

        public const string Table_Field_Name = "Name";
        public const string Table_Field_Caption = "Caption";

        #endregion

        /// <summary>
        /// Предоставляет алиасы контролов пользовательского интерфейса.
        /// </summary>
        public static class UI
        {
            public const string Performers = nameof(Performers);
            public const string AdditionalApprovalContainer = nameof(AdditionalApprovalContainer);
            public const string AdditionalApprovalBlock = nameof(AdditionalApprovalBlock);
            public const string AdditionalApprovers = nameof(AdditionalApprovers);
            public const string AddComputedRoleLink = nameof(AddComputedRoleLink);
            public const string CompletionOptionsTable = nameof(CompletionOptionsTable);
            public const string IsMassCreationCheckBox = nameof(IsMassCreationCheckBox);
            public const string IsMajorPerformerCheckBox = nameof(IsMajorPerformerCheckBox);
            public const string ControllerAutoComplete = nameof(ControllerAutoComplete);
            public const string UseRoutesInWorkflowEngine = nameof(UseRoutesInWorkflowEngine);
            public const string ActionCompletionOptionsTable = nameof(ActionCompletionOptionsTable);
            public const string ActionEventsTable = nameof(ActionEventsTable);
            public const string SignCardFiles = nameof(SignCardFiles);
        }

        /// <summary>
        /// Предоставляет названия ключей, по которым содержатся значения хранящиеся в параметрах объектов.
        /// </summary>
        public static class NamesKeys
        {
            /// <summary>
            /// Название ключа, по которому в <see cref="WorkflowStorageBase.Hash"/> <see cref="IWorkflowEngineContext.ProcessInstance"/> содержится номер цикла согласования. Тип значения: <see cref="int"/>.
            /// </summary>
            public const string ProcessCycle = nameof(ProcessCycle);

            /// <summary>
            /// Название ключа, по которому в <see cref="IWorkflowEngineSignal.Hash"/> <see cref="WorkflowEngineTaskSignal"/> содержится признак завершения задания,
            /// отправленного в процессе типа <see cref="Wf.WfHelper.ResolutionProcessName"/>. Тип значения: <see cref="bool"/>.
            /// </summary>
            /// <remarks>
            /// Параметр используется при обработке завершения заданий в действии <see cref="T:Tessa.Extensions.Default.Server.Workflow.WorkflowEngine.KrResolutionAction"/>.
            /// В параметре <see cref="WorkflowEngineTaskSignal.TaskIDs"/> указан идентификатор родительского задания типа <see cref="DefaultTaskTypes.WfResolutionProjectTypeID"/>.
            /// </remarks>
            public const string ResolutionFinished = StorageHelper.SystemKeyPrefix + nameof(ResolutionFinished);

            /// <summary>
            /// Название ключа, по которому в <see cref="IWorkflowEngineContext.ActionInstance"/> хранится порядковый номер текущего исполнителя. Тип значения: <see cref="int"/>.
            /// </summary>

            /// <summary>
            /// Имя ключа, по которому в <see cref="CardTask.Settings"/> содержится значение флага, дающего права на редактирование схемы согласования.
            /// Тип значения: <see cref="bool"/>. Значение по умолчанию: <see langword="false"/>.
            /// </summary>
            /// <seealso cref="RouteTaskPermissionsTaskTypes"/>
            /// <seealso cref="T:Tessa.Extensions.Default.Server.Workflow.WorkflowEngine.KrAmendingAction"/>
            public const string CanEditApprovalScheme = StorageHelper.SystemKeyPrefix + nameof(CanEditApprovalScheme);
            public const string CurrentPerformerIndex = StorageHelper.SystemKeyPrefix + nameof(CurrentPerformerIndex);

            /// <summary>
            /// Название ключа, по которому в <see cref="IWorkflowEngineContext.ActionInstance"/> хранится флаг, показывающий, будет ли результат обработки действия отрицательным. Тип значения: <see cref="bool"/>.
            /// </summary>
            public const string IsNegativeActionResult = StorageHelper.SystemKeyPrefix + nameof(IsNegativeActionResult);

            /// <summary>
            /// Название ключа, по которому в <see cref="IWorkflowEngineContext.ActionInstance"/> хранится полный список исполнителей. Тип значения: список <see cref="IList{T}"/>, где T - словарь <see cref="IDictionary{TKey, TValue}"/> представляющий соответствующий объект <see cref="RoleEntryStorage"/>.
            /// </summary>
            public const string RoleList = StorageHelper.SystemKeyPrefix + nameof(RoleList);
        }

        #endregion

        #region Static fields

        /// <summary>
        /// Неизменяемый неупорядоченный набор типов заданий используемых в маршрутах Workflow Engine.
        /// </summary>
        public static readonly ImmutableHashSet<Guid> RouteTaskTypes =
            [
                DefaultTaskTypes.KrApproveTypeID,
                DefaultTaskTypes.KrSigningTypeID,
                DefaultTaskTypes.KrUniversalTaskTypeID,
                DefaultTaskTypes.KrEditTypeID,
                DefaultTaskTypes.KrEditInterjectTypeID,
                DefaultTaskTypes.KrRegistrationTypeID,
                DefaultTaskTypes.KrRequestCommentTypeID,
            ];

        /// <summary>
        /// Неизменяемый неупорядоченный набор типов заданий используемых в маршрутах Workflow Engine предоставляющих специальные параметры прав доступа.
        /// </summary>
        /// <remarks>Является подмножеством <see cref="RouteTaskTypes"/>.</remarks>
        public static readonly ImmutableHashSet<Guid> RouteTaskPermissionsTaskTypes =
            [
                DefaultTaskTypes.KrApproveTypeID,
                DefaultTaskTypes.KrSigningTypeID,
                DefaultTaskTypes.KrUniversalTaskTypeID
            ];

        /// <summary>
        /// Неизменяемый неупорядоченный набор имён полей, которые не надо контролировать при принятии решения об отображении индикатора наличия изменений в строке табличного контрола.
        /// </summary>
        public static readonly ImmutableHashSet<string> ExceptSectionFieldNames =
            ImmutableHashSet.Create(
                StringComparer.Ordinal,
                // with some .NET SDK versions compiler might try to choose overload with "scoped ReadOnlySpan<T>",
                // resulting in error: The feature 'params collections' is currently in Preview and *unsupported*.

                // ReSharper disable once RedundantExplicitParamsArrayCreation
                new[]
                {
                    ActionSeveralTaskTypesOptionsBase.TaskType + Names.Table_ID,
                    ActionSeveralTaskTypesOptionsBase.TaskType + Table_Field_Name,
                    ActionSeveralTaskTypesOptionsBase.TaskType + Table_Field_Caption,

                    ActionOptionsBase.Option + Names.Table_ID,
                    ActionOptionsBase.Option + Table_Field_Caption,

                    ActionOptionsActionBase.ActionOption + Names.Table_ID,
                    ActionOptionsActionBase.ActionOption + Table_Field_Caption,

                    "EventID",
                    "EventName"
                });

        /// <summary>
        /// Неизменяемый неупорядоченный набор имён секций, которые не надо контролировать при принятии решения об отображении индикатора наличия изменений в строке табличного контрола.
        /// </summary>
        public static readonly ImmutableHashSet<string> ExceptSectionNames =
            ImmutableHashSet.Create(
                StringComparer.Ordinal,
                // with some .NET SDK versions compiler might try to choose overload with "scoped ReadOnlySpan<T>",
                // resulting in error: The feature 'params collections' is currently in Preview and *unsupported*.

                // ReSharper disable once RedundantExplicitParamsArrayCreation
                new[]
                {
                    KrApprovalActionOptionLinksVirtual.SectionName,
                    KrSigningActionOptionLinksVirtual.SectionName,
                    KrTaskRegistrationActionOptionLinksVirtual.SectionName,
                    KrUniversalTaskActionButtonLinksVirtual.SectionName
                });

        /// <summary>
        /// Компаратор определяющий порядок сохранения карточек в действиях взаимодействующих с элементами подсистемы маршрутов. Порядок сохранения определяется <see cref="KrConstants.KrCardStorePriority"/>.
        /// </summary>
        public static readonly CardTypePriorityComparer KrCardStorePriorityComparerDefault = new(KrConstants.KrCardStorePriority);

        #endregion
    }
}
