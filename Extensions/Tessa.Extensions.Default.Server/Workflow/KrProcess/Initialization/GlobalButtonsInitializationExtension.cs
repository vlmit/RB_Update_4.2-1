using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Tessa.Extensions.Default.Server.Workflow.KrCompilers;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform;
using Tessa.Platform.Initialization;
using Tessa.Platform.Validation;
using Tessa.Workflow;
using static Tessa.Extensions.Default.Shared.Workflow.KrProcess.KrConstants;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Initialization
{
    public sealed class GlobalButtonsInitializationExtension : ServerInitializationExtension
    {
        #region Fields

        private readonly IKrProcessButtonVisibilityEvaluator buttonVisibilityEvaluator;

        #endregion

        #region Constructors

        public GlobalButtonsInitializationExtension(
            IKrProcessButtonVisibilityEvaluator buttonVisibilityEvaluator)
        {
            this.buttonVisibilityEvaluator = buttonVisibilityEvaluator ?? throw new ArgumentNullException(nameof(buttonVisibilityEvaluator));
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async Task AfterRequest(IServerInitializationExtensionContext context)
        {
            if (!context.RequestIsSuccessful)
            {
                return;
            }

            var innerValidationResult = new ValidationResultBuilder();
            var buttonEvaluationContext = new KrProcessButtonVisibilityEvaluatorContext(
                innerValidationResult,
                context.CancellationToken);

            var evaluatedButtons = await this.buttonVisibilityEvaluator.EvaluateGlobalButtonsAsync(buttonEvaluationContext);

            foreach (var item in innerValidationResult)
            {
                context.ValidationResult.Add(item.Type == ValidationResultType.Error
                    ? new ValidationResultItem(item, ValidationResultType.Warning)
                    : item);
            }

            var groups = evaluatedButtons.GroupBy(p => p.TileGroup);
            var tileInfos = new List<KrTileInfo>(evaluatedButtons.Count);
            foreach (var group in groups)
            {
                if (string.IsNullOrWhiteSpace(group.Key))
                {
                    tileInfos
                        .AddRange(group.Select(ConvertToTileInfo));
                }
                else
                {
                    var nested = group.Select(ConvertToTileInfo).ToList();
                    var tiles = new List<KrTileInfo>(nested.Where(o => o.IsGlobal));

                    var hidden = tiles.All(y => y.Hidden);
                    var showGroupOnToolBar = !hidden && tiles.Any(y => y.ToolbarVisibilityMode != ButtonToolbarVisibilityMode.DoNotShow);

                    var globalGroupTile = new KrTileInfo(
                        id: Guid.Empty,
                        name: string.Empty,
                        caption: group.Key,
                        icon: Ui.DefaultTileGroupIcon,
                        tooltip: string.Empty,
                        isGlobal: true,
                        askConfirmation: false,
                        confirmationMessage: string.Empty,
                        actionGrouping: false,
                        buttonHotkey: null,
                        order: 0,
                        nestedTiles: tiles.OrderByLocalized(p => p.Caption),
                        handlerID: null,
                        toolbarVisibilityMode: showGroupOnToolBar
                            ? ButtonToolbarVisibilityMode.Show
                            : ButtonToolbarVisibilityMode.DoNotShow,
                        alias: null,
                        hidden: hidden);
                    tileInfos.Add(globalGroupTile);
                }
            }

            context.Response.SetGlobalTiles(tileInfos.OrderByLocalized(p => p.Caption).ToList());
        }

        #endregion

        #region private

        private static KrTileInfo ConvertToTileInfo(
            IKrProcessButton button)
        {
            return new KrTileInfo(
                button.ID,
                button.Name,
                button.Caption,
                button.Icon,
                button.Tooltip,
                button.IsGlobal,
                button.AskConfirmation,
                button.ConfirmationMessage,
                button.ActionGrouping,
                button.ButtonHotkey,
                button.Order,
                ImmutableList<KrTileInfo>.Empty,
                button.HandlerID,
                button.ToolbarVisibilityMode,
                button.Alias,
                button.Hidden);
        }

        #endregion
    }
}
