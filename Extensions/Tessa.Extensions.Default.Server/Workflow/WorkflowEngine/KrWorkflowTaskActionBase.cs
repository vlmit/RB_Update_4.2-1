#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.ComponentModel;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Extensions.Default.Shared.Workflow.WorkflowEngine;
using Tessa.Notices;
using Tessa.Platform;
using Tessa.Platform.Data;
using Tessa.Platform.Storage;
using Tessa.Workflow;
using Tessa.Workflow.Actions;
using Tessa.Workflow.Actions.Descriptors;
using Tessa.Workflow.Compilation;
using Tessa.Workflow.Helpful;
using Tessa.Workflow.Storage;

namespace Tessa.Extensions.Default.Server.Workflow.WorkflowEngine
{
    /// <summary>
    /// Базовый класс для обработчиков действий маршрутов.
    /// </summary>
    public abstract class KrWorkflowTaskActionBase(WorkflowActionDescriptor descriptor, IWorkflowTaskActionDeps deps)
        : WorkflowTaskActionBase(descriptor, deps)
    {
        #region Base Overrides

        /// <inheritdoc/>
        public override void PrepareForExecute(WorkflowActionStateStorage actionState, IWorkflowEngineContext context)
        {
            ThrowIfNull(actionState);

            actionState.Hash.Remove(EventsSectionName);
            actionState.Hash.Remove(DialogsSectionName);
            actionState.Hash.Remove(DialogButtonsSectionName);
            actionState.Hash.Remove(DialogButtonLinksSectionName);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Асинхронно отправляет уведомление о завершении действия.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IWorkflowEngineContext" path="/summary"/></param>
        /// <param name="scriptObject"><inheritdoc cref="IWorkflowEngineCompiled" path="/summary"/></param>
        /// <param name="info">Информация по отправляемому уведомлению о завершении действия.</param>
        /// <param name="methodName">Имя метода с помощью которого выполняется модификация шаблона сообщения.</param>
        /// <param name="methodDescriptor">Дескриптор метода.</param>
        /// <returns>Асинхронная задача.</returns>
        protected async Task SendCompleteActionNotificationAsync(
            IWorkflowEngineContext context,
            IWorkflowEngineCompiled? scriptObject,
            WorkflowNotificationInfoBase info,
            string methodName,
            WorkflowActionMethodDescriptor methodDescriptor)
        {
            if (info is { NotificationID: not null, Cancel: false })
            {
                var roles = await info.GetRolesAsync();

                var cardID = context.ProcessInstance!.CardID;

                context.ValidationResult.Add(
                    await this.NotificationManager.SendAsync(
                        info.NotificationID!.Value,
                        roles,
                        new NotificationSendContext
                        {
                            MainCardID = cardID,
                            TaskTypeID = context.Task?.TypeID,
                            ExcludeDeputies = info.ExcludeDeputies,
                            Info = context.CreatePlaceholderInfoWithoutTask(clone: true),
                            ModifyEmailActionAsync = (email, ct) =>
                                scriptObject?.ExecuteActionAsync(methodName, methodDescriptor, email) ?? Task.CompletedTask,
                            DisableSubscribers = info.ExcludeSubscribers,
                            GetCardFuncAsync = (validationResult, ct) =>
                                this.CardsScope.GetCardAsync(
                                    cardID,
                                    validationResult,
                                    ct),
                        },
                        context.CancellationToken));
            }
        }

        /// <summary>
        /// Завершает дочерние задания указанных типов.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IWorkflowEngineContext" path="/summary"/></param>
        /// <param name="scriptObject"><inheritdoc cref="IWorkflowEngineCompiled" path="/summary"/></param>
        /// <param name="parentTaskRowID">Идентификатор родительского задания.</param>
        /// <param name="taskTypeIDs">Коллекция типов обрабатываемых заданий.</param>
        /// <param name="modifyActionAsync">Действие выполняемое перед завершением задания.</param>
        /// <returns>Асинхронная задача.</returns>
        protected async Task CompleteSubtasksAsync(
            IWorkflowEngineContext context,
            IWorkflowEngineCompiled? scriptObject,
            Guid parentTaskRowID,
            ICollection<Guid> taskTypeIDs,
            Func<CardTask, Task> modifyActionAsync)
        {
            var mainCard = await context.GetMainCardAsync(context.CancellationToken);

            if (mainCard is null)
            {
                return;
            }

            foreach (var task in mainCard.Tasks.Where(i => i.ParentRowID == parentTaskRowID && taskTypeIDs.Contains(i.TypeID)))
            {
                task.Action = CardTaskAction.Complete;

                await modifyActionAsync(task);
                await this.CompleteTaskAsync(context, scriptObject, task.RowID, true, task.OptionID);
            }
        }

        /// <summary>
        /// Создаёт дайджест задания на основе дайджеста указанного в настройках действия, комментария инициатора процесса согласования и дополнительного комментария.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IWorkflowEngineContext" path="/summary"/></param>
        /// <param name="baseDigest">Дайджест указанный в настройках действия.</param>
        /// <param name="additionalComment">Дополнительный комментарий.</param>
        /// <returns>Дайджест.</returns>
        /// <remarks>Для разделения частей дайджеста используется строка локализации "KrActions_TaskDigestPartsSeparator".</remarks>
        protected static async Task<string?> CreateDigestAsync(
            IWorkflowEngineContext context,
            string? baseDigest,
            string? additionalComment = null)
        {
            const int capacityDefault = 64;

            var sCard = await context.GetKrSatelliteAsync();

            if (sCard is null)
            {
                return null;
            }

            var authorComment = sCard
                .Sections[KrConstants.KrApprovalCommonInfo.Name]
                .TryGetRawFields()
                ?.TryGet<string>(KrConstants.KrApprovalCommonInfo.AuthorComment);

            var isNullOrEmptyAuthorComment = string.IsNullOrEmpty(authorComment);
            var isNullOrEmptyAdditionalComment = string.IsNullOrEmpty(additionalComment);

            baseDigest = await GetWithPlaceholdersAsync(
                context,
                baseDigest,
                context.Task);

            if (isNullOrEmptyAuthorComment
                && isNullOrEmptyAdditionalComment)
            {
                return baseDigest;
            }

            var sb = StringBuilderHelper.Acquire(capacityDefault);
            sb.Append(baseDigest);

            if (!isNullOrEmptyAuthorComment)
            {
                AppendDigestPartsSeparator(sb)
                .Append(authorComment);
            }

            if (!isNullOrEmptyAdditionalComment)
            {
                AppendDigestPartsSeparator(sb)
                .Append(additionalComment);
            }

            return sb.ToStringAndRelease();

            static StringBuilder AppendDigestPartsSeparator(StringBuilder sb)
            {
                if (sb.Length > 0)
                {
                    sb
                        .AppendLine()
                        .AppendLine("{$KrActions_TaskDigestPartsSeparator}");
                }

                return sb;
            }
        }

        /// <summary>
        /// Удаляет указанное задание.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IWorkflowEngineContext" path="/summary"/></param>
        /// <param name="taskRowID">Идентификатор удаляемого задания.</param>
        /// <returns>Асинхронная задача.</returns>
        /// <remarks>В реализации по умолчанию удаляет информацию о задании из истории заданий.</remarks>
        protected virtual async Task DeleteTaskCoreAsync(
            IWorkflowEngineContext context,
            Guid taskRowID)
        {
            // Удаляем запись из TaskHistory, если она уже есть
            await using (context.DbScope.Create())
            {
                var db = context.DbScope.Db;

                // запрос на удаление записи из истории заданий
                await db.SetCommand(
                        context.DbScope.BuilderFactory
                            .DeleteFrom("TaskHistory")
                            .Where().C("RowID").Equals().P("RowID")
                            .And().NotExists(b => b
                                .Select().Top(1).V(1).From("TaskHistory").NoLock()
                                .Where().C("ParentRowID").Equals().P("RowID")
                                .Limit(1))
                            .Build(),
                        db.Parameter("RowID", taskRowID))
                    .LogCommand()
                    .ExecuteNonQueryAsync(context.CancellationToken);
            }
        }

        /// <summary>
        /// Делегирует задание другому пользователю.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IWorkflowEngineContext" path="/summary"/></param>
        /// <param name="scriptObject"><inheritdoc cref="IWorkflowEngineCompiled" path="/summary"/></param>
        /// <param name="task">Делегируемое задание.</param>
        /// <param name="digest">Дайджест задания.</param>
        /// <param name="taskKindID">Идентификатор типа задания.</param>
        /// <param name="taskKindCaption">Название типа задания.</param>
        /// <returns>Асинхронная задача.</returns>
        protected async Task DelegateTaskAsync(
            IWorkflowEngineContext context,
            IWorkflowEngineCompiled? scriptObject,
            CardTask task,
            string? digest,
            Guid? taskKindID = null,
            string? taskKindCaption = null)
        {
            const int capacityDefault = 128;

            await context.TryRemoveActiveTaskAsync(
                task.RowID,
                context.ValidationResult,
                context.CancellationToken);

            var fields = task.Card.Sections[KrConstants.KrTask.Name].Fields;
            var result = StringBuilderHelper.Acquire(capacityDefault)
                .Append("{$ApprovalHistory_TaskIsDelegated} \"")
                .Append(fields.Get<string>(KrConstants.KrTask.DelegateName))
                .Append('"');
            var comment = fields.TryGet<string>(KrConstants.KrTask.Comment);

            if (string.IsNullOrWhiteSpace(comment))
            {
                comment = null;
            }
            else
            {
                result
                    .Append(". {$ApprovalHistory_Comment}: ")
                    .Append(comment);
            }

            task.Result = result.ToStringAndRelease();

            digest = await CreateDigestAsync(context, digest, comment);

            var delegatedTask = await context.SendTaskAsync(
                task.TypeID,
                digest,
                task.Planned,
                null,
                null,
                fields.Get<Guid>(KrConstants.KrTask.DelegateID),
                fields.Get<string>(KrConstants.KrTask.DelegateName),
                parentRowID: task.RowID,
                cancellationToken: context.CancellationToken);

            if (delegatedTask is null
                || !context.ValidationResult.IsSuccessful())
            {
                return;
            }

            await CardComponentHelper.FillTaskAssignedRolesAsync(task, context.DbScope, cancellationToken: context.CancellationToken);

            foreach (var author in task.TaskAssignedRoles.Where(x => x.TaskRoleID == CardFunctionRoles.AuthorID))
            {
                delegatedTask.AddAuthor(
                    author.RoleID,
                    author.RoleName,
                    author.Position);
            }

            delegatedTask.Info[CardHelper.TaskKindIDKey] = taskKindID;
            delegatedTask.Info[CardHelper.TaskKindCaptionKey] = taskKindCaption;

            if (task.Card.Sections.TryGetValue(KrConstants.KrAdditionalApprovalInfo.Name, out var oldSection))
            {
                var additionalApprovalInfoSection = new CardSection(KrConstants.KrAdditionalApprovalInfo.Name, oldSection.GetStorage())
                {
                    Type = CardSectionType.Table
                };

                foreach (var row in additionalApprovalInfoSection.Rows)
                {
                    // ReSharper disable AccessToStaticMemberViaDerivedType
                    row.Fields[KrConstants.KrAdditionalApprovalInfo.ID] = delegatedTask.RowID;
                    // ReSharper restore AccessToStaticMemberViaDerivedType
                    row.State = CardRowState.Inserted;
                }

                delegatedTask.Card.Sections[KrConstants.KrAdditionalApprovalInfo.Name].Set(additionalApprovalInfoSection);
            }

            context.AddTaskToNextContextTasks(delegatedTask);
            await context.AddActiveTaskAsync(
                delegatedTask.RowID,
                context.ValidationResult,
                context.CancellationToken);

            await this.DelegateTaskCoreAsync(
                context,
                scriptObject,
                task,
                delegatedTask);
        }

        #endregion

        #region Virtual Methods

        /// <summary>
        /// Делегирует задание другому пользователю.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IWorkflowEngineContext" path="/summary"/></param>
        /// <param name="scriptObject"><inheritdoc cref="IWorkflowEngineCompiled" path="/summary"/></param>
        /// <param name="originalTask">Делегируемое задание.</param>
        /// <param name="delegatedTask">Делегированное задание.</param>
        /// <returns>Асинхронная задача.</returns>
        protected virtual Task DelegateTaskCoreAsync(
            IWorkflowEngineContext context,
            IWorkflowEngineCompiled? scriptObject,
            CardTask originalTask,
            CardTask delegatedTask) => Task.CompletedTask;

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        protected override Task ExecuteAsync(
            IWorkflowEngineContext context,
            IWorkflowEngineCompiled? scriptObject)
        {
            context.CardsScope.CardStorePriorityComparer = WorkflowConstants.KrCardStorePriorityComparerDefault;

            return Task.CompletedTask;
        }

        #endregion
    }
}
