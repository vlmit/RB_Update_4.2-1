#nullable enable

using System.Threading;
using System.Threading.Tasks;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Scope;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.WorkflowProcesses;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess
{
    /// <inheritdoc cref="IWorkflowProcessRunner"/>
    public class KrRoutesProcessRunner(
        IKrProcessLauncher krProcessLauncher,
        IKrScope krScope) : IWorkflowProcessRunner
    {
        #region Fields

        private readonly IKrProcessLauncher krProcessLauncher = NotNullOrThrow(krProcessLauncher);
        private readonly IKrScope krScope = NotNullOrThrow(krScope);

        #endregion

        #region IProcessRunner Members

        /// <inheritdoc/>
        public async ValueTask StartProcessAsync(IWorkflowProcessRunnerContext context, CancellationToken cancellationToken = default)
        {
            ThrowIfNull(context);

            var process = KrProcessBuilder
                .CreateProcess()
                .SetProcess(context.ProcessID)
                .SetCard(context.CardID)
                .Build();

            var level = this.krScope.EnterNewLevel();
            try
            {
                var launchResult = await this.krProcessLauncher.LaunchAsync(
                    process,
                    specificParameters: new KrProcessServerLauncher.SpecificParameters()
                    {
                        RaiseErrorWhenExecutionIsForbidden = true,
                    },
                    cancellationToken: cancellationToken);

                context.ValidationResult.Add(launchResult.ValidationResult);
            }
            finally
            {
                await level.ExitAsync(context.ValidationResult);
            }
        }

        #endregion
    }
}
