#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Extensions.Default.Shared;
using Tessa.Extensions.Default.Shared.AbTest;
using Tessa.Platform;
using Tessa.Platform.Collections;
using Tessa.Platform.Runtime;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.UI;
using Tessa.UI.Cards;
using Tessa.UI.Tiles;
using Tessa.UI.Tiles.Extensions;

namespace Tessa.Extensions.Default.Client.AbTest
{
    /// <summary>
    /// Плитки для карточки с настройками типового решения.
    /// </summary>
    public sealed class AbSettingsTileExtension(
        CreateDialogFormFuncAsync createDialogFormFuncAsync,
        IAdvancedCardDialogManager cardDialogManager,
        ICardRepository cardRepository,
        ISession session)
        : TileExtension
    {
        #region Fields

        private readonly CreateDialogFormFuncAsync createDialogFormFuncAsync = NotNullOrThrow(createDialogFormFuncAsync);

        private readonly IAdvancedCardDialogManager cardDialogManager = NotNullOrThrow(cardDialogManager);

        private readonly ICardRepository cardRepository = NotNullOrThrow(cardRepository);

        private readonly ISession session = NotNullOrThrow(session);

        #endregion

        #region Private Methods

        private void EnableOnSettingsCardAndAdministrator(object? sender, TileEvaluationEventArgs e) =>
            e.SetIsEnabledWithCollapsing(
                e.CurrentTile,
                e.CurrentTile.Context.CardEditor?.CardModel?.CardType.ID == DefaultCardTypes.KrSettingsTypeID
                && this.session.User.IsAdministrator());

        private async Task<ValidationResult> GenerateAsync(int userCount, int partnerCount)
        {
            using (TessaSplash.Create(TessaSplashMessage.CreatingMultipleCards))
            {
                var response = await this.cardRepository.RequestAsync(
                    new CardRequest
                    {
                        RequestType = AbRequestTypes.TestData,
                        Info =
                        {
                            { "UserCount", Int32Boxes.Box(userCount) },
                            { "PartnerCount", Int32Boxes.Box(partnerCount) }
                        }
                    });

                return response.ValidationResult.Build();
            }
        }

        private async Task GenerateButtonFuncAsync(CardSection section, Func<Task> closeActionAsync)
        {
            var rawFields = section.RawFields;
            var userCount = rawFields.Get<int?>("UserCount") ?? 0;
            if (userCount < 0)
            {
                TessaDialog.ShowError("$AbTest_Generator_WarnUserCountNegative");
                return;
            }

            var partnerCount = rawFields.Get<int?>("PartnerCount") ?? 0;
            if (partnerCount < 0)
            {
                TessaDialog.ShowError("$AbTest_Generator_WarnPartnerCountNegative");
                return;
            }

            if (userCount == 0 && partnerCount == 0)
            {
                TessaDialog.ShowMessage("$AbTest_Generator_WarnCardsCountNotDefined");
                return;
            }

            var text = userCount == 0
                ? $"{await LocalizeNameAsync("AbTest_Generator_CreatePartnersConfirmation")} {partnerCount}. {await LocalizeNameAsync("UI_Common_ContinueConfirmation")}"
                : partnerCount == 0
                    ? $"{await LocalizeNameAsync("AbTest_Generator_CreateUsersConfirmation")} {userCount}. {await LocalizeNameAsync("UI_Common_ContinueConfirmation")}"
                    : $"{await LocalizeNameAsync("AbTest_Generator_CreatePartnersConfirmation")} {partnerCount}.{Environment.NewLine}{await LocalizeNameAsync("AbTest_Generator_CreateUsersConfirmation")} {userCount}.\r\n{await LocalizeNameAsync("UI_Common_ContinueConfirmation")}";

            if (!TessaDialog.Confirm(text))
            {
                return;
            }

            if (userCount > 1000
                && !TessaDialog.Confirm(
                    $"{await LocalizeNameAsync("AbTest_Generator_WarnTooMuchUsers")} {userCount}{await LocalizeNameAsync("UI_Common_ContinueConfirmation")}",
                    "$UI_Common_Attention"))
            {
                return;
            }

            if (partnerCount > 1000
                && !TessaDialog.Confirm(
                    $"{await LocalizeNameAsync("AbTest_Generator_WarnTooMuchPartners")} {partnerCount}{await LocalizeNameAsync("UI_Common_ContinueConfirmation")}",
                    "$UI_Common_Attention"))
            {
                return;
            }

            await closeActionAsync();

            var result = await this.GenerateAsync(userCount, partnerCount);
            TessaDialog.ShowNotEmpty(result);
        }

        #endregion

        #region Command Actions

        private async void GenerateActionAsync(object parameter)
        {
            var (_, model) = await this.createDialogFormFuncAsync("AbCardGenerator", modifyResponseAsync: (response, ct) =>
            {
                var fields = response.Card.Sections["Table"].RawFields;
                fields["UserCount"] = Int32Boxes.Zero;
                fields["PartnerCount"] = Int32Boxes.Zero;
                return ValueTask.CompletedTask;
            });
            if (model is null)
            {
                return;
            }

            await this.cardDialogManager.ShowCardAsync(
                model,
                prepareEditorActionAsync: (editor, ct) =>
                {
                    editor.StatusBarIsVisible = false;

                    editor.Toolbar.Actions.AddRange(
                        new CardToolbarAction(
                            "CreateCards",
                            "$AbTest_Generator_CreateCardsButton",
                            editor.Toolbar.CreateIcon("Int426"),
                            new DelegateCommand(async _ =>
                            {
                                await this.GenerateButtonFuncAsync(
                                    editor.CardModel.Card.Sections["Table"],
                                    closeActionAsync: async () => await editor.CloseAsync(cancellationToken: CancellationToken.None));
                            }),
                            order: 1),
                        new CardToolbarAction(
                            TileNames.Cancel,
                            "$UI_Common_Cancel",
                            editor.Toolbar.CreateIcon("Int626"),
                            new DelegateCommand(async _ => await editor.CloseAsync(cancellationToken: CancellationToken.None)),
                            tooltip: "$UI_Common_Cancel",
                            order: 2));

                    editor.Context.SetDialogClosingAction((dialogContext, args) => TaskBoxes.False);
                    return new(true);
                },
                options: new ShowCardOptions
                {
                    DisplayValue = model.Card.TypeCaption,
                    WithTabControlBackground = true
                });
        }

        #endregion

        #region Base Overrides

        public override Task InitializingGlobal(ITileGlobalExtensionContext context)
        {
            var panel = context.Workspace.LeftPanel;
            panel.Tiles.Add(
                new Tile(
                    "AbGenerateTestCards",
                    TileHelper.SplitCaption("$AbTest_Generator_GenerateTestCards"),
                    context.Icons.Get("Thin1"),
                    panel,
                    new DelegateCommand(this.GenerateActionAsync),
                    TileGroups.Top,
                    order: 1,
                    verticalAlignment: TileVerticalAlignment.Bottom,
                    evaluating: this.EnableOnSettingsCardAndAdministrator));

            return Task.CompletedTask;
        }

        #endregion
    }
}
