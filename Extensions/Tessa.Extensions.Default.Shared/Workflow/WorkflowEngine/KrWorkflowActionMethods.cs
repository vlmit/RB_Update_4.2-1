using System;
using System.Collections.Generic;
using Tessa.Cards;
using Tessa.Files;
using Tessa.Localization;
using Tessa.Notices;
using Tessa.Platform.Storage;
using Tessa.Scheme;
using Tessa.Workflow.Actions;
using Tessa.Workflow.Actions.Descriptors;
using Tessa.Workflow.Helpful;
using static Tessa.Extensions.Default.Shared.Workflow.WorkflowEngine.WorkflowConstants;

namespace Tessa.Extensions.Default.Shared.Workflow.WorkflowEngine
{
    /// <summary>
    /// Описания методов действий.
    /// </summary>
    public static class KrWorkflowActionMethods
    {
        #region Methods parameters

        public static readonly IReadOnlyList<(string Type, string Name)> TaskCompleteBaseParams =
        [
            new(nameof(CardTask), "task"),
            new("dynamic", "taskCard"),
            new("dynamic", "taskCardTables"),
            new(nameof(WorkflowTaskNotificationInfoBase), "notificationInfo")
        ];

        public static readonly IReadOnlyList<(string Type, string Name)> ActionCompleteParams =
        [
            new(nameof(WorkflowTaskNotificationInfoBase), "notificationInfo")
        ];

        #endregion

        #region KrTaskRegistration methods

        /// <summary>
        /// Дескриптор метода инициализации задания в действии <see cref="KrDescriptors.KrTaskRegistrationDescriptor"/>.
        /// </summary>
        public static readonly WorkflowActionMethodDescriptor KrTaskRegistrationTaskInitMethod =
            CreateTaskInitMethodTemplate(
                [KrTaskRegistrationActionVirtual.SectionName, KrTaskRegistrationActionVirtual.InitTaskScript]);

        /// <summary>
        /// Дескриптор метода выполняющегося при завершении задания с определённым вариантом завершения в действии <see cref="KrDescriptors.KrTaskRegistrationDescriptor"/>.
        /// </summary>
        public static readonly WorkflowActionMethodDescriptor KrTaskRegistrationTaskOptionMethod =
            CreateTaskOptionMethodTemplate(
                [KrTaskRegistrationActionOptionsVirtual.Script],
                [KrTaskRegistrationActionOptionsVirtual.SectionName],
                [KrTaskRegistrationActionOptionsVirtual.Option, Table_Field_Caption],
                [KrTaskRegistrationActionOptionsVirtual.Option, Table_Field_Caption]);

        /// <summary>
        /// Дескриптор метода изменения уведомления отправляющегося при завершении задания с определённым вариантом завершения в действии <see cref="KrDescriptors.KrTaskRegistrationDescriptor"/>.
        /// </summary>
        public static readonly WorkflowActionMethodDescriptor KrTaskRegistrationTaskCompleteNotificationMethod =
            CreateTaskCompleteNotificationMethodTemplate(
                [KrTaskRegistrationActionOptionsVirtual.NotificationScript],
                [KrTaskRegistrationActionOptionsVirtual.SectionName],
                [KrTaskRegistrationActionOptionsVirtual.Option, Table_Field_Caption],
                [KrTaskRegistrationActionOptionsVirtual.Option, Table_Field_Caption]);

        /// <summary>
        /// Дескриптор метода изменения уведомления отправляющегося при отправлении задания в действии <see cref="KrDescriptors.KrTaskRegistrationDescriptor"/>.
        /// </summary>
        public static readonly WorkflowActionMethodDescriptor KrTaskRegistrationTaskStartNotificationMethod =
            CreateTaskStartNotificationMethodTemplate(
                [KrTaskRegistrationActionVirtual.SectionName, KrTaskRegistrationActionVirtual.NotificationScript]);

        #endregion

        #region KrApproval methods

        /// <summary>
        /// Дескриптор метода инициализации задания в действии <see cref="KrDescriptors.KrApprovalDescriptor"/>.
        /// </summary>
        public static readonly WorkflowActionMethodDescriptor KrApprovalInitMethod =
            CreateTaskInitMethodTemplate(
                [KrApprovalActionVirtual.SectionName, KrApprovalActionVirtual.InitTaskScript]);

        /// <summary>
        /// Дескриптор метода изменения уведомления отправляющегося при отправлении задания в действии <see cref="KrDescriptors.KrApprovalDescriptor"/>.
        /// </summary>
        public static readonly WorkflowActionMethodDescriptor KrApprovalStartNotificationMethod =
            CreateTaskStartNotificationMethodTemplate(
                [KrApprovalActionVirtual.SectionName, KrApprovalActionVirtual.NotificationScript]);

        /// <summary>
        /// Дескриптор метода выполняющегося при завершении задания с определённым вариантом завершения в действии <see cref="KrDescriptors.KrApprovalDescriptor"/>.
        /// </summary>
        public static readonly WorkflowActionMethodDescriptor KrApprovalOptionMethod =
            CreateTaskOptionMethodTemplate(
                [KrApprovalActionOptionsVirtual.Script],
                [KrApprovalActionOptionsVirtual.SectionName],
                [KrApprovalActionOptionsVirtual.Option, Table_Field_Caption],
                [KrApprovalActionOptionsVirtual.Option, Table_Field_Caption]);

        /// <summary>
        /// Дескриптор метода изменения уведомления отправляющегося при завершении задания с определённым вариантом завершения в действии <see cref="KrDescriptors.KrApprovalDescriptor"/>.
        /// </summary>
        public static readonly WorkflowActionMethodDescriptor KrApprovalCompleteNotificationMethod =
            CreateTaskCompleteNotificationMethodTemplate(
                [KrApprovalActionOptionsVirtual.NotificationScript],
                [KrApprovalActionOptionsVirtual.SectionName],
                [KrApprovalActionOptionsVirtual.Option, Table_Field_Caption],
                [KrApprovalActionOptionsVirtual.Option, Table_Field_Caption]);

        /// <summary>
        /// Дескриптор метода выполняющегося при завершении задания с определённым вариантом завершения в действии <see cref="KrDescriptors.KrApprovalDescriptor"/>.
        /// </summary>
        public static readonly WorkflowActionMethodDescriptor KrApprovalActionOptionActionMethod =
            CreateActionOptionMethodTemplate(
                [KrApprovalActionOptionsActionVirtual.Script],
                [KrApprovalActionOptionsActionVirtual.SectionName],
                [KrApprovalActionOptionsActionVirtual.ActionOption, Table_Field_Caption],
                [KrApprovalActionOptionsActionVirtual.ActionOption, Table_Field_Caption]);

        /// <summary>
        /// Дескриптор метода изменения уведомления отправляющегося при завершении действия с определённым вариантом завершения в действии <see cref="KrDescriptors.KrApprovalDescriptor"/>.
        /// </summary>
        public static readonly WorkflowActionMethodDescriptor KrApprovalCompleteActionNotificationMethod =
            CreateActionCompleteNotificationMethodTemplate(
                [KrApprovalActionOptionsActionVirtual.NotificationScript],
                [KrApprovalActionOptionsActionVirtual.SectionName],
                [KrApprovalActionOptionsActionVirtual.ActionOption, Table_Field_Caption],
                [KrApprovalActionOptionsActionVirtual.ActionOption, Table_Field_Caption]);

        #endregion

        #region KrSigning methods

        /// <summary>
        /// Дескриптор метода инициализации задания в действии <see cref="KrDescriptors.KrSigningDescriptor"/>.
        /// </summary>
        public static readonly WorkflowActionMethodDescriptor KrSigningInitMethod =
            CreateTaskInitMethodTemplate(
                [KrSigningActionVirtual.SectionName, KrSigningActionVirtual.InitTaskScript]);

        /// <summary>
        /// Дескриптор метода изменения уведомления отправляющегося при отправлении задания в действии <see cref="KrDescriptors.KrSigningDescriptor"/>.
        /// </summary>
        public static readonly WorkflowActionMethodDescriptor KrSigningStartNotificationMethod =
            CreateTaskStartNotificationMethodTemplate(
                [KrSigningActionVirtual.SectionName, KrSigningActionVirtual.NotificationScript]);

        /// <summary>
        /// Дескриптор метода выполняющегося при завершении задания с определённым вариантом завершения в действии <see cref="KrDescriptors.KrSigningDescriptor"/>.
        /// </summary>
        public static readonly WorkflowActionMethodDescriptor KrSigningOptionMethod =
            CreateTaskOptionMethodTemplate(
                [KrSigningActionOptionsVirtual.Script],
                [KrSigningActionOptionsVirtual.SectionName],
                [KrSigningActionOptionsVirtual.Option, Table_Field_Caption],
                [KrSigningActionOptionsVirtual.Option, Table_Field_Caption]);

        /// <summary>
        /// Дескриптор метода изменения уведомления отправляющегося при завершении задания с определённым вариантом завершения в действии <see cref="KrDescriptors.KrSigningDescriptor"/>.
        /// </summary>
        public static readonly WorkflowActionMethodDescriptor KrSigningCompleteNotificationMethod =
            CreateTaskCompleteNotificationMethodTemplate(
                [KrSigningActionOptionsVirtual.NotificationScript],
                [KrSigningActionOptionsVirtual.SectionName],
                [KrSigningActionOptionsVirtual.Option, Table_Field_Caption],
                [KrSigningActionOptionsVirtual.Option, Table_Field_Caption]);

        /// <summary>
        /// Дескриптор метода выполняющегося при завершении задания с определённым вариантом завершения в действии <see cref="KrDescriptors.KrSigningDescriptor"/>.
        /// </summary>
        public static readonly WorkflowActionMethodDescriptor KrSigningActionOptionActionMethod =
            CreateActionOptionMethodTemplate(
                [KrSigningActionOptionsActionVirtual.Script],
                [KrSigningActionOptionsActionVirtual.SectionName],
                [KrSigningActionOptionsActionVirtual.ActionOption, Table_Field_Caption],
                [KrSigningActionOptionsActionVirtual.ActionOption, Table_Field_Caption]);

        /// <summary>
        /// Дескриптор метода изменения уведомления отправляющегося при завершении действия с определённым вариантом завершения в действии <see cref="KrDescriptors.KrSigningDescriptor"/>.
        /// </summary>
        public static readonly WorkflowActionMethodDescriptor KrSigningCompleteActionNotificationMethod =
            CreateActionCompleteNotificationMethodTemplate(
                [KrSigningActionOptionsActionVirtual.NotificationScript],
                [KrSigningActionOptionsActionVirtual.SectionName],
                [KrSigningActionOptionsActionVirtual.ActionOption, Table_Field_Caption],
                [KrSigningActionOptionsActionVirtual.ActionOption, Table_Field_Caption]);

        #endregion

        #region KrAmending methods

        /// <summary>
        /// Дескриптор метода инициализации задания в действии <see cref="KrDescriptors.KrAmendingDescriptor"/>.
        /// </summary>
        public static readonly WorkflowActionMethodDescriptor KrAmendingInitMethod =
            CreateTaskInitMethodTemplate(
                [KrAmendingActionVirtual.SectionName, KrAmendingActionVirtual.InitTaskScript]);

        /// <summary>
        /// Дескриптор метода изменения уведомления отправляющегося при отправлении задания в действии <see cref="KrDescriptors.KrAmendingDescriptor"/>.
        /// </summary>
        public static readonly WorkflowActionMethodDescriptor KrAmendingStartNotificationMethod =
            CreateTaskStartNotificationMethodTemplate(
                [KrAmendingActionVirtual.SectionName, KrAmendingActionVirtual.NotificationScript]);

        /// <summary>
        /// Дескриптор метода выполняющегося при завершении задания с определённым вариантом завершения в действии <see cref="KrDescriptors.KrAmendingDescriptor"/>.
        /// </summary>
        public static readonly WorkflowActionMethodDescriptor KrAmendingOptionMethod =
            new()
            {
                ErrorDescription = "$KrActions_Task_SingleOptionScriptError",
                MethodName = "CompleteOptionTaskScript",
                Parameters = TaskCompleteBaseParams,
                StorePath = [KrAmendingActionVirtual.SectionName, KrAmendingActionVirtual.CompleteOptionTaskScript],
            };

        /// <summary>
        /// Дескриптор метода изменения уведомления отправляющегося при завершении задания с определённым вариантом завершения в действии <see cref="KrDescriptors.KrAmendingDescriptor"/>.
        /// </summary>
        public static readonly WorkflowActionMethodDescriptor KrAmendingCompleteNotificationMethod =
            CreateCompletionTaskNotificationMethodTemplate(
                [KrAmendingActionVirtual.SectionName, KrAmendingActionVirtual.CompleteOptionNotificationScript]);

        #endregion

        #region KrUniversalTask methods

        /// <summary>
        /// Дескриптор метода инициализации задания в действии <see cref="KrDescriptors.KrUniversalTaskDescriptor"/>.
        /// </summary>
        public static readonly WorkflowActionMethodDescriptor KrUniversalTaskInitMethod =
            CreateTaskInitMethodTemplate(
                [KrUniversalTaskActionVirtual.SectionName, KrUniversalTaskActionVirtual.InitTaskScript]);

        /// <summary>
        /// Дескриптор метода выполняющегося при завершении задания с определённым вариантом завершения в действии <see cref="KrDescriptors.KrUniversalTaskDescriptor"/>.
        /// </summary>
        public static readonly WorkflowActionMethodDescriptor KrUniversalTaskOptionMethod =
            CreateTaskOptionMethodTemplate(
                [KrUniversalTaskActionButtonsVirtual.Script],
                [KrUniversalTaskActionButtonsVirtual.SectionName],
                [Table_Field_Caption],
                [Table_Field_Caption]);

        #endregion

        #region EditInterjectTask

        /// <summary>
        /// Дескриптор метода инициализации задания доработки автором <see cref="DefaultTaskTypes.KrEditInterjectTypeID"/>.
        /// </summary>
        public static readonly WorkflowActionMethodDescriptor EditInterjectTaskInitMethod =
            CreateTaskInitMethodTemplate(
                [KrWeEditInterjectOptionsVirtual.SectionName, KrWeEditInterjectOptionsVirtual.InitTaskScript],
                "InitEditInterjectTaskScript");

        /// <summary>
        /// Дескриптор метода изменения уведомления отправляющегося при отправлении задания доработки автором <see cref="DefaultTaskTypes.KrEditInterjectTypeID"/>.
        /// </summary>
        public static readonly WorkflowActionMethodDescriptor EditInterjectTaskStartNotificationMethod =
            CreateTaskStartNotificationMethodTemplate(
                [KrWeEditInterjectOptionsVirtual.SectionName, KrWeEditInterjectOptionsVirtual.NotificationScript],
                "EditInterjectTaskNotificationScript");

        #endregion

        #region AdditionalApproval

        /// <summary>
        /// Дескриптор метода инициализации задания дополнительного согласования <see cref="DefaultTaskTypes.KrAdditionalApprovalTypeID"/>.
        /// </summary>
        public static readonly WorkflowActionMethodDescriptor AdditionalApprovalTaskInitMethod =
            CreateTaskInitMethodTemplate(
                [KrWeAdditionalApprovalOptionsVirtual.SectionName, KrWeAdditionalApprovalOptionsVirtual.InitTaskScript],
                "InitAdditionalApprovalTaskScript");

        /// <summary>
        /// Дескриптор метода изменения уведомления отправляющегося при отправлении задания дополнительного согласования <see cref="DefaultTaskTypes.KrAdditionalApprovalTypeID"/>.
        /// </summary>
        public static readonly WorkflowActionMethodDescriptor AdditionalApprovalTaskStartNotificationMethod =
            CreateTaskStartNotificationMethodTemplate(
                [KrWeAdditionalApprovalOptionsVirtual.SectionName, KrWeAdditionalApprovalOptionsVirtual.NotificationScript],
                "AdditionalApprovalTaskNotificationScript");

        #endregion

        #region RequestComment

        /// <summary>
        /// Дескриптор метода инициализации задания запроса комментария <see cref="DefaultTaskTypes.KrRequestCommentTypeID"/>.
        /// </summary>
        public static readonly WorkflowActionMethodDescriptor RequestCommentTaskInitMethod =
            CreateTaskInitMethodTemplate(
                [KrWeRequestCommentOptionsVirtual.SectionName, KrWeRequestCommentOptionsVirtual.InitTaskScript],
                "InitRequestCommentTaskScript");

        /// <summary>
        /// Дескриптор метода изменения уведомления отправляющегося при отправлении задания запроса комментария <see cref="DefaultTaskTypes.KrRequestCommentTypeID"/>.
        /// </summary>
        public static readonly WorkflowActionMethodDescriptor RequestCommentTaskStartNotificationMethod =
            CreateTaskStartNotificationMethodTemplate(
                [KrWeRequestCommentOptionsVirtual.SectionName, KrWeRequestCommentOptionsVirtual.NotificationScript],
                "RequestCommentTaskNotificationScript");

        #endregion

        #region WorkflowCreate Methods

        /// <summary>
        /// Дескриптор метода инициализации карточки в действии создания карточки.
        /// </summary>
        public static readonly WorkflowActionMethodDescriptor CreateCardInitMethod = new()
        {
            ErrorDescription = "$WorkflowEngine_Actions_CreateCard_CompilationError",
            MethodName = "InitCard",
            Parameters =
            [
                ("dynamic", "newCard"),
                ("dynamic", "newCardTables"),
                (nameof(Card), "newCardObject"),
                (nameof(IFileContainer), "newCardFileContainer")
            ],
            StorePath = ["KrCreateCardAction", "Script"],
        };

        #endregion

        #region Public methods

        /// <summary>
        /// Создаёт дескриптор метода инициализации задания.
        /// </summary>
        /// <param name="storePath">Путь к месту в параметрах действия, где хранится текст скрипта. Для методов формируемых автоматически по строке - данное поле определяет путь к месту не в параметрах действия, а в строке.</param>
        /// <param name="methodName">Имя метода. Значение по умолчанию: InitTaskScript.</param>
        /// <returns>Дескриптор метода действия.</returns>
        public static WorkflowActionMethodDescriptor CreateTaskInitMethodTemplate(
            string[] storePath,
            string methodName = "InitTaskScript")
        {
            return new WorkflowActionMethodDescriptor
            {
                ErrorDescription = "$WorkflowEngine_Actions_Task_InitScriptError",
                MethodName = methodName,
                Parameters = WorkflowTaskActionBase.TaskParams,
                StorePath = storePath,
            };
        }

        /// <summary>
        /// Создаёт дескриптор метода изменения уведомления отправляющегося при отправлении задания.
        /// </summary>
        /// <param name="storePath">Путь к месту в параметрах действия, где хранится текст скрипта. Для методов формируемых автоматически по строке - данное поле определяет путь к месту не в параметрах действия, а в строке.</param>
        /// <param name="methodName">Имя метода. Значение по умолчанию: NotificationScript.</param>
        /// <returns>Дескриптор метода действия.</returns>
        public static WorkflowActionMethodDescriptor CreateTaskStartNotificationMethodTemplate(
            string[] storePath,
            string methodName = "NotificationScript")
        {
            return new WorkflowActionMethodDescriptor
            {
                ErrorDescription = "$WorkflowEngine_Actions_Task_NotificationScriptError",
                MethodName = methodName,
                Parameters =
                [
                    (typeof(NotificationEmail).FullName, "email"),
                    (nameof(CardTask), "task")
                ],
                StorePath = storePath,
            };
        }

        /// <summary>
        /// Создаёт дескриптор метода выполняющегося при завершении задания с определённым вариантом завершения.
        /// </summary>
        /// <param name="storePath">Путь к месту в параметрах действия, где хранится текст скрипта. Для методов формируемых автоматически по строке - данное поле определяет путь к месту не в параметрах действия, а в строке.</param>
        /// <param name="listPath">Путь к месту в параметрах действия, где хранится таблица со скриптами. Путь к скрипту внутри строки определяется по <paramref name="storePath"/>.</param>
        /// <param name="errorDescriptionPath">Путь в строке к месту, где хранится строка используемая при формировании описания места возникновения ошибки при компиляции скрипта. Путь к строке определяется по <paramref name="listPath"/>.</param>
        /// <param name="methodDescriptionPath">Путь в строке к месту, где хранится строка используемая при формировании описания метода. Путь к строке определяется по <paramref name="listPath"/>.</param>
        /// <returns>Дескриптор метода действия.</returns>
        public static WorkflowActionMethodDescriptor CreateTaskOptionMethodTemplate(
            string[] storePath,
            string[] listPath,
            string[] errorDescriptionPath,
            string[] methodDescriptionPath)
        {
            return new WorkflowActionMethodDescriptor
            {
                ErrorDescription = "$WorkflowEngine_Actions_Task_OptionScriptError",
                MethodName = "Option",
                Parameters = WorkflowTaskActionBase.TaskCompleteParams,
                StorePath = storePath,
                ComplexDescriptor = new WorkflowActionMethodsComplexDescriptor
                {
                    ListPath = listPath,
                    GetMethodNameSuffix = hash => hash.TryGet<Guid>(Names.Table_RowID).ToString("N"),
                    GetErrorDescription = (text, hash) => LocalizeFormat(
                        text,
                        WorkflowEngineHelper.Get<string>(hash, errorDescriptionPath)),
                    GetMethodDescription = (text, index, hash) =>
                        LocalizeFormat(
                            "$WorkflowEngine_Actions_Task_OptionsDescription",
                            WorkflowEngineHelper.Get<string>(hash, methodDescriptionPath),
                            (index + 1).ToString()),
                },
            };
        }

        /// <summary>
        /// Создаёт дескриптор метода изменения уведомления отправляющегося при завершении задания с определённым вариантом завершения.
        /// </summary>
        /// <param name="storePath">Путь к месту в параметрах действия, где хранится текст скрипта. Для методов формируемых автоматически по строке - данное поле определяет путь к месту не в параметрах действия, а в строке.</param>
        /// <param name="listPath">Путь к месту в параметрах действия, где хранится таблица со скриптами. Путь к скрипту внутри строки определяется по <paramref name="storePath"/>.</param>
        /// <param name="errorDescriptionPath">Путь в строке к месту, где хранится строка используемая при формировании описания места возникновения ошибки при компиляции скрипта. Путь к строке определяется по <paramref name="listPath"/>.</param>
        /// <param name="methodDescriptionPath">Путь в строке к месту, где хранится строка используемая при формировании описания метода. Путь к строке определяется по <paramref name="listPath"/>.</param>
        /// <returns>Дескриптор метода действия.</returns>
        public static WorkflowActionMethodDescriptor CreateTaskCompleteNotificationMethodTemplate(
            string[] storePath,
            string[] listPath,
            string[] errorDescriptionPath,
            string[] methodDescriptionPath)
        {
            return new WorkflowActionMethodDescriptor
            {
                ErrorDescription = "$WorkflowEngine_Actions_Task_OptionNotificationScriptError",
                MethodName = "Notification",
                Parameters =
                [
                    (typeof(NotificationEmail).FullName, "email"),
                    (nameof(CardTask), "task")
                ],
                StorePath = storePath,
                ComplexDescriptor = new WorkflowActionMethodsComplexDescriptor
                {
                    ListPath = listPath,
                    GetMethodNameSuffix = hash => hash.TryGet<Guid>(Names.Table_RowID).ToString("N"),
                    GetErrorDescription = (text, hash) => LocalizeFormat(
                        text,
                        WorkflowEngineHelper.Get<string>(hash, errorDescriptionPath)),
                    GetMethodDescription = (text, index, hash) =>
                        LocalizeFormat(
                            "$WorkflowEngine_Actions_Task_OptionsDescription",
                            WorkflowEngineHelper.Get<string>(hash, methodDescriptionPath),
                            (index + 1).ToString()),
                },
            };
        }

        /// <summary>
        /// Создаёт дескриптор метода изменения уведомления отправляющегося при завершении задания.
        /// </summary>
        /// <param name="storePath">Путь к месту в параметрах действия, где хранится текст скрипта. Для методов формируемых автоматически по строке - данное поле определяет путь к месту не в параметрах действия, а в строке.</param>
        /// <param name="methodName">Имя метода. Значение по умолчанию: CompletionNotificationScript.</param>
        /// <returns>Дескриптор метода действия.</returns>
        public static WorkflowActionMethodDescriptor CreateCompletionTaskNotificationMethodTemplate(
            string[] storePath,
            string methodName = "CompletionNotificationScript")
        {
            return new WorkflowActionMethodDescriptor
            {
                ErrorDescription = "$KrActions_TaskCompletion_NotificationScriptError",
                MethodName = methodName,
                Parameters =
                [
                    (typeof(NotificationEmail).FullName, "email"),
                    (nameof(CardTask), "task")
                ],
                StorePath = storePath,
            };
        }

        /// <summary>
        /// Создаёт дескриптор метода выполняющегося при завершении действия с определённым вариантом завершения.
        /// </summary>
        /// <param name="storePath">Путь к месту в параметрах действия, где хранится текст скрипта. Для методов формируемых автоматически по строке - данное поле определяет путь к месту не в параметрах действия, а в строке.</param>
        /// <param name="listPath">Путь к месту в параметрах действия, где хранится таблица со скриптами. Путь к скрипту внутри строки определяется по <paramref name="storePath"/>.</param>
        /// <param name="errorDescriptionPath">Путь в строке к месту, где хранится строка используемая при формировании описания места возникновения ошибки при компиляции скрипта. Путь к строке определяется по <paramref name="listPath"/>.</param>
        /// <param name="methodDescriptionPath">Путь в строке к месту, где хранится строка используемая при формировании описания метода. Путь к строке определяется по <paramref name="listPath"/>.</param>
        /// <returns>Дескриптор метода действия.</returns>
        public static WorkflowActionMethodDescriptor CreateActionOptionMethodTemplate(
            string[] storePath,
            string[] listPath,
            string[] errorDescriptionPath,
            string[] methodDescriptionPath)
        {
            return new WorkflowActionMethodDescriptor
            {
                ErrorDescription = "$KrActions_Actions_Action_OptionScriptError",
                MethodName = "ActionOption",
                Parameters = ActionCompleteParams,
                StorePath = storePath,
                ComplexDescriptor = new WorkflowActionMethodsComplexDescriptor
                {
                    ListPath = listPath,
                    GetMethodNameSuffix = hash => hash.TryGet<Guid>(Names.Table_RowID).ToString("N"),
                    GetErrorDescription = (text, hash) => LocalizeFormat(
                        text,
                        WorkflowEngineHelper.Get<string>(hash, errorDescriptionPath)),
                    GetMethodDescription = (text, index, hash) =>
                        LocalizeFormat(
                            "$KrActions_Actions_Action_OptionsDescription",
                            WorkflowEngineHelper.Get<string>(hash, methodDescriptionPath),
                            (index + 1).ToString()),
                },
            };
        }

        /// <summary>
        /// Создаёт дескриптор метода изменения уведомления отправляющегося при завершении задания с определённым вариантом завершения.
        /// </summary>
        /// <param name="storePath">Путь к месту в параметрах действия, где хранится текст скрипта. Для методов формируемых автоматически по строке - данное поле определяет путь к месту не в параметрах действия, а в строке.</param>
        /// <param name="listPath">Путь к месту в параметрах действия, где хранится таблица со скриптами. Путь к скрипту внутри строки определяется по <paramref name="storePath"/>.</param>
        /// <param name="errorDescriptionPath">Путь в строке к месту, где хранится строка используемая при формировании описания места возникновения ошибки при компиляции скрипта. Путь к строке определяется по <paramref name="listPath"/>.</param>
        /// <param name="methodDescriptionPath">Путь в строке к месту, где хранится строка используемая при формировании описания метода. Путь к строке определяется по <paramref name="listPath"/>.</param>
        /// <returns>Дескриптор метода действия.</returns>
        public static WorkflowActionMethodDescriptor CreateActionCompleteNotificationMethodTemplate(
            string[] storePath,
            string[] listPath,
            string[] errorDescriptionPath,
            string[] methodDescriptionPath)
        {
            return new WorkflowActionMethodDescriptor
            {
                ErrorDescription = "$KrActions_Actions_Action_OptionNotificationScriptError",
                MethodName = "ActionNotification",
                Parameters = [(typeof(NotificationEmail).FullName, "email")],
                StorePath = storePath,
                ComplexDescriptor = new WorkflowActionMethodsComplexDescriptor
                {
                    ListPath = listPath,
                    GetMethodNameSuffix = hash => hash.TryGet<Guid>(Names.Table_RowID).ToString("N"),
                    GetErrorDescription = (text, hash) => LocalizeFormat(
                        text,
                        WorkflowEngineHelper.Get<string>(hash, errorDescriptionPath)),
                    GetMethodDescription = (text, index, hash) =>
                        LocalizeFormat(
                            "$KrActions_Actions_Action_OptionsDescription",
                            WorkflowEngineHelper.Get<string>(hash, methodDescriptionPath),
                            (index + 1).ToString()),
                },
            };
        }

        #endregion
    }
}
