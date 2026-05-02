#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.ComponentModel;
using Tessa.Extensions.Default.Server.Workflow.KrTaskManager;
using Tessa.Extensions.Default.Server.Workflow.KrTaskManager.Managers;
using Tessa.Extensions.Default.Shared;
using Tessa.Extensions.Default.Shared.Notices;
using Tessa.Extensions.Default.Shared.Workflow;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Notices;
using Tessa.Platform.Collections;
using Tessa.Platform.Data;
using Tessa.Platform.Runtime;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.Roles;
using Tessa.Views;
using Tessa.Views.Metadata;
using Tessa.Views.Metadata.Criteria;
using Tessa.Workflow.ApprovalProcess;
using Tessa.Workflow.ApprovalProcess.Nodes;
using Unity;

namespace Tessa.Extensions.Default.Server.Workflow.ApprovalProcess
{
    /// <summary>
    /// Обработчик узла "Согласование".
    /// </summary>
    /// <param name="session"><inheritdoc cref="ISession" path="/summary"/></param>
    /// <param name="viewService"><inheritdoc cref="IViewService" path="/summary"/></param>
    /// <param name="approvalTaskManager"><inheritdoc cref="IKrApprovalTaskManager" path="/summary"/></param>
    /// <param name="taskManagerContextFactory"><inheritdoc cref="IKrTaskManagerContextFactory" path="/summary"/></param>
    /// <param name="taskManagerDataProviderFactory"><inheritdoc cref="IKrTaskManagerDataProviderFactory" path="/summary"/></param>
    /// <param name="dbScope"><inheritdoc cref="IDbScope" path="/summary"/></param>
    /// <param name="notificationManager"><inheritdoc cref="INotificationManager" path="/summary"/></param>
    public sealed class KrApproverNodeHandler(
        ISession session,
        IViewService viewService,
        IKrApprovalTaskManager approvalTaskManager,
        IKrTaskManagerContextFactory taskManagerContextFactory,
        IKrTaskManagerDataProviderFactory taskManagerDataProviderFactory,
        IDbScope dbScope,
        [Dependency(NotificationManagerNames.DeferredWithoutTransaction)] INotificationManager notificationManager) : NodeHandlerBase
    {
        #region Nested types

        private record RoleInfo(int Type, string? Description, string? Parent);

        #endregion

        #region Fields

        private readonly ISession session = NotNullOrThrow(session);
        private readonly IViewService viewService = NotNullOrThrow(viewService);
        private readonly IKrApprovalTaskManager approvalTaskManager = NotNullOrThrow(approvalTaskManager);
        private readonly IKrTaskManagerContextFactory taskManagerContextFactory = NotNullOrThrow(taskManagerContextFactory);
        private readonly IKrTaskManagerDataProviderFactory taskManagerDataProviderFactory = NotNullOrThrow(taskManagerDataProviderFactory);
        private readonly IDbScope dbScope = NotNullOrThrow(dbScope);
        private readonly INotificationManager notificationManager = NotNullOrThrow(notificationManager);

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async ValueTask<NodeHandleResult> OnNodeStartedAsync(IApprovalProcessExecutionContext context)
        {
            ThrowIfNull(context.CurrentNode?.Data);

            var approvalNodeData = context.CurrentNode.Data.FromSerializedDictionary<ApproverNodeData>();
            if (approvalNodeData is not { Approvers.Count: > 0 })
            {
                return NodeHandleResult.NodeCompleted;
            }

            if (!await this.TryInitializeInitiatorAsync(context)
                || !await this.SendTasksForNodeAsync(
                    context,
                    approvalNodeData))
            {
                return NodeHandleResult.Faulted;
            }

            context.CurrentNode.Data = approvalNodeData.ToSerializedDictionary();

            return context.CurrentNodeTasks.Count == 0
                ? NodeHandleResult.NodeCompleted
                : NodeHandleResult.ContinuationRequired;
        }

        /// <inheritdoc/>
        public override async ValueTask<NodeHandleResult> OnNodeContinuedAsync(IApprovalProcessExecutionContext context)
        {
            ThrowIfNull(context.CurrentNode?.Data);

            if (context.CurrentNodeTasks.Count == 0)
            {
                return NodeHandleResult.NodeCompleted;
            }

            if (context.StoreCard is null
                || context.StoreCard.TryGetTasks() is not { Count: > 0 } tasks)
            {
                return NodeHandleResult.ContinuationRequired;
            }

            var approvalNodeData = context.CurrentNode.Data.FromSerializedDictionary<ApproverNodeData>();
            if (approvalNodeData is not { Approvers.Count: > 0 })
            {
                return NodeHandleResult.NodeCompleted;
            }

            var approvalContext = this.CreateApprovalContext(context, approvalNodeData);
            var dataProvider = this.CreateDataProvider(approvalContext.ExternalContext);
            bool? finishApproved = null;

            foreach (var task in tasks)
            {
                if (!context.CurrentNodeTasks.TryFirst(x => x.TaskID == task.RowID, out var taskInfo))
                {
                    continue;
                }

                var result = await this.approvalTaskManager.CompleteTaskAsync(
                    approvalContext,
                    dataProvider,
                    task);

                if (task.TypeID != DefaultTaskTypes.KrApproveTypeID)
                {
                    continue;
                }

                ApproverNodeCompletionState? completionState = null;
                if (task.OptionID == DefaultCompletionOptions.Approve)
                {
                    completionState = ApproverNodeCompletionState.Approved;
                }
                else if (task.OptionID == DefaultCompletionOptions.Disapprove)
                {
                    completionState = ApproverNodeCompletionState.Disapproved;
                }

                if (result == KrTaskManagerCompletionOptions.PositiveResult)
                {
                    finishApproved = true;
                }
                else if (result == KrTaskManagerCompletionOptions.NegativeResult)
                {
                    finishApproved = false;
                }

                if (completionState is not null)
                {
                    if (approvalNodeData.Approvers.TryFirst(x => x.TaskInfo?.TaskID == task.RowID, out var approver))
                    {
                        var userInfo = await this.GetRoleInfoAsync(this.session.User.ID, context.CancellationToken);
                        var isDeputy = task.TaskSessionRoles.Any(role => role.FunctionRoleID == CardFunctionRoles.PerformerID && role.IsDeputy);

                        approver.TaskInfo!.CompletionInfo = new()
                        {
                            Completed = context.ExecutionDateTime,
                            Comment = task.Card.Sections.GetOrAddEntry(KrConstants.KrTask.Name).RawFields.TryGet<string>(KrConstants.KrTask.Comment),
                            CompletedBy = new()
                            {
                                ID = this.session.User.ID,
                                Name = this.session.User.Name,
                                Type = RoleType.Personal,
                                Description = userInfo?.Description,
                                Parent = userInfo?.Parent
                            },
                            CompletionState = completionState.Value,
                            CompletedByDeputy = isDeputy
                        };
                    }
                }
            }

            context.CurrentNode.Data = approvalNodeData.ToSerializedDictionary();

            if (finishApproved is null)
            {
                return NodeHandleResult.ContinuationRequired;
            }

            return await FinishNodeAsync(
                context,
                approvalContext,
                finishApproved.Value);
        }

        /// <inheritdoc/>
        public override async ValueTask<NodeHandleResult> OnStoppedAsync(IApprovalProcessExecutionContext context)
        {
            ThrowIfNull(context.CurrentNode);

            if (context.CurrentNodeTasks.Count == 0
                || context.CurrentNode.Data.FromSerializedDictionary<ApproverNodeData>() is not { Approvers.Count: > 0 } approvalNodeData)
            {
                return NodeHandleResult.NodeCompleted;
            }

            var card = await context.GetCardAsync(forceLoadTasks: true);
            if (card is null)
            {
                return NodeHandleResult.Faulted;
            }

            var approvalContext = this.CreateApprovalContext(context, approvalNodeData);
            var dataProvider = this.CreateDataProvider(approvalContext.ExternalContext);

            foreach (var taskInfo in context.CurrentNodeTasks.ToArray())
            {
                await this.CancelTaskAsync(approvalContext, dataProvider, card, taskInfo.TaskID);
            }

            context.CurrentNode.Data = approvalNodeData.ToSerializedDictionary();

            return NodeHandleResult.NodeCompleted;
        }

        /// <inheritdoc/>
        public override async ValueTask<NodeHandleResult> OnNodeChangedAsync(IApprovalProcessExecutionContext context)
        {
            ThrowIfNull(context.CurrentNode?.Data);

            var approvalNodeData = context.CurrentNode.Data.FromSerializedDictionary<ApproverNodeData>();
            if (approvalNodeData is not { Approvers.Count: > 0 })
            {
                return NodeHandleResult.NodeCompleted;
            }

            var instance = await context.GetProcessInstanceAsync();
            if (instance is null)
            {
                return NodeHandleResult.Faulted;
            }

            var card = await context.GetCardAsync(forceLoadTasks: true);
            if (card is null)
            {
                return NodeHandleResult.Faulted;
            }

            var approvalContext = this.CreateApprovalContext(context, approvalNodeData);
            var dataProvider = this.CreateDataProvider(approvalContext.ExternalContext);

            if (instance.Process.Nodes.TryFirst(x => x.ID == context.CurrentNode.NodeID, out var newApprovalNode)
                && newApprovalNode.Data.FromSerializedDictionary<ApproverNodeData>() is { } newApprovalNodeData)
            {
                // Узел найден, сравниваем изменения
                // Обновляем данные узла
                approvalNodeData.Duration = newApprovalNodeData.Duration;
                approvalNodeData.Text = newApprovalNodeData.Text;

                List<ApproverNodeApprover> newApprovers = [.. newApprovalNodeData.Approvers];
                for (var i = approvalNodeData.Approvers.Count - 1; i >= 0; i--)
                {
                    var oldApprover = approvalNodeData.Approvers[i];
                    var newApproverIndex = newApprovalNodeData.Approvers.IndexOf(x => x.ID == oldApprover.ID);
                    if (newApproverIndex == -1)
                    {
                        await this.CancelTaskAsync(approvalContext, dataProvider, card, oldApprover.TaskInfo!.TaskID);
                        approvalNodeData.Approvers.RemoveAt(i);
                    }
                    else
                    {
                        newApprovalNodeData.Approvers.RemoveAt(newApproverIndex);
                    }
                }

                if (newApprovalNodeData.Approvers.Count > 0)
                {
                    if (!await this.SendTasksForNodeAsync(
                            context,
                            newApprovalNodeData))
                    {
                        return NodeHandleResult.Faulted;
                    }

                    foreach (var newApprover in newApprovalNodeData.Approvers)
                    {
                        var approver = newApprover.DeepClone();
                        approvalNodeData.Approvers.Add(approver);
                    }
                }

                newApprovalNodeData.Approvers = newApprovers;
                foreach (var newApprover in newApprovalNodeData.Approvers)
                {
                    newApprover.TaskInfo = null;
                }

                newApprovalNode.Data = newApprovalNodeData.ToSerializedDictionary();
                context.NotifyProcessChanged();

                approvalNodeData.Serialize(context.CurrentNode.Data);

                return context.CurrentNodeTasks.Count == 0
                    ? await FinishNodeAsync(
                        context,
                        approvalContext,
                        approvalNodeData.Approvers.All(x => x.TaskInfo?.CompletionInfo?.CompletionState == ApproverNodeCompletionState.Approved))
                    : NodeHandleResult.ContinuationRequired;
            }
            else
            {
                // Узел не найден, значит был удалён
                foreach (var taskInfo in context.CurrentNodeTasks.ToArray())
                {
                    await this.CancelTaskAsync(approvalContext, dataProvider, card, taskInfo.TaskID);
                }

                return NodeHandleResult.NodeCompleted;
            }
        }

        /// <inheritdoc/>
        public override ValueTask<bool> ValidateNodeIntegrityAsync(IApprovalProcessValidationContext context, ApprovalProcessNode node)
        {
            ThrowIfNull(context);
            ThrowIfNull(node);

            var approvalNodeData = node.Data?.FromSerializedDictionary<ApproverNodeData>();
            if (approvalNodeData is not { Approvers.Count: > 0 })
            {
                context.ValidationResult.AddError(
                    this,
                    "$ApprovalProcess_Validation_ApprovalNodeMustHaveApprover",
                    node.ID);
                return ValueTask.FromResult(false);
            }

            return ValueTask.FromResult(true);
        }

        /// <inheritdoc/>
        public override ValueTask<bool> ValidateNodeChangesAsync(
            IApprovalProcessValidationContext context,
            ApprovalProcessNode? node,
            ApprovalProcessNode? previousNode)
        {
            ThrowIfNull(context);

            if (node is null)
            {
                return ValueTask.FromResult(this.ValidateNodeDeleting(context, previousNode));
            }

            var approvalNodeData = node.Data?.FromSerializedDictionary<ApproverNodeData>();
            if (approvalNodeData is not { Approvers.Count: > 0 })
            {
                context.ValidationResult.AddError(
                    this,
                    "$ApprovalProcess_Validation_ApprovalNodeMustHaveApprover",
                    node.ID);
                return ValueTask.FromResult(false);
            }

            if (previousNode is null)
            {
                return ValueTask.FromResult(this.ValidateNodeCreating(context, approvalNodeData));
            }

            var previousNodeData = previousNode.Data?.FromSerializedDictionary<ApproverNodeData>();
            if (previousNodeData is not { Approvers.Count: > 0 })
            {
                context.ValidationResult.AddError(
                    this,
                    "$ApprovalProcess_Validation_ApprovalNodeMustHaveApprover",
                    node.ID);
                return ValueTask.FromResult(false);
            }

            var isRunning = context.ProcessState == ApprovalProcessState.Running;
            var result = true;
            var newApprovers = approvalNodeData.Approvers.ToList();
            var isCompletedNode = previousNodeData.Approvers.All(x => x.TaskInfo?.CompletionInfo is not null)
                || (!isRunning && previousNodeData.Approvers.All(x => x.TaskInfo is null || x.SkipOnCurrentCycle));
            var isActiveNode = !isCompletedNode && previousNodeData.Approvers.Any(x => x.TaskInfo is not null);

            if (isCompletedNode
                && isRunning
                && context.PreviousProcess is not null)
            {
                var fromEdge = context.Process.Edges.FirstOrDefault(x => x.Target == node.ID);
                var fromEdgePrevious = context.PreviousProcess.Edges.FirstOrDefault(x => x.Target == node.ID);

                if (fromEdge?.Source != fromEdgePrevious?.Source)
                {
                    context.ValidationResult.AddError(
                        this,
                        "$ApprovalProcess_Validation_UnableToEditEdge");
                    result = false;
                }
            }

            if (isRunning)
            {
                if (approvalNodeData.Text != previousNodeData.Text)
                {
                    context.ValidationResult.AddError(
                        this,
                        "$ApprovalProcess_Validation_PropertyChangingNotAllowed",
                        "$ApprovalProcess_Validation_PropertyChangingNotAllowed_Node",
                        nameof(ApproverNodeData.Text));
                    result = false;
                }

                if (approvalNodeData.Duration != previousNodeData.Duration)
                {
                    context.ValidationResult.AddError(
                        this,
                        "$ApprovalProcess_Validation_PropertyChangingNotAllowed",
                        "$ApprovalProcess_Validation_PropertyChangingNotAllowed_Node",
                        nameof(ApproverNodeData.Duration));
                    result = false;
                }
            }

            foreach (var previousApprover in previousNodeData.Approvers)
            {
                var approverIndex = newApprovers.IndexOf(x => x.ID == previousApprover.ID);
                if (approverIndex == -1)
                {
                    // Согласующий был удалён, запрещаем удаление, если он уже завершил задание
                    if (previousApprover.TaskInfo?.CompletionInfo is not null
                        && isRunning)
                    {
                        context.ValidationResult.AddError(
                            this,
                            "$ApprovalProcess_Validation_UnableToDeleteApprover");
                        result = false;
                    }

                    continue;
                }

                var approver = newApprovers[approverIndex];
                newApprovers.RemoveAt(approverIndex);

                if (isActiveNode)
                {
                    if (!previousApprover.Equals(approver))
                    {
                        context.ValidationResult.AddError(
                            this,
                            "$ApprovalProcess_Validation_UnableToChangeApprover");
                        result = false;
                    }
                }
                else
                {
                    // Флаг "Пропустить на текущем цикле" можно ставить только, если задание согласовано и процесс не запущен
                    if (previousApprover.SkipOnCurrentCycle != approver.SkipOnCurrentCycle
                        && (isRunning
                        || approver.TaskInfo?.CompletionInfo?.CompletionState != ApproverNodeCompletionState.Approved))
                    {
                        context.ValidationResult.AddError(
                            this,
                            "$ApprovalProcess_Validation_PropertyChangingNotAllowed",
                            "$ApprovalProcess_Validation_PropertyChangingNotAllowed_Node",
                            nameof(ApproverNodeApprover.SkipOnCurrentCycle));
                        result = false;
                    }

                    if (!(previousApprover.TaskInfo?.Equals(approver.TaskInfo) ?? approver.TaskInfo is null))
                    {
                        context.ValidationResult.AddError(
                            this,
                            "$ApprovalProcess_Validation_PropertyChangingNotAllowed",
                            "$ApprovalProcess_Validation_PropertyChangingNotAllowed_Node",
                            nameof(ApproverNodeApprover.TaskInfo));
                        result = false;
                    }
                }
            }

            if (newApprovers.Count > 0)
            {
                if (isCompletedNode
                    && isRunning)
                {
                    context.ValidationResult.AddError(
                        this,
                        "$ApprovalProcess_Validation_UnableToAddApprover");
                    result = false;
                }
                else
                {
                    foreach (var approver in newApprovers)
                    {
                        if (approver.TaskInfo is not null)
                        {
                            context.ValidationResult.AddError(
                                this,
                                "$ApprovalProcess_Validation_PropertyChangingNotAllowed",
                                "$ApprovalProcess_Validation_PropertyChangingNotAllowed_Node",
                                nameof(ApproverNodeApprover.TaskInfo));
                            result = false;
                        }
                    }
                }
            }

            return ValueTask.FromResult(result);
        }

        /// <inheritdoc/>
        public override async ValueTask ResetNodeAsync(
            IApprovalProcessExecutionContext context,
            ApprovalProcessNode node,
            bool isRevoke)
        {
            ThrowIfNull(context);
            ThrowIfNull(node);

            var approvalNodeData = node.Data.FromSerializedDictionary<ApproverNodeData>();
            if (approvalNodeData is not { Approvers.Count: > 0 })
            {
                return;
            }

            var instance = await context.GetProcessInstanceAsync();
            if (instance is null)
            {
                return;
            }

            var hasChanges = false;
            foreach (var approver in approvalNodeData.Approvers)
            {
                if (approver.TaskInfo is null
                    || approver.SkipOnCurrentCycle
                    && !isRevoke
                    && approver.TaskInfo.CompletionInfo is { CompletionState: ApproverNodeCompletionState.Approved })
                {
                    continue;
                }

                approver.TaskInfo = null;
                approver.SkipOnCurrentCycle = false;
                hasChanges = true;
            }

            if (hasChanges)
            {
                approvalNodeData.Serialize(node.Data!);
            }
        }

        #endregion

        #region Private Methods

        private async ValueTask<bool> SendTasksForNodeAsync(
            IApprovalProcessExecutionContext context,
            ApproverNodeData approvalNodeData)
        {
            var approvalContext = this.CreateApprovalContext(context, approvalNodeData);
            var dataProvider = this.CreateDataProvider(approvalContext.ExternalContext);

            await this.approvalTaskManager.StartAsync(approvalContext, dataProvider);

            return approvalContext.ValidationResult.IsSuccessful();
        }

        private async ValueTask CancelTaskAsync(
            IKrTaskManagerContext<KrApprovalProcessExternalContext> context,
            IKrApprovalTaskManagerDataProvider<KrApprovalProcessExternalContext> dataProvider,
            Card card,
            Guid taskID)
        {
            if (!card.Tasks.TryFirst(x => x.RowID == taskID, out var task)
                || task.TypeID != DefaultTaskTypes.KrApproveTypeID)
            {
                return;
            }

            task.Action = CardTaskAction.Complete;
            task.State = CardRowState.Deleted;
            task.OptionID = DefaultCompletionOptions.Revoke;

            await this.approvalTaskManager.CompleteTaskAsync(
                context,
                dataProvider,
                task);

            if (context.ExternalContext.NodeData.Approvers.TryFirst(x => x.TaskInfo?.TaskID == task.RowID, out var approver))
            {
                approver.TaskInfo = null;
            }
        }

        private bool ValidateNodeDeleting(
            IApprovalProcessValidationContext context,
            ApprovalProcessNode? previousNode)
        {
            if (context.ProcessState != ApprovalProcessState.Running
                || previousNode is null
                || previousNode.Data?.FromSerializedDictionary<ApproverNodeData>() is not { } previousNodeData)
            {
                return true;
            }

            var isCompletedNode = previousNodeData.Approvers.Any(x => x.TaskInfo?.CompletionInfo is not null);
            if (isCompletedNode)
            {
                context.ValidationResult.AddError(
                    this,
                    "$ApprovalProcess_Validation_UnableToDeleteNode");
                return false;
            }

            return true;
        }

        private IKrTaskManagerContext<KrApprovalProcessExternalContext> CreateApprovalContext(
            IApprovalProcessExecutionContext context,
            ApproverNodeData nodeData)
        {
            var krContext = new KrApprovalProcessExternalContext()
            {
                Context = context,
                NodeData = nodeData,
            };

            return this.taskManagerContextFactory.Create<IKrTaskManagerContext<KrApprovalProcessExternalContext>, KrApprovalProcessExternalContext>(
                krContext);
        }

        private IKrApprovalTaskManagerDataProvider<KrApprovalProcessExternalContext> CreateDataProvider(
            KrApprovalProcessExternalContext krContext) =>
            this.taskManagerDataProviderFactory.Create<IKrApprovalTaskManagerDataProvider<KrApprovalProcessExternalContext>, KrApprovalProcessExternalContext>(
                krContext,
                null,
                dataProvider =>
                {
                    dataProvider.CreateTaskActionAsync = (t, p, ct) => this.CreateTaskActionAsync(krContext, t, p, ct);
                    dataProvider.CompleteTaskActionAsync = (t, ct) => this.CompleteTaskActionAsync(krContext, t, ct);
                    dataProvider.DelegateTaskActionAsync = (ot, dt, role, _) => this.DelegateTaskActionAsync(krContext, ot, dt, role);
                });

        private async ValueTask CreateTaskActionAsync(
            KrApprovalProcessExternalContext context,
            CardTask task,
            IRoleUser? performer,
            CancellationToken cancellationToken)
        {
            if (performer is RoleEntryStorage roleEntryStorage
                && context.NodeData.Approvers.TryFirst(x => x.ID == roleEntryStorage.RowID, out var approver))
            {
                var instance = await context.Context.GetProcessInstanceAsync(cancellationToken: cancellationToken);
                if (instance is not null)
                {
                    approver.TaskInfo = new ApproverNodeTaskInfo()
                    {
                        Created = context.Context.ExecutionDateTime,
                        TaskID = task.RowID,
                        Cycle = instance.Settings.Cycle,
                    };
                }
            }

            await this.SendTaskNotificationAsync(
                context.Context,
                task);
        }

        private async ValueTask CompleteTaskActionAsync(
            KrApprovalProcessExternalContext krContext,
            CardTask task,
            CancellationToken cancellationToken)
        {
            if (task.State == CardRowState.Deleted)
            {
                krContext.Context.CurrentNodeTasks.RemoveAll(x => x.TaskID == task.RowID);
            }

            if (task.TypeID == DefaultTaskTypes.KrRequestCommentTypeID
                && task.OptionID == DefaultCompletionOptions.AddComment)
            {
                await CardComponentHelper.FillTaskAssignedRolesAsync(
                    task,
                    this.dbScope,
                    cancellationToken: cancellationToken);

                krContext.Context.ValidationResult.Add(
                    await this.notificationManager
                        .SendAsync(
                            DefaultNotifications.CommentNotification,
                            task.TaskAssignedRoles.Where(x => x.TaskRoleID == CardFunctionRoles.AuthorID).Select(x => x.RoleID).ToArray(),
                            new NotificationSendContext
                            {
                                MainCardID = krContext.Context.CardID,
                                TaskTypeID = task.TypeID,
                                GetCardFuncAsync = (validationResult, ct) =>
                                    krContext.Context.GetCardAsync(
                                        validationResult: validationResult,
                                        cancellationToken: ct),
                                Info = DefaultNotificationHelper.GetInfoWithTask(task),
                            },
                            cancellationToken));
            }
        }

        private async ValueTask DelegateTaskActionAsync(
            KrApprovalProcessExternalContext context,
            CardTask originalTask,
            CardTask delegatedTask,
            IRoleUser role)
        {
            if (context.NodeData.Approvers.TryFirst(x => x.TaskInfo?.TaskID == originalTask.RowID, out var approver))
            {
                approver.TaskInfo!.TaskID = delegatedTask.RowID;
                var newRoleInfo = await this.GetRoleInfoAsync(role.ID, context.Context.CancellationToken);
                if (newRoleInfo is not null)
                {
                    approver.TaskInfo!.DelegatedTo = new()
                    {
                        ID = role.ID,
                        Name = role.Name,
                        Type = (RoleType) newRoleInfo.Type,
                        Description = newRoleInfo.Description,
                        Parent = newRoleInfo.Parent
                    };
                }
            }

            await this.SendTaskNotificationAsync(
                context.Context,
                delegatedTask);
        }

        private async ValueTask<bool> TryInitializeInitiatorAsync(IApprovalProcessExecutionContext context)
        {
            var sCard = await context.GetSatelliteAsync(
                DefaultCardTypes.KrSatelliteTypeID,
                cancellationToken: context.CancellationToken);

            if (sCard is null)
            {
                return false;
            }

            var infoSection = sCard.GetApprovalInfoSection();
            if (infoSection.RawFields.TryGet<Guid?>(KrConstants.KrProcessCommonInfo.AuthorID) is null)
            {
                infoSection.Fields[KrConstants.KrProcessCommonInfo.AuthorID] = this.session.User.ID;
                infoSection.Fields[KrConstants.KrProcessCommonInfo.AuthorName] = this.session.User.Name;
            }

            return true;
        }

        private async Task<RoleInfo?> GetRoleInfoAsync(
            Guid roleID,
            CancellationToken cancellationToken = default)
        {
            if (await this.viewService.GetByNameAsync("ApprovalProcessEditorRoles", cancellationToken) is not { } view)
            {
                return null;
            }

            var request = new TessaViewRequest(view.Alias)
            {
                new RequestParameter("RoleID").Add(EqualsToCriteriaOperator.Instance, roleID)
            };

            var result = await view.GetDataAsync(request, cancellationToken);
            if (result.Rows.Count != 1)
            {
                return null;
            }

            var descriptionIndex = result.GetColumnIndex("Description");
            var parentIndex = result.GetColumnIndex("Parent");
            var typeIndex = result.GetColumnIndex("TypeID");

            if (descriptionIndex >= 0 && parentIndex >= 0 && typeIndex >= 0)
            {
                var description = (string?) result.Rows[0][descriptionIndex];
                var parent = (string?) result.Rows[0][parentIndex];
                var type = (short) result.Rows[0][typeIndex]!;

                return new(type, description, parent);
            }

            return null;
        }

        private static async ValueTask<NodeHandleResult> FinishNodeAsync(
            IApprovalProcessExecutionContext context,
            IKrTaskManagerContext<KrApprovalProcessExternalContext> approvalContext,
            bool isApproved)
        {
            if (isApproved)
            {
                return NodeHandleResult.NodeCompleted;
            }

            var instance = await context.GetProcessInstanceAsync();
            if (instance is null)
            {
                return NodeHandleResult.Faulted;
            }

            if (instance.Settings.ReturnAfterDisapproval)
            {
                if (instance.Settings.ChangeStateOnEnd)
                {
                    await approvalContext.SetStateAsync(
                        KrState.Disapproved,
                        context.ValidationResult,
                        context.CancellationToken);
                }

                instance.State = ApprovalProcessState.Disapproved;
                context.NotifyProcessChanged();
                return NodeHandleResult.ProcessFinished;
            }

            return NodeHandleResult.NodeCompleted;
        }

        private bool ValidateNodeCreating(IApprovalProcessValidationContext context, ApproverNodeData approvalNodeData)
        {
            if (approvalNodeData.Approvers.Any(x => x.TaskInfo is not null))
            {
                context.ValidationResult.AddError(
                    this,
                    "$ApprovalProcess_Validation_PropertyChangingNotAllowed",
                    "$ApprovalProcess_Validation_PropertyChangingNotAllowed_Node",
                    nameof(ApproverNodeApprover.TaskInfo));

                return false;
            }

            return true;
        }

        private async Task SendTaskNotificationAsync(
            IApprovalProcessExecutionContext context,
            CardTask task)
        {
            context.ValidationResult.Add(
                await this.notificationManager.SendAsync(
                    DefaultNotifications.TaskNotification,
                    task.TaskAssignedRoles
                        .Where(static x => x.TaskRoleID == CardFunctionRoles.PerformerID)
                        .Select(static x => x.RoleID)
                        .ToArray(),
                    new NotificationSendContext
                    {
                        MainCardID = context.CardID,
                        TaskTypeID = task.TypeID,
                        Info = DefaultNotificationHelper.GetInfoWithTask(task),
                        ModifyEmailActionAsync = async (email, _) =>
                        {
                            DefaultNotificationHelper.ModifyTaskCaption(
                                email,
                                task);
                        },
                        GetCardFuncAsync = (validationResult, ct) =>
                            context.GetCardAsync(
                                validationResult: validationResult,
                                cancellationToken: ct),
                    },
                    context.CancellationToken));
        }

        #endregion
    }
}

