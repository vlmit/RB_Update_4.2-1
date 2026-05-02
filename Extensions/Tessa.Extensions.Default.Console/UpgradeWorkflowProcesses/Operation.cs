using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Platform.ConsoleApps;
using Tessa.Workflow.Upgrade;

namespace Tessa.Extensions.Default.Console.UpgradeWorkflowProcesses
{
    public sealed class Operation : ConsoleOperation<OperationContext>
    {
        #region Fields

        private readonly IWorkflowEngineUpgradeManager upgradeManager;

        #endregion

        #region Constructors

        public Operation(
            IConsoleSessionManager sessionManager,
            IConsoleLogger logger,
            IWorkflowEngineUpgradeManager upgradeManager)
            : base(logger, sessionManager, extendedInitialization: true)
        {
            this.upgradeManager = NotNullOrThrow(upgradeManager);
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc />
        public override async Task<int> ExecuteAsync(OperationContext context, CancellationToken cancellationToken = default)
        {
            if (!this.SessionManager.IsOpened)
            {
                return -1;
            }

            List<Guid>? processesTemplateCardIDs;
            string startMessage;

            if (context.Identifiers is { Count: > 0 })
            {
                processesTemplateCardIDs = context.Identifiers!.ToList();
                switch (context.Mode)
                {
                    case Mode.Template:
                        startMessage =
                            "Upgrading workflow actions started, upgrading workflow actions in process versions for business process template cards with IDs = ";
                        break;
                    case Mode.Instance:
                        startMessage =
                            "Upgrading workflow actions started, upgrading workflow actions in process instances for business process template cards with IDs = ";
                        break;
                    case Mode.All:
                        startMessage =
                            "Upgrading workflow actions started, upgrading workflow actions in process versions and instances for business process template cards with IDs = ";
                        break;
                    default:
                        // Такого быть не должно, но пусть будет код ошибки, на всякий случай.
                        return -3;
                }

                startMessage += string.Join(";" + Environment.NewLine, context.Identifiers);
            }
            else
            {
                processesTemplateCardIDs = null;
                switch (context.Mode)
                {
                    case Mode.Template:
                        startMessage =
                            "Upgrading workflow actions started, upgrading workflow actions in all process templates.";
                        break;
                    case Mode.Instance:
                        startMessage =
                            "Upgrading workflow actions started, upgrading workflow actions in all process instances.";
                        break;
                    case Mode.All:
                        startMessage =
                            "Upgrading workflow actions started, upgrading workflow actions in all process templates and instances.";
                        break;
                    default:
                        // Такого быть не должно, но пусть будет код ошибки, на всякий случай.
                        return -3;
                }
            }

            await this.Logger.InfoAsync(startMessage);

            bool hasErrors;
            try
            {
                switch (context.Mode)
                {
                    case Mode.Template:
                        var templateResult = await this.upgradeManager.UpgradeProcessTemplatesForProcessCardsAsync(processesTemplateCardIDs, cancellationToken);
                        await this.Logger.LogResultAsync(templateResult);
                        hasErrors = templateResult.HasErrors;
                        break;
                    case Mode.Instance:
                        var instancesResult = await this.upgradeManager.UpgradeProcessInstancesForProcessCardsAsync(processesTemplateCardIDs, cancellationToken);
                        await this.Logger.LogResultAsync(instancesResult);
                        hasErrors = instancesResult.HasErrors;
                        break;
                    default:
                       var templateUpgradeResult = await this.upgradeManager.UpgradeProcessTemplatesForProcessCardsAsync(processesTemplateCardIDs, cancellationToken); 
                       await this.Logger.LogResultAsync(templateUpgradeResult);
                       var instancesUpgradeResult = await this.upgradeManager.UpgradeProcessInstancesForProcessCardsAsync(processesTemplateCardIDs, cancellationToken);
                       await this.Logger.LogResultAsync(instancesUpgradeResult);
                       hasErrors = templateUpgradeResult.HasErrors || instancesUpgradeResult.HasErrors;
                       break;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                await this.Logger.LogExceptionAsync("Error has occurred while upgrading workflow actions", e);
                return -4;
            }

            if (!hasErrors)
            {
                await this.Logger.InfoAsync("Workflow actions has been upgraded successfully.");
            }
            else
            {
                await this.Logger.InfoAsync("Workflow actions has been upgraded with errors.");
            }

            return 0;
        }

        #endregion
    }
}
