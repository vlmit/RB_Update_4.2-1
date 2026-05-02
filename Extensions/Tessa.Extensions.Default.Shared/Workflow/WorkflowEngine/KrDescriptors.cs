using System.Collections.Generic;
using Tessa.Workflow.Actions.Descriptors;

namespace Tessa.Extensions.Default.Shared.Workflow.WorkflowEngine
{
    public static class KrDescriptors
    {
        public static readonly WorkflowActionDescriptor KrChangeStateDescriptor =
            new(DefaultCardTypes.KrChangeStateActionTypeID)
            {
                Group = "$KrActions_StandardSolutionGroup",
                Icon = "Thin248",
                Order = 99,
                NotPersistentModeAllowed = true
            };

        public static readonly WorkflowActionDescriptor CreateCardDescriptor =
            new(DefaultCardTypes.WorkflowCreateCardActionTypeID)
            {
                Icon = "Thin1",
                Methods =
                [
                    KrWorkflowActionMethods.CreateCardInitMethod
                ],
                NotPersistentModeAllowed = true
            };

        /// <summary>
        /// Описание действия "Ознакомление".
        /// </summary>
        public static readonly WorkflowActionDescriptor AcquaintanceDescriptor =
            new(DefaultCardTypes.KrAcquaintanceActionTypeID)
            {
                Group = "$KrActions_StandardSolutionGroup",
                Icon = "Thin83",
                NotPersistentModeAllowed = true
            };

        public static readonly WorkflowActionDescriptor RegistrationDescriptor =
            new(DefaultCardTypes.KrRegistrationActionTypeID)
            {
                Group = "$KrActions_StandardSolutionGroup",
                Icon = "Thin325",
                NotPersistentModeAllowed = true,
            };

        public static readonly WorkflowActionDescriptor DeregistrationDescriptor =
            new(DefaultCardTypes.KrDeregistrationActionTypeID)
            {
                Group = "$KrActions_StandardSolutionGroup",
                Icon = "Thin325",
                NotPersistentModeAllowed = true,
            };

        /// <summary>
        /// Задание регистрации.
        /// </summary>
        public static readonly WorkflowActionDescriptor KrTaskRegistrationDescriptor =
            new(DefaultCardTypes.KrTaskRegistrationActionTypeID)
            {
                Group = "$KrActions_RoutesGroup",
                Icon = "Thin325",
                Methods =
                [
                    KrWorkflowActionMethods.KrTaskRegistrationTaskInitMethod,
                    KrWorkflowActionMethods.KrTaskRegistrationTaskOptionMethod,
                    WorkflowActionMethods.TaskEventMethod,
                    KrWorkflowActionMethods.KrTaskRegistrationTaskStartNotificationMethod,
                    KrWorkflowActionMethods.KrTaskRegistrationTaskCompleteNotificationMethod
                ]
            };

        /// <summary>
        /// Согласование.
        /// </summary>
        public static readonly WorkflowActionDescriptor KrApprovalDescriptor =
            new(DefaultCardTypes.KrApprovalActionTypeID)
            {
                Group = "$KrActions_RoutesGroup",
                Icon = "Int968",
                Methods =
                [
                    KrWorkflowActionMethods.KrApprovalInitMethod,
                    KrWorkflowActionMethods.KrApprovalOptionMethod,
                    WorkflowActionMethods.TaskEventMethod,
                    KrWorkflowActionMethods.KrApprovalStartNotificationMethod,
                    KrWorkflowActionMethods.KrApprovalCompleteNotificationMethod,
                    KrWorkflowActionMethods.KrApprovalActionOptionActionMethod,
                    KrWorkflowActionMethods.KrApprovalCompleteActionNotificationMethod,
                    KrWorkflowActionMethods.EditInterjectTaskInitMethod,
                    KrWorkflowActionMethods.EditInterjectTaskStartNotificationMethod,
                    KrWorkflowActionMethods.AdditionalApprovalTaskInitMethod,
                    KrWorkflowActionMethods.AdditionalApprovalTaskStartNotificationMethod,
                    KrWorkflowActionMethods.RequestCommentTaskInitMethod,
                    KrWorkflowActionMethods.RequestCommentTaskStartNotificationMethod
                ],
                Version = 2
            };

        /// <summary>
        /// Подписание.
        /// </summary>
        public static readonly WorkflowActionDescriptor KrSigningDescriptor =
            new(DefaultCardTypes.KrSigningActionTypeID)
            {
                Group = "$KrActions_RoutesGroup",
                Icon = "Int1042",
                Methods =
                [
                    KrWorkflowActionMethods.KrSigningInitMethod,
                    KrWorkflowActionMethods.KrSigningOptionMethod,
                    WorkflowActionMethods.TaskEventMethod,
                    KrWorkflowActionMethods.KrSigningStartNotificationMethod,
                    KrWorkflowActionMethods.KrSigningCompleteNotificationMethod,
                    KrWorkflowActionMethods.KrSigningActionOptionActionMethod,
                    KrWorkflowActionMethods.KrSigningCompleteActionNotificationMethod,
                    KrWorkflowActionMethods.EditInterjectTaskInitMethod,
                    KrWorkflowActionMethods.EditInterjectTaskStartNotificationMethod,
                    KrWorkflowActionMethods.AdditionalApprovalTaskInitMethod,
                    KrWorkflowActionMethods.AdditionalApprovalTaskStartNotificationMethod,
                    KrWorkflowActionMethods.RequestCommentTaskInitMethod,
                    KrWorkflowActionMethods.RequestCommentTaskStartNotificationMethod
                ],
                Version = 2
            };

        /// <summary>
        /// Доработка.
        /// </summary>
        public static readonly WorkflowActionDescriptor KrAmendingDescriptor =
            new(DefaultCardTypes.KrAmendingActionTypeID)
            {
                Group = "$KrActions_RoutesGroup",
                Icon = "Thin3",
                Methods =
                [
                    WorkflowActionMethods.TaskEventMethod,
                    KrWorkflowActionMethods.KrAmendingInitMethod,
                    KrWorkflowActionMethods.KrAmendingStartNotificationMethod,
                    KrWorkflowActionMethods.KrAmendingOptionMethod,
                    KrWorkflowActionMethods.KrAmendingCompleteNotificationMethod
                ]
            };

        /// <summary>
        /// Настраиваемое задание.
        /// </summary>
        public static readonly WorkflowActionDescriptor KrUniversalTaskDescriptor =
            new(DefaultCardTypes.KrUniversalTaskActionTypeID)
            {
                Group = "$KrActions_RoutesGroup",
                Version = 2,
                Icon = "Int1053",
                Methods =
                [
                    WorkflowActionMethods.TaskEventMethod,
                    KrWorkflowActionMethods.KrUniversalTaskInitMethod,
                    WorkflowActionMethods.TaskStartNotificationMethod,
                    KrWorkflowActionMethods.KrUniversalTaskOptionMethod,
                    WorkflowActionMethods.TaskCompleteNotificationMethod,
                    WorkflowActionMethods.TaskRoleMethod
                ]
            };

        /// <summary>
        /// Выполнение задачи.
        /// </summary>
        public static readonly WorkflowActionDescriptor KrResolutionDescriptor =
            new(DefaultCardTypes.KrResolutionActionTypeID)
            {
                Group = "$KrActions_RoutesGroup",
                Icon = "Int1036",
                Methods =
                [
                    WorkflowActionMethods.TaskEventMethod
                ]
            };

        /// <summary>
        /// Инициализация маршрута.
        /// </summary>
        public static readonly WorkflowActionDescriptor KrRouteInitializationDescriptor =
            new (DefaultCardTypes.KrRouteInitializationActionTypeID)
            {
                Group = "$KrActions_RoutesGroup",
                Icon = "Int146"
            };

        /// <summary>
        /// Все дескрипторы типовых действий KrDescriptors.
        /// </summary>
        public static IReadOnlyList<WorkflowActionDescriptor> All =>
        [
            KrChangeStateDescriptor,
                CreateCardDescriptor,
                AcquaintanceDescriptor,
                RegistrationDescriptor,
                DeregistrationDescriptor,
                KrTaskRegistrationDescriptor,
                KrApprovalDescriptor,
                KrSigningDescriptor,
                KrAmendingDescriptor,
                KrUniversalTaskDescriptor,
                KrResolutionDescriptor,
                KrRouteInitializationDescriptor
        ];
    }
}
