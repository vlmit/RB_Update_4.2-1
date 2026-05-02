#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Tessa.BusinessCalendar;
using Tessa.Cards;
using Tessa.Platform;
using Tessa.Platform.Operations;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.UI;
using Tessa.UI.Cards;
using Tessa.UI.Cards.Controls;
using Tessa.UI.Cards.Controls.AutoComplete;
using Tessa.UI.Notifications;

// ReSharper disable AsyncVoidMethod

namespace Tessa.Extensions.Default.Client.UI
{
    public sealed class CalendarUIExtension(
        IBusinessCalendarService businessCalendarService,
        IOperationRepository operationRepository,
        INotificationUIManager notificationUIManager,
        IAdvancedCardDialogManager dialogManager,
        ICardRepository cardRepository)
        : CardUIExtension
    {
        #region Fields

        private readonly IBusinessCalendarService businessCalendarService = NotNullOrThrow(businessCalendarService);

        private readonly IOperationRepository operationRepository = NotNullOrThrow(operationRepository);

        private readonly INotificationUIManager notificationUIManager = NotNullOrThrow(notificationUIManager);

        private readonly IAdvancedCardDialogManager dialogManager = NotNullOrThrow(dialogManager);

        private readonly ICardRepository cardRepository = NotNullOrThrow(cardRepository);

        #endregion

        #region Command Actions

        private async void ValidateCalendarButtonActionAsync(object parameter)
        {
            var context = UIContext.Current;
            var editor = context.CardEditor;
            var cardID = editor.CardModel.Card.ID;

            var calendarID = editor.CardModel.Card.Sections[key: BusinessCalendarHelper.CalendarSettingsSection].RawFields.Get<int?>(key: "CalendarID");
            if (!calendarID.HasValue)
            {
                await TessaDialog.ShowErrorAsync(message: "$UI_Common_Messages_CantValidateCalendarWithEmptyID");
                return;
            }

            var result = await this.businessCalendarService.ValidateCalendarAsync(calendarCardID: cardID);
            TessaDialog.ShowNotEmpty(result: result, caption: "$UI_BusinessCalendar_ValidatedTitle");
        }

        private async void RebuildCalendarButtonActionAsync(object parameter)
        {
            var context = UIContext.Current;
            var editor = context.CardEditor;

            if (editor is { OperationInProgress: false })
            {
                var calendarID = editor.CardModel.Card.Sections[key: BusinessCalendarHelper.CalendarSettingsSection]
                    .RawFields.Get<int?>(key: "CalendarID");
                if (!calendarID.HasValue)
                {
                    await TessaDialog.ShowErrorAsync(message: "$UI_Common_Messages_CantCalcCalendarWithEmptyID");
                    return;
                }

                var isAlive = await this.operationRepository.IsAliveAsync(id: OperationTypes.CalculatingCalendar).ConfigureAwait(continueOnCapturedContext: false);
                if (isAlive)
                {
                    await TessaDialog.ShowErrorAsync(message: "$UI_Common_Messages_CalendarRebuildOperationAlreadyRunning");
                    return;
                }

                using (editor.SetOperationInProgress())
                {
                    var storeRequest = new CardStoreRequest
                    {
                        Card = editor.CardModel.Card.Clone(),
                        Info = { [key: BusinessCalendarHelper.RebuildMarkKey] = BooleanBoxes.True }
                    };

                    var storeResponse = await this.cardRepository.StoreAsync(request: storeRequest).ConfigureAwait(continueOnCapturedContext: false);
                    var storeResult = storeResponse.ValidationResult.Build();

                    Guid? operationID;
                    if (storeResult.IsSuccessful
                        && (operationID = storeResponse.Info.TryGet<Guid?>(key: BusinessCalendarHelper.RebuildOperationIDKey)).HasValue)
                    {
                        await context.CardEditor.OpenCardAsync(cardID: editor.CardModel.Card.ID, cardTypeID: editor.CardModel.Card.TypeID,
                            cardTypeName: editor.CardModel.Card.TypeName, context: context);
                        using (TessaSplash.Create(message: TessaSplashMessage.CalculatingCalendar))
                        {
                            do
                            {
                                await Task.Delay(millisecondsDelay: 500).ConfigureAwait(continueOnCapturedContext: false);
                            } while (await this.operationRepository.IsAliveAsync(id: operationID.Value).ConfigureAwait(continueOnCapturedContext: false));
                        }

                        var errorCardID = storeResponse.Info.Get<Guid?>(key: BusinessCalendarHelper.RebuildOperationErrorCardIDKey);
                        if (errorCardID.HasValue)
                        {
                            var errorCardRequest = new CardGetRequest
                            {
                                CardID = errorCardID,
                                CardTypeID = CardHelper.ErrorTypeID,
                                CardTypeName = CardHelper.ErrorTypeName
                            };

                            var errorCardRequestResult =
                                await this.cardRepository.GetAsync(request: errorCardRequest).ConfigureAwait(continueOnCapturedContext: false);

                            Card? errorCard;
                            var errorCardGetResult = errorCardRequestResult.ValidationResult.Build();
                            if (errorCardGetResult.IsSuccessful &&
                                (errorCard = errorCardRequestResult.TryGetCard()) is not null &&
                                errorCard.Sections.TryGetValue(key: "Errors", value: out var errorsSection))
                            {
                                await TessaDialog.ShowNotEmptyAsync(
                                    result: ValidationResult.FromText(text: errorsSection.RawFields.Get<string>(key: "Text"),
                                        type: ValidationResultType.Error));
                            }

                            if (errorCardGetResult.Items.Count != 1 ||
                                errorCardGetResult.Items[index: 0].Key != CardValidationKeys.InstanceNotFound)
                            {
                                await TessaDialog.ShowNotEmptyAsync(result: errorCardGetResult);
                            }
                        }
                    }

                    await TessaDialog.ShowNotEmptyAsync(result: storeResult);
                }

                await this.notificationUIManager.ShowTextOrMessageBoxAsync(text: "$UI_BusinessCalendar_CalendarIsRebuiltNotification")
                    .ConfigureAwait(continueOnCapturedContext: false);
            }
        }

        #endregion

        #region Private Methods

        private static void AttachCommandToButton(ICardUIExtensionContext context, string buttonAlias, Action<object> action)
        {
            if (!context.Model.Controls.TryGet(name: buttonAlias, viewModel: out var control))
            {
                return;
            }

            var button = (ButtonViewModel) control;
            if (!context.Model.Card.Permissions.Resolver.GetCardPermissions().Has(flag: CardPermissionFlags.AllowModify))
            {
                button.IsReadOnly = true;
                return;
            }

            button.CommandClosure.Execute = action;
        }

        private async Task OpenCalendarTypeInDialogAsync(ICardUIExtensionContext context, CancellationToken cancellationToken)
        {
            Guid? calendarTypeID;
            if (!context.Card.Sections.TryGetValue(key: "CalendarSettings", value: out var settingsSection) ||
                !settingsSection.RawFields.TryGetValue(key: "CalendarTypeID", value: out var calendarTypeIDObject) ||
                (calendarTypeID = calendarTypeIDObject as Guid?) is null)
            {
                return;
            }

            var calendarTypeName = settingsSection.RawFields.Get<string>(key: "CalendarTypeCaption");

            using var splash = TessaSplash.Create(message: TessaSplashMessage.OpeningCard);
            await this.dialogManager.OpenCardAsync(
                cardID: calendarTypeID,
                cardTypeID: null,
                options: new OpenCardOptions
                {
                    DisplayValue = calendarTypeName,
                    UIContext = context.UIContext,
                    Splash = splash,
                    CardEditorModifierActionAsync = async openingContext =>
                    {
                        var uiContext = UIContext.Current;

                        uiContext.CardEditor.Closed += async (sender, args) =>
                        {
                            var typeCard = uiContext.CardEditor.CardModel.Card;
                            if (uiContext.CardEditor.IsUpdatedServer)
                            {
                                var typeSection = typeCard.Sections[key: "CalendarTypes"];

                                var task =
                                    DispatcherHelper.InvokeInUIAsync(callback: async () =>
                                    {
                                        settingsSection.Fields[key: "CalendarTypeCaption"] = typeSection.RawFields[key: "Caption"];
                                        settingsSection.Fields[key: "CalendarTypeID"] = calendarTypeID;
                                    });
                                await task;
                            }
                        };
                    }
                },
                cancellationToken: cancellationToken);
        }

        #endregion

        #region Base Overrides

        public override async Task Initialized(ICardUIExtensionContext context)
        {
            AttachCommandToButton(context: context, buttonAlias: "ValidateCalendar", action: this.ValidateCalendarButtonActionAsync);
            AttachCommandToButton(context: context, buttonAlias: "RebuildCalendar", action: this.RebuildCalendarButtonActionAsync);

            if (!context.Model.Controls.TryGet(name: "CalendarType", viewModel: out var control))
            {
                return;
            }

            if (control is AutoCompleteEntryViewModel calendarTypeControl)
            {
                calendarTypeControl.ChangeFieldCommandClosure.Execute = async (o) =>
                    await this.OpenCalendarTypeInDialogAsync(context: context, cancellationToken: context.CancellationToken);
            }

            // Для всех новых записей в именованных интервалах, которые добавляются с клиента - ставим, что они добавлены вручную. 
            if (!context.Model.Controls.TryGet(name: "NamedRanges", viewModel: out var namedRangesControl))
            {
                return;
            }

            ((GridViewModel) namedRangesControl).RowInvoked +=
                (sender, args) =>
                {
                    if (args.Action == GridRowAction.Inserted)
                    {
                        args.Row.Fields[key: "IsManual"] = BooleanBoxes.True;
                    }
                };
        }

        #endregion
    }
}
