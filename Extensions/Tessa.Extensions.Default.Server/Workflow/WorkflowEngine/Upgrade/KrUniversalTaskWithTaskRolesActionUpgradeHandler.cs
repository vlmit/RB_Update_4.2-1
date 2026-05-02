#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.Workflow.Actions;
using Tessa.Workflow.Helpful;
using Tessa.Workflow.Storage;
using Tessa.Workflow.Upgrade;
using static Tessa.Extensions.Default.Shared.Workflow.WorkflowEngine.WorkflowConstants;

namespace Tessa.Extensions.Default.Server.Workflow.WorkflowEngine.Upgrade
{
    public class KrUniversalTaskWithTaskRolesActionUpgradeHandler :
        IWorkflowEngineActionUpgradeHandler
    {
        #region Constants

        private const string MainSectionName = KrUniversalTaskActionVirtual.SectionName;

        #endregion

        #region Private Methods

        /// <summary>
        /// Формирует и добавляет в <param ref="hash"/> строку, которая отражает запись в списке связанный с заданием ролей для варианта завершения настраиваемого задания.
        /// </summary>
        /// <param name="hash">Объект хеша - хранилище, в которое добавляется информация. В данном случае таблица настроек связанных с действием ролей.</param>
        /// <param name="taskRoleID">Идентификатор функциональной роли.</param>
        /// <param name="taskRoleCaption">Название функциональной роли.</param>
        /// <param name="parentRowID">Идентификатор в родительской секции.</param>
        private static void AddTaskActionCompletionOptionTaskRoleRow(
            ICollection<object?> hash,
            Guid taskRoleID,
            string taskRoleCaption,
            Guid parentRowID) => hash.Add(
                new Dictionary<string, object?>
                {
                    { "RowID", Guid.NewGuid() },
                    {
                        "TaskRole",
                        new Dictionary<string, object?>
                        {
                            { "ID", taskRoleID },
                            { "Caption", taskRoleCaption }
                        }
                    },
                    {
                        "TaskButton",
                        new Dictionary<string, object?>
                        {
                            { "RowID", parentRowID }
                        }
                    },
                });

        /// <summary>
        /// Формирует и добавляет в <param ref="hash"/> строку, которая отражает запись в списке получателей уведомления о завершении задания для конкретного варианта завершения.
        /// </summary>
        /// <param name="hash">Объект хеша - хранилище, в которое добавляется информация. В данном случае таблица настроек связанных с действием ролей.</param>
        /// <param name="roleID">Идентификатор роли.</param>
        /// <param name="roleName">Имя роли.</param>
        /// <param name="parentRowID">Идентификатор в родительской секции.</param>
        private static void AddTaskActionNotificationRolesRow(
            ICollection<object?> hash,
            Guid roleID,
            string roleName,
            Guid parentRowID) => hash.Add(
                new Dictionary<string, object?>
                {
                    { "RowID", Guid.NewGuid() },
                    {
                        "Role",
                        new Dictionary<string, object?>
                        {
                            { "ID", roleID },
                            { "Name", roleName }
                        }
                    },
                    {
                        "TaskCompletionNotifications",
                        new Dictionary<string, object?>
                        {
                            { "RowID", parentRowID }
                        }
                    },
                });

        #endregion

        #region IWorkflowEngineActionUpgradeHandler Members

        /// <inheritdoc />
        public Task UpgradeActionTemplateAsync(
            WorkflowActionStorage actionStorage,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            // Получим поля "Автор" и "Роль"
            var authorID = WorkflowEngineHelper.Get<object>(actionStorage.Hash, MainSectionName, KrUniversalTaskActionVirtual.Author, "ID");
            var authorName = WorkflowEngineHelper.Get<string>(actionStorage.Hash, MainSectionName, KrUniversalTaskActionVirtual.Author, "Name") ?? string.Empty;

            var roleID = WorkflowEngineHelper.Get<object>(actionStorage.Hash, MainSectionName, KrUniversalTaskActionVirtual.Role, "ID");
            var roleName = WorkflowEngineHelper.Get<string>(actionStorage.Hash, MainSectionName, KrUniversalTaskActionVirtual.Role, "Name") ?? string.Empty;

            // Создадим список TaskAssignedRoles
            var taskAssignedRolesRows = new List<object?>();
            if (authorID is not null)
            {
                UpgradeHelper.AddTaskAssignedRole(taskAssignedRolesRows, CardFunctionRoles.AuthorID, "$Enum_FunctionRoles_Author", authorID, authorName);
            }

            if (roleID is not null)
            {
                UpgradeHelper.AddTaskAssignedRole(taskAssignedRolesRows, CardFunctionRoles.PerformerID, "$Enum_FunctionRoles_Performer", roleID, roleName, true, false);
            }

            // Запишем TaskAssignedRoles
            actionStorage.Hash.Add(WorkflowActionTypes.TaskRolesSectionName, taskAssignedRolesRows);

            // Удалим поля "Роль" и "Автор"
            WorkflowEngineHelper.RemoveLastPath(actionStorage.Hash, MainSectionName, KrUniversalTaskActionVirtual.Role);
            WorkflowEngineHelper.RemoveLastPath(actionStorage.Hash, MainSectionName, KrUniversalTaskActionVirtual.Author);

            // Получим настройки уведомлений
            var notificationID = WorkflowEngineHelper.Get<object>(actionStorage.Hash, MainSectionName, "Notification", "ID");
            var notificationName = WorkflowEngineHelper.Get<string>(actionStorage.Hash, MainSectionName, "Notification", "Name") ?? string.Empty;
            var notificationScript = WorkflowEngineHelper.Get<string>(actionStorage.Hash, MainSectionName, "NotificationScript");
            var notificationExcludeDeputies = WorkflowEngineHelper.Get<object>(actionStorage.Hash, MainSectionName, "ExcludeDeputies") ?? false;
            var notificationExcludeSubscribers = WorkflowEngineHelper.Get<object>(actionStorage.Hash, MainSectionName, "ExcludeSubscribers") ?? false;

            // Создадим список уведомлений о задании
            var startTaskNotifications = new List<object?>();
            // Создадим список функциональных ролей получателей уведомлений о задании
            var startNotificationTaskRoles = new List<object?>();

            if (notificationID is not null)
            {
                var rowID =
                    UpgradeHelper.AddTaskNotificationRow(
                        startTaskNotifications,
                        notificationID,
                        notificationName,
                        notificationExcludeDeputies,
                        notificationExcludeSubscribers,
                        notificationScript);

                UpgradeHelper.AddNotificationTaskRoles(
                    startNotificationTaskRoles,
                    CardFunctionRoles.PerformerID,
                    "$Enum_FunctionRoles_Performer",
                    null,
                    rowID);
            }
            // Запишем список уведомлений о задании
            actionStorage.Hash.Add(WorkflowActionTypes.NotificationsSectionName, startTaskNotifications);
            // Запишем список функциональных ролей получателей уведомлений о задании
            actionStorage.Hash.Add(WorkflowActionTypes.NotificationTaskRolesSectionName, startNotificationTaskRoles);

            // Удалим старые настройки уведомлений
            WorkflowEngineHelper.RemoveLastPath(actionStorage.Hash, MainSectionName, "Notification");
            WorkflowEngineHelper.RemoveLastPath(actionStorage.Hash, MainSectionName, "NotificationScript");
            WorkflowEngineHelper.RemoveLastPath(actionStorage.Hash, MainSectionName, "ExcludeDeputies");
            WorkflowEngineHelper.RemoveLastPath(actionStorage.Hash, MainSectionName, "ExcludeSubscribers");

            // Создадим список для настроек уведомлений вариантов завершений
            var optionsTaskNotifications = new List<object?>();
            // Создадим список функциональных ролей получателей уведомлений о завершений
            var optionsNotificationTaskRoles = new List<object?>();
            // Создадим список функциональных ролей, которым доступны варианты завершения
            var optionsTaskRoles = new List<object?>();
            // Создадим список ролей, получателей уведомлений по вариантам завершения
            var notificationReceiversRoles = new List<object?>();

            // Получим настройки вариантов завершения
            var optionRows = actionStorage.Hash.TryGet<IList>(KrUniversalTaskActionButtonsVirtual.SectionName);
            if (optionRows is not null)
            {
                var processRows = optionRows.Cast<Dictionary<string, object?>>();
                foreach (var row in processRows)
                {
                    var taskOptionRowID = WorkflowEngineHelper.Get<Guid>(row, "RowID");
                    var optionNotificationID = WorkflowEngineHelper.Get<object>(row, "Notification", "ID");
                    var optionNotificationName = WorkflowEngineHelper.Get<string>(row, "Notification", "Name") ?? string.Empty;
                    var optionExcludeDeputies = WorkflowEngineHelper.Get<object>(row, "ExcludeDeputies") ?? false;
                    var optionExcludeSubscribers = WorkflowEngineHelper.Get<object>(row, "ExcludeSubscribers") ?? false;
                    var optionSendToAuthor = WorkflowEngineHelper.Get<bool>(row, "SendToAuthor");
                    var optionSendToPerformer = WorkflowEngineHelper.Get<bool>(row, "SendToPerformer");
                    var optionNotificationScript = WorkflowEngineHelper.Get<string>(row, "NotificationScript");

                    if (optionNotificationID is not null)
                    {
                        // Добавим запись в список настроек уведомлений вариантов завершений
                        var rowID = UpgradeHelper.AddTaskActionCompletionNotificationRow(
                            optionsTaskNotifications,
                            "TaskButton",
                            taskOptionRowID,
                            optionNotificationID,
                            optionNotificationName,
                            optionNotificationScript,
                            optionExcludeDeputies,
                            optionExcludeSubscribers);

                        if (optionSendToAuthor)
                        {
                            UpgradeHelper.AddNotificationTaskRoles(
                                optionsNotificationTaskRoles,
                                CardFunctionRoles.AuthorID,
                                "$Enum_FunctionRoles_Author",
                                rowID,
                                null);
                        }

                        if (optionSendToPerformer)
                        {
                            UpgradeHelper.AddNotificationTaskRoles(
                                optionsNotificationTaskRoles,
                                CardFunctionRoles.PerformerID,
                                "$Enum_FunctionRoles_Performer",
                                rowID,
                                null);
                        }

                        var taskActionNotificationRolesRows =
                            actionStorage.Hash.TryGet<IList>(KrUniversalTaskActionNotificationRolesVitrual.SectionName)
                            ?.Cast<Dictionary<string, object?>>()
                            .Where(x =>
                                WorkflowEngineHelper.Get<Guid>(x!, "Button", "RowID") == taskOptionRowID)
                            .ToList();

                        if (taskActionNotificationRolesRows is not null)
                        {
                            foreach (var roleRow in taskActionNotificationRolesRows)
                            {
                                // Перенесём получателя уведомлений
                                var receiverRoleID = WorkflowEngineHelper.Get<Guid?>(roleRow, "Role", "ID");

                                if (receiverRoleID.HasValue)
                                {
                                    var receiverRoleName = WorkflowEngineHelper.Get<string>(roleRow, "Role", "Name") ?? string.Empty;

                                    AddTaskActionNotificationRolesRow(
                                        notificationReceiversRoles,
                                        receiverRoleID.Value,
                                        receiverRoleName,
                                        rowID);
                                }
                            }
                        }
                    }

                    // Добавим список функциональных ролей, которым доступен вариант
                    // В данном случае - только исполнитель
                    AddTaskActionCompletionOptionTaskRoleRow(
                        optionsTaskRoles,
                        CardFunctionRoles.PerformerID,
                        "$Enum_FunctionRoles_Performer",
                        taskOptionRowID);

                    // Удалим старые настройки уведомлений
                    WorkflowEngineHelper.RemoveLastPath(row, "Notification");
                    WorkflowEngineHelper.RemoveLastPath(row, "ExcludeDeputies");
                    WorkflowEngineHelper.RemoveLastPath(row, "ExcludeSubscribers");
                    WorkflowEngineHelper.RemoveLastPath(row, "SendToAuthor");
                    WorkflowEngineHelper.RemoveLastPath(row, "SendToPerformer");
                    WorkflowEngineHelper.RemoveLastPath(row, "NotificationScript");
                }

                // Удалим старую секцию получателей уведомлений по варианту завершения.
                WorkflowEngineHelper.RemoveLastPath(actionStorage.Hash, KrUniversalTaskActionNotificationRolesVitrual.SectionName);
            }

            // Запишем список для настроек уведомлений вариантов завершений
            actionStorage.Hash.Add(WorkflowActionTypes.CompletionNotificationsSectionName, optionsTaskNotifications);
            // Запишем список функциональных ролей получателей уведомлений о завершений
            if (!actionStorage.Hash.TryAdd(WorkflowActionTypes.NotificationTaskRolesSectionName, optionsNotificationTaskRoles))
            {
                // Ключ мог быть добавлен ранее.
                actionStorage.Hash.Get<List<object?>>(WorkflowActionTypes.NotificationTaskRolesSectionName)!.AddRange(optionsNotificationTaskRoles);
            }

            // Запишем список функциональных ролей, которым доступны варианты завершения
            actionStorage.Hash.Add(KrUniversalTaskActionButtonTaskRolesVirtual.SectionName, optionsTaskRoles);

            // Запишем список ролей, получателей уведомлений по вариантам завершения
            if (notificationReceiversRoles.Count != 0)
            {
                actionStorage.Hash.Add(WorkflowTaskActionBase.NotificationRolesSectionName, notificationReceiversRoles);
            }

            // Установим новую версию
            actionStorage.Version = 2;

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task UpgradeActionInstanceAsync(
            WorkflowActionStorage actionTemplateStorage,
            WorkflowActionStateStorage actionStateStorage,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            // Удалим из запущенного действия основную секцию и заполним тем, что в шаблоне.
            UpgradeHelper.ReplaceSection(actionTemplateStorage, actionStateStorage, MainSectionName);

            // Удалим из запущенного действия "KrUniversalTaskActionNotificationRolesVitrual" и перенесём "WeTaskActionNotificationRoles" секцию из шаблона.
            WorkflowEngineHelper.RemoveLastPath(actionStateStorage.Hash, KrUniversalTaskActionNotificationRolesVitrual.SectionName);
            var notificationSectionStorage = actionTemplateStorage.Hash.TryGet<object>(WorkflowTaskActionBase.NotificationRolesSectionName);
            actionStateStorage.Hash.Add(WorkflowTaskActionBase.NotificationRolesSectionName, notificationSectionStorage);

            // Перенесём из шаблона KrUniversalTaskActionButtonTaskRolesVirtual со списком ФР для вариантов завершения.
            var buttonTaskRolesStorage = actionTemplateStorage.Hash.TryGet<object>(KrUniversalTaskActionButtonTaskRolesVirtual.SectionName);
            actionStateStorage.Hash.Add(KrUniversalTaskActionButtonTaskRolesVirtual.SectionName, buttonTaskRolesStorage);

            // Установим новую версию
            actionStateStorage.Version = 2;

            return Task.CompletedTask;
        }

        #endregion
    }
}
