using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Extensions.Default.Shared.AbTest;
using Tessa.UI;
using Tessa.UI.Cards;
using Tessa.UI.Notifications;
using Tessa.UI.Tiles;
using Tessa.UI.Tiles.Extensions;

namespace Tessa.Extensions.Default.Client.AbTest
{
    public sealed class AbExternalSystemRequestTileExtension(
        ICardRepository cardRepository,
        INotificationUIManager notificationUIManager,
        CreateDialogFormFuncAsync createDialogFormFuncAsync,
        IAdvancedCardDialogManager cardDialogManager)
        : TileExtension
    {
        #region Fields

        private readonly ICardRepository cardRepository = NotNullOrThrow(cardRepository);

        private readonly IAdvancedCardDialogManager cardDialogManager = NotNullOrThrow(cardDialogManager);

        private readonly INotificationUIManager notificationUIManager = NotNullOrThrow(notificationUIManager);

        private readonly CreateDialogFormFuncAsync createDialogFormFuncAsync = NotNullOrThrow(createDialogFormFuncAsync);

        #endregion

        #region Private Methods

        private static void EnableForCarAndCanAdd(object sender, TileEvaluationEventArgs e)
        {
            ICardEditorModel editor = e.CurrentTile.Context.CardEditor;

            e.SetIsEnabledWithCollapsing(
                e.CurrentTile,
                editor is { CardModel: not null }
                && editor.CardModel.CardType.ID == AbCardTypes.AbCarTypeID
                && editor.CardModel.FileContainer.Permissions.CanAdd);
        }

        #endregion

        #region Command Actions

        private async void CommandActionAsync(object parameter) =>
            await AbExternalSystemHelper.RequestAndAddFileAsync(
                this.cardDialogManager,
                this.cardRepository,
                this.createDialogFormFuncAsync,
                this.notificationUIManager);

        #endregion

        #region Base Overrides

        public override Task InitializingGlobal(ITileGlobalExtensionContext context)
        {
            context.Workspace.LeftPanel.Tiles.Add(
                new Tile(
                    "FakeExternalSystem",
                    "$AbTest_CardTypes_Controls_RequestExternalSystem",
                    context.Icons.Get("Thin111"),
                    context.Workspace.LeftPanel,
                    new DelegateCommand(this.CommandActionAsync),
                    TileGroups.Cards,
                    order: 1000,
                    evaluating: EnableForCarAndCanAdd));

            return Task.CompletedTask;
        }

        #endregion
    }
}
