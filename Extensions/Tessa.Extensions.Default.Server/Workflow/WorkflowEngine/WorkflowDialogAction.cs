#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Extensions.Default.Server.Normalization;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess.ClientCommandInterpreter;
using Tessa.Platform;
using Tessa.Workflow;
using Tessa.Workflow.Actions;
using Tessa.Workflow.Normalization;

namespace Tessa.Extensions.Default.Server.Workflow.WorkflowEngine
{
    public sealed class WorkflowDialogAction(
        ICardRepository cardRepository,
        ISignatureProvider signatureProvider,
        IWorkflowEngineCardsScope cardsScope,
        Func<ICardTaskCompletionOptionSettingsBuilder> ctcBuilderFactory,
        ICardFileManager cardFileManager,
        WorkflowDialogManager workflowDialogManager) : Tessa.Workflow.Actions.WorkflowDialogAction(
        cardRepository,
        signatureProvider,
        cardsScope,
        ctcBuilderFactory,
        cardFileManager,
        workflowDialogManager)
    {
        #region Base Overrides

        /// <inheritdoc/>
        protected override Task StoreDialogWithoutTaskAsync(
            IWorkflowEngineContext context,
            Dictionary<string, object?> storeInfo)
        {
            if (!(context.ResponseInfo.TryGetValue(KrProcessSharedExtensions.KrProcessClientCommandInfoMark, out var commandsObj)
                    && commandsObj is IList commands))
            {
                commands = new List<object>();
                context.ResponseInfo[KrProcessSharedExtensions.KrProcessClientCommandInfoMark] = commands;
            }

            commands.Add(
                new KrProcessClientCommand(
                    DefaultCommandTypes.WeShowAdvancedDialog,
                    new Dictionary<string, object?>
                    {
                        [KrConstants.Keys.CompletionOptionSettings] = storeInfo,
                    }).GetStorage());

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        protected override WorkflowNormalizationSettings GetNormalizationSettings()
        {
            var settings = base.GetNormalizationSettings();
            settings.Settings.Add(
                new WorkflowNormalizationSetting(
                    DefaultNormalizationSources.TaskKinds,
                    DialogMainSection,
                    "TaskKind",
                    "Caption")
            );
            return settings;
        }

        #endregion
    }
}
