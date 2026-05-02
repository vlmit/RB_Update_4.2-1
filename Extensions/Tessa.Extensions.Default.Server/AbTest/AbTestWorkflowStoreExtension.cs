using System;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.ComponentModel;
using Tessa.Cards.Extensions;
using Tessa.Cards.Workflow;
using Tessa.Extensions.Default.Server.Workflow;
using Tessa.Extensions.Default.Server.Workflow.KrProcess;
using Tessa.Extensions.Default.Shared.AbTest;
using Unity;

namespace Tessa.Extensions.Default.Server.AbTest
{
    public sealed class AbTestWorkflowStoreExtension(
        IKrTokenProvider krTokenProvider,
        [Dependency(CardRepositoryNames.Default)] ICardRepository cardRepositoryToCreateNextRequest,
        ICardRepository cardRepositoryToStoreNextRequest,
        ICardRepository cardRepositoryToCreateTasks,
        ICardTaskHistoryManager taskHistoryManager,
        ICardGetStrategy cardGetStrategy,
        IWorkflowQueueProcessor workflowQueueProcessor)
        : KrWorkflowStoreExtension(krTokenProvider,
            cardRepositoryToCreateNextRequest,
            cardRepositoryToStoreNextRequest,
            cardRepositoryToCreateTasks,
            taskHistoryManager,
            cardGetStrategy,
            workflowQueueProcessor)
    {
        #region Base Overrides

        protected override async ValueTask<bool> TaskIsAllowedAsync(CardTask task, ICardStoreExtensionContext context)
        {
            Guid taskTypeID = task.TypeID;
            return taskTypeID == AbTaskTypes.AbTask1TypeID
                || taskTypeID == AbTaskTypes.AbTask2TypeID;
        }

        protected override async ValueTask<bool> CanHandleQueueItemAsync(WorkflowQueueItem queueItem, ICardStoreExtensionContext context) =>
            AbTestProcessHelper.MainSubProcess == queueItem.Signal.ProcessTypeName;

        protected override ValueTask<bool> CanStartProcessAsync(Guid? processID, string processName, ICardStoreExtensionContext context)
        {
            switch (processName)
            {
                case AbTestProcessHelper.ProcessName:
                    return new ValueTask<bool>(true);

                default:
                    return new ValueTask<bool>(false);
            }
        }

        protected override Task StartProcessAsync(
            Guid? processID,
            string processName,
            IWorkflowWorker workflowWorker,
            CancellationToken cancellationToken = default)
        {
            switch (processName)
            {
                case AbTestProcessHelper.ProcessName:
                    return workflowWorker.StartProcessAsync(
                        AbTestProcessHelper.MainSubProcess,
                        newProcessID: processID,
                        cancellationToken: cancellationToken);

                default:
                    throw new ArgumentOutOfRangeException(nameof(processName), processName, null);
            }
        }

        protected override async ValueTask<IWorkflowWorker> CreateWorkerAsync(
            IWorkflowManager workflowManager,
            CancellationToken cancellationToken = default) =>
            new AbTestWorkflowWorker(workflowManager, this.CardRepositoryToCreateTasks);

        #endregion
    }
}
