using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Tessa.Cards;
using Tessa.Extensions.Default.Shared;
using Tessa.Extensions.Default.Shared.Acquaintance;
using Tessa.Extensions.Default.Shared.Workflow.KrPermissions;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Extensions.Default.Shared.Workflow.Wf;
using Tessa.Platform;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.UI;
using Tessa.UI.Cards;
using Tessa.UI.Tiles;
using Tessa.UI.Tiles.Extensions;
using Tessa.Views.Metadata;
using Tessa.Views.Metadata.Criteria;

namespace Tessa.Extensions.Default.Client.Tiles
{
    public sealed class AcquaintanceTileExtension(
        IKrTypesCache krTypesCache,
        ICardMetadata cardMetadata,
        ICardRepository cardRepository,
        CreateCardModelFuncAsync createCardModelFuncAsync,
        ICardDialogManager dialogManager,
        IUIHost uiHost,
        IKrAcquaintanceManager acquaintanceManager)
        : TileExtension
    {
        #region Fields

        private readonly IKrTypesCache krTypesCache = NotNullOrThrow(krTypesCache);

        private readonly ICardMetadata cardMetadata = NotNullOrThrow(cardMetadata);

        private readonly ICardRepository cardRepository = NotNullOrThrow(cardRepository);

        private readonly CreateCardModelFuncAsync createCardModelFuncAsync = NotNullOrThrow(createCardModelFuncAsync);

        private readonly ICardDialogManager dialogManager = NotNullOrThrow(dialogManager);

        private readonly IUIHost uiHost = NotNullOrThrow(uiHost);

        private readonly IKrAcquaintanceManager acquaintanceManager = NotNullOrThrow(acquaintanceManager);

        #endregion

        #region Private Methods

        private static void EnableOnCardUpdateAndNotTaskCard(object sender, TileEvaluationEventArgs e)
        {
            ICardEditorModel editor = e.CurrentTile.Context.CardEditor;
            ICardModel model;

            e.SetIsEnabledWithCollapsing(
                e.CurrentTile,
                editor != null
                && (model = editor.CardModel) != null
                && model.Card.StoreMode == CardStoreMode.Update
                && model.CardType.ID != DefaultCardTypes.WfTaskCardTypeID);
        }

        private async ValueTask<bool> CanUseResolutionsAsync(ICardModel model, CancellationToken cancellationToken = default)
        {
            KrComponents components = await WfHelper.GetUsedComponentsAsync(this.krTypesCache, model.Card, cancellationToken);
            return components.Has(KrComponents.Resolutions);
        }

        /// <summary>
        /// Показывает окно с выбором списка ролей, которым нужно будет отправить ознакомление.
        /// </summary>
        /// <param name="obj"></param>
        private async void ShowAcquaintanceWindowAsync(object obj)
        {
            ICardEditorModel editor = UIContext.Current.CardEditor;
            if (editor == null)
            {
                return;
            }

            // Сперва сохраняем карточку
            ICardModel mainCardModel;
            if ((mainCardModel = editor.CardModel) != null)
            {
                bool cardIsNew = mainCardModel.Card.StoreMode == CardStoreMode.Insert;

                if (!cardIsNew && (await mainCardModel.HasChangesAsync() || NotEnoughPermissions(editor.CardModel)))
                {
                    await KrTileHelper.OpenMarkedCardAsync(
                        KrPermissionsHelper.CalculateResolutionPermissionsMark,
                        null, // Не требуем подтверждения действия, если не было изменений
                        () => true, // Автоматом подтверждаем сохранение
                        async () =>
                        {
                            // Нужно заново обратиться к модели в редакторе,
                            // т.к. ссылка mainCardModel уже не указывает на актуальный CardModel
                            if (NotEnoughPermissions(editor.CardModel))
                            {
                                TessaDialog.ShowNotEmpty(
                                    ValidationResult.FromText(
                                        this,
                                        KrPermissionsHelper.GetNotEnoughPermissionsErrorMessage(
                                            KrPermissionFlagDescriptors.CreateResolutions),
                                        ValidationResultType.Error));

                                return false;
                            }

                            await DispatcherHelper.InvokeInUIAsync(() =>
                                OpenRolesDialogAsync(
                                    editor,
                                    this.cardMetadata,
                                    this.cardRepository,
                                    this.dialogManager,
                                    this.uiHost,
                                    this.acquaintanceManager,
                                    this.createCardModelFuncAsync));

                            return true;
                        });
                }
                else
                {
                    OpenRolesDialogAsync(
                        editor,
                        this.cardMetadata,
                        this.cardRepository,
                        this.dialogManager,
                        this.uiHost,
                        this.acquaintanceManager,
                        this.createCardModelFuncAsync);
                }
            }
        }

        private static bool NotEnoughPermissions(ICardModel cardModel)
        {
            var krToken = KrToken.TryGet(cardModel.Card.Info);
            return krToken?.HasPermission(KrPermissionFlagDescriptors.CreateResolutions) == false;
        }

        private static async void OpenRolesDialogAsync(
            ICardEditorModel editor,
            ICardMetadata cardMetadata,
            ICardRepository cardRepository,
            ICardDialogManager dialogManager,
            IUIHost uiHost,
            IKrAcquaintanceManager acquaintanceManager,
            CreateCardModelFuncAsync createCardModelFuncAsync)
        {
            // Запрашиваем информируемых по умолчанию
            ICardModel model = editor.CardModel;
            if (model == null)
            {
                return;
            }

            var defaultRolesRequest = new CardRequest
            {
                RequestType = DefaultRequestTypes.GetDefaultAcquaintanceRoles,
                CardID = model.Card.ID,
            };

            CardResponse defaultRolesResponse = await cardRepository.RequestAsync(defaultRolesRequest);

            ValidationResult defaultRolesResult = defaultRolesResponse.ValidationResult.Build();
            TessaDialog.ShowNotEmpty(defaultRolesResult);
            if (!defaultRolesResult.IsSuccessful)
            {
                return;
            }

            // Построение диалога
            CardTypeNamedForm dialogForm;
            if (!(await cardMetadata.GetCardTypesAsync()).TryGetValue("Dialogs", out CardType dialogType) ||
                (dialogForm = dialogType.Forms.FirstOrDefault(x => x.Name == "Acquaintance")) == null)
            {
                return;
            }

            CardNewRequest request = new CardNewRequest { CardTypeID = dialogType.ID };
            CardNewResponse response = await cardRepository.NewAsync(request);
            if (!CheckResponseAndSetID(response))
            {
                return;
            }

            ICardModel windowCardModel = await createCardModelFuncAsync(
                response.Card,
                response.SectionRows,
                dialogManager.ShowRowAsync);

            // Получаем список ролей для ознакомления по умолчанию
            List<Tuple<Guid, string>> defaultRoles = AcquaintanceHelper.GetAcquaintanceDefaultRoles(defaultRolesResponse.Info);
            ListStorage<CardRow> rolesRows = windowCardModel.Card.Sections["DialogRoles"].Rows;

            foreach ((Guid roleID, string roleName) in defaultRoles)
            {
                CardRow roleRow = rolesRows.Add();
                roleRow.RowID = Guid.NewGuid();
                roleRow["RoleID"] = roleID;
                roleRow["RoleName"] = roleName;
                roleRow.State = CardRowState.None;
            }

            var windowClosure = new Window[] { null };

            // Показываем диалог
            await uiHost.ShowFormDialogAsync(
                await LocalizeAsync(dialogForm.TabCaption),
                dialogForm,
                windowCardModel,
                buttons:
                [
                    new UIButton(
                        "$UI_Common_OK",
                        isDefault: true,
                        buttonActionAsync: async btn =>
                        {
                            var contentElement = windowClosure[0]?.Content as UIElement;
                            if (contentElement != null)
                            {
                                contentElement.IsEnabled = false;
                            }

                            bool closed = false;
                            ValidationResult result;

                            try
                            {
                                if (rolesRows.Count == 0)
                                {
                                    TessaDialog.ShowNotEmpty(ValidationResult.FromText(
                                        "$KrMessages_Acquaintance_RolesRequired", ValidationResultType.Warning));
                                    return;
                                }

                                // Получаем комментарий
                                string comment = windowCardModel.Card.Sections["Dialogs"].Fields.Get<string>("Comment");

                                Card card = model.Card;
                                Guid cardID = card.ID;
                                Guid[] roleIDs = rolesRows.Select(x => x.Get<Guid>("RoleID")).ToArray();

                                using (TessaSplash.Create(TessaSplashMessage.ExecutingCommand))
                                {
                                    result = await acquaintanceManager.SendAsync(cardID, roleIDs, false, comment, addSuccessMessage: true);
                                }

                                if (result.IsSuccessful)
                                {
                                    closed = true;
                                    await btn.CloseAsync();
                                }
                            }
                            finally
                            {
                                if (!closed && contentElement != null)
                                {
                                    contentElement.IsEnabled = true;
                                }
                            }

                            TessaDialog.ShowNotEmpty(result);
                        }),
                    new UIButton("$UI_Common_Cancel", isCancel: true)
                ],
                initializeWindowActionAsync: async (window, ct) => windowClosure[0] = window);
        }

        private static bool CheckResponseAndSetID(CardNewResponse response)
        {
            response.Card.ID = Guid.NewGuid();
            return CheckResponse(response);
        }

        private static bool CheckResponse(CardResponseBase response)
        {
            ValidationResult responseResult = response.Validate();
            TessaDialog.ShowNotEmpty(responseResult);
            if (!responseResult.IsSuccessful)
            {
                return false;
            }

            ValidationResult result = response.ValidationResult.Build();
            TessaDialog.ShowNotEmpty(result);
            return result.IsSuccessful;
        }

        /// <summary>
        /// Открывает новую вкладку с представлением AcquaintanceHistory
        /// </summary>
        /// <param name="obj"></param>
        private async void OpenAcquaintanceHistoryViewAsync(object obj)
        {
            if (UIContext.Current.CardEditor is { OperationInProgress: false, CardModel: { } model })
            {
                await this.uiHost.ShowViewAsync(
                    "AcquaintanceHistory",
                    await LocalizeNameAsync("Views_AcquaintanceHistory"),
                    [new RequestParameter("CardIDParam").Add(EqualsToCriteriaOperator.Instance, model.Card.ID)]);
            }
        }

        #endregion

        #region Base Overrides

        public override Task InitializingGlobal(ITileGlobalExtensionContext context)
        {
            ITileContextSource contextSource = context.Workspace.LeftPanel;

            var tile = new Tile(
                TileNames.AcquaintanceGroup,
                TileHelper.SplitCaption("$KrTiles_AcquaintanceGroup"),
                context.Icons.Get("Thin83"),
                contextSource,
                DelegateCommand.Empty,
                TileGroups.Cards,
                order: 28,
                evaluating: EnableOnCardUpdateAndNotTaskCard,
                tiles:
                [
                    new Tile(
                        TileNames.Acquaintance,
                        TileHelper.SplitCaption("$KrTiles_Acquaintance"),
                        context.Icons.Get("Thin83"),
                        context.Workspace.LeftPanel,
                        new DelegateCommand(this.ShowAcquaintanceWindowAsync),
                        TileGroups.Cards,
                        order: 1,
                        evaluating: EnableOnCardUpdateAndNotTaskCard),


                    new Tile(
                        TileNames.AcquaintanceHistory,
                        TileHelper.SplitCaption("$KrTiles_AcquaintanceHistory"),
                        context.Icons.Get("Thin84"),
                        context.Workspace.LeftPanel,
                        new DelegateCommand(this.OpenAcquaintanceHistoryViewAsync),
                        TileGroups.Cards,
                        order: 2,
                        evaluating: EnableOnCardUpdateAndNotTaskCard)
                ]);

            tile.SetActionsGrouping();
            context.Workspace.LeftPanel.Tiles.Add(tile);

            return Task.CompletedTask;
        }


        public override async Task InitializingLocal(ITileLocalExtensionContext context)
        {
            ITilePanel panel = context.Workspace.LeftPanel;
            ICardEditorModel editor;
            ICardModel model;
            ITile acquaintanceGroup;

            if ((editor = panel.Context.CardEditor) != null
                && (model = editor.CardModel) != null
                && (acquaintanceGroup = panel.Tiles.TryGet(TileNames.AcquaintanceGroup)) != null
                && (!(await WfHelper.TypeSupportsWorkflowAsync(this.krTypesCache, model.CardType))
                    || !await this.CanUseResolutionsAsync(model)))
            {
                // дочерние плитки тоже скрываются
                acquaintanceGroup.DisableWithCollapsing();
            }
        }

        #endregion
    }
}
