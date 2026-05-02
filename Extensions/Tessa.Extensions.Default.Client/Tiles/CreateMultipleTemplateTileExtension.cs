#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Tessa.Cards;
using Tessa.Extensions.Default.Shared;
using Tessa.Platform;
using Tessa.Platform.Collections;
using Tessa.Platform.Runtime;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.UI;
using Tessa.UI.Cards;
using Tessa.UI.Tiles;
using Tessa.UI.Tiles.Extensions;
using Tessa.Views;
using Tessa.Views.Metadata;

namespace Tessa.Extensions.Default.Client.Tiles
{
    public sealed class CreateMultipleTemplateTileExtension(
        ICardRepository cardRepository,
        ICardManager cardManager,
        ICardFileManager cardFileManager,
        ICardDialogManager dialogManager,
        CreateCardModelFuncAsync createCardModelFuncAsync,
        ICardMetadata cardMetadata,
        ISession session,
        IUIHost uiHost,
        IViewService viewService,
        IViewSpecialParameters viewSpecialParameters)
        : TileExtension
    {
        #region DirectoryEntry Private Class

        private sealed class DirectoryEntry : NamedEntry;

        #endregion

        #region DirectoryIterator Private Class

        private sealed class DirectoryIterator
        {
            #region Constructors

            public DirectoryIterator(List<DirectoryEntry> entries)
            {
                ThrowIfNull(entries);
                ThrowIf(entries, entries.Count == 0);

                this.entries = entries;
            }

            #endregion

            #region Fields

            private readonly List<DirectoryEntry> entries;

            private int index; // = 0

            #endregion

            #region Methods

            public DirectoryEntry GetNext()
            {
                var result = this.entries[this.index++];

                if (this.index == this.entries.Count)
                {
                    this.index = 0;
                }

                return result;
            }

            #endregion
        }

        #endregion

        #region Fields

        private readonly ICardRepository cardRepository = NotNullOrThrow(cardRepository);

        private readonly ICardManager cardManager = NotNullOrThrow(cardManager);

        private readonly ICardFileManager cardFileManager = NotNullOrThrow(cardFileManager);

        private readonly ICardDialogManager dialogManager = NotNullOrThrow(dialogManager);

        private readonly CreateCardModelFuncAsync createCardModelFuncAsync = NotNullOrThrow(createCardModelFuncAsync);

        private readonly ICardMetadata cardMetadata = NotNullOrThrow(cardMetadata);

        private readonly ISession session = NotNullOrThrow(session);

        private readonly IUIHost uiHost = NotNullOrThrow(uiHost);

        private readonly IViewService viewService = NotNullOrThrow(viewService);

        private readonly IViewSpecialParameters viewSpecialParameters = NotNullOrThrow(viewSpecialParameters);

        #endregion

        #region Private Constants

        private const string PartnersViewAlias = "Partners";

        private const string PartnersViewPrefix = "Partner";

        private const string UsersViewAlias = "Users";

        private const string UsersViewPrefix = "User";

        #endregion

        #region Private Methods

        private static bool TypeIsAllowedForMultipleCreation(CardType cardType)
        {
            return cardType.Flags.HasNot(CardTypeFlags.Hidden)
                && cardType.Flags.HasNot(CardTypeFlags.Singleton);
        }

        private static void GetTypeInfoForMultipleCreation(
            CardType cardType,
            out bool hasPartner,
            out bool hasAuthor)
        {
            var documentCommonInfo = cardType.SchemeItems
                .FirstOrDefault(x => x.SectionID == DefaultSchemeHelper.DocumentCommonInfoSectionID);

            if (documentCommonInfo is not null)
            {
                hasPartner = documentCommonInfo.ColumnIDList.Contains(DefaultSchemeHelper.PartnerComplexColumnID);
                hasAuthor = documentCommonInfo.ColumnIDList.Contains(DefaultSchemeHelper.AuthorComplexColumnID);
            }
            else
            {
                hasPartner = false;
                hasAuthor = false;
            }
        }

        private static bool CheckResponseAndSetID(CardNewResponse response)
        {
            response.Card.ID = Guid.NewGuid();
            return CheckResponse(response);
        }

        private static bool CheckResponse(CardResponseBase response)
        {
            var responseResult = response.Validate();
            TessaDialog.ShowNotEmpty(responseResult);
            if (!responseResult.IsSuccessful)
            {
                return false;
            }

            var result = response.ValidationResult.Build();
            TessaDialog.ShowNotEmpty(result);
            return result.IsSuccessful;
        }

        private static bool CheckResponseAndSetIDSilent(
            CardNewResponse response,
            IValidationResultBuilder validationResult)
        {
            if (response.Card.ID == Guid.Empty)
            {
                response.Card.ID = Guid.NewGuid();
            }

            return CheckResponseSilent(response, validationResult);
        }

        private static bool CheckResponseSilent(
            CardResponseBase response,
            IValidationResultBuilder validationResult)
        {
            var responseResult = response.Validate();
            validationResult.Add(responseResult);
            if (!responseResult.IsSuccessful)
            {
                return false;
            }

            var result = response.ValidationResult.Build();
            validationResult.Add(result);
            return result.IsSuccessful;
        }

        private async void CreateMultipleCardsActionAsync(object parameter)
        {
            var editor = UIContext.Current.CardEditor;

            ICardModel modelInEditor;
            CardType cardInTemplateType;
            CardTypeNamedForm? dialogForm;
            if (editor is null
                || (modelInEditor = editor.CardModel) is null
                || (cardInTemplateType = modelInEditor.TryGetCardInTemplateType()) is null
                || !(await this.cardMetadata.GetCardTypesAsync()).TryGetValue("Dialogs", out var dialogType)
                || (dialogForm = dialogType.Forms.FirstOrDefault(x => x.Name == "CreateMultipleCards")) is null)
            {
                return;
            }

            var request = new CardNewRequest { CardTypeID = dialogType.ID };
            var response = await this.cardRepository.NewAsync(request);
            if (!CheckResponseAndSetID(response))
            {
                return;
            }

            var model = await this.createCardModelFuncAsync(
                response.Card,
                response.SectionRows,
                this.dialogManager.ShowRowAsync);

            var mainFields = model.Card.Sections["Dialogs"].RawFields;
            GetTypeInfoForMultipleCreation(cardInTemplateType, out var hasPartner, out var hasAuthor);

            await this.uiHost.ShowFormDialogAsync(
                await LocalizeAsync(dialogForm.TabCaption),
                dialogForm,
                model,
                async (form, ct) =>
                {
                    var block = (form as IFormWithBlocksViewModel)?.Blocks.FirstOrDefault();
                    if (block is not null)
                    {
                        IControlViewModel? changePartnerControl = null;
                        if (!hasPartner && (changePartnerControl =
                                block.Controls.FirstOrDefault(x => x.Name == "ChangePartner")) is not null)
                        {
                            changePartnerControl.ControlVisibility = Visibility.Collapsed;
                        }

                        IControlViewModel? changeAuthorControl = null;
                        if (!hasAuthor && (changeAuthorControl =
                                block.Controls.FirstOrDefault(x => x.Name == "ChangeAuthor")) is not null)
                        {
                            changeAuthorControl.ControlVisibility = Visibility.Collapsed;
                        }

                        if (changePartnerControl is not null || changeAuthorControl is not null)
                        {
                            ((IFormWithBlocksViewModel) form).Rearrange();
                        }
                    }
                },
                buttons: new[]
                {
                    new UIButton(
                        "$UI_Cards_CreateMultipleTemplate_CreateCards",
                        async btn =>
                        {
                            var cardCount = mainFields.Get<int?>("CardCount");

                            if (cardCount > 0
                                && TessaDialog.Confirm(
                                    string.Format(
                                        await LocalizeNameAsync("UI_Cards_CreateMultipleTemplate_Confirm"),
                                        cardCount)))
                            {
                                var changePartner = hasPartner && mainFields.Get<bool>("ChangePartner");
                                var changeAuthor = hasAuthor && mainFields.Get<bool>("ChangeAuthor");

                                // карточка шаблона может быть асинхронно изменена, поэтому сначала клонируем её,
                                // а потом запускаем поток с операцией массового создания
                                var templateCard = modelInEditor.Card.Clone();

                                var tuple = await this.TryCreateMultipleCardsAsync(cardCount.Value, changePartner, changeAuthor, templateCard);
                                await btn.CloseAsync();

                                if (tuple is not null)
                                {
                                    TessaDialog.ShowNotEmpty(tuple.Item1);
                                    TessaDialog.ShowMessage(tuple.Item2);
                                }
                            }
                        },
                        isDefault: true,
                        isEnabledFunc: () => mainFields.Get<int?>("CardCount") > 0),
                    new UIButton("$UI_Common_Cancel", isCancel: true),
                });
        }

        private async Task<List<DirectoryEntry>?> TryLoadDirectoryAsync(
            string viewAlias,
            string entryPrefix,
            CancellationToken cancellationToken = default)
        {
            var view = await this.viewService.GetByNameAsync(viewAlias, cancellationToken).ConfigureAwait(false);
            if (view is null)
            {
                TessaDialog.ShowError(string.Format(await LocalizeNameAsync("UI_Cards_CreateMultipleTemplate_NoAccessToDictionary"), viewAlias));
                return null;
            }

            var viewMetadata = await view.GetMetadataAsync(cancellationToken);
            var viewRequest = new TessaViewRequest(viewMetadata.Alias);

            var currentPage = 1;
            var pageLimit = viewMetadata.ExportDataPageLimit;
            if (pageLimit <= 0)
            {
                pageLimit = ViewMetadata.DefaultExportDataPageLimit;
            }

            this.viewSpecialParameters.ProvidePageLimitParameter(viewRequest.Parameters, pageLimit);

            var result = new List<DirectoryEntry>();
            var entryIDIndex = -1;
            var entryNameIndex = -1;

            while (true)
            {
                this.viewSpecialParameters.ProvidePageOffsetParameter(viewRequest.Parameters, currentPage++, pageLimit);

                var viewResult = await view.GetDataAsync(viewRequest, cancellationToken).ConfigureAwait(false);
                if (viewResult.Rows.Count == 0)
                {
                    break;
                }

                if (entryIDIndex < 0)
                {
                    entryIDIndex = viewResult.GetColumnIndex($"{entryPrefix}ID");
                    if (entryIDIndex < 0)
                    {
                        return null;
                    }
                }

                if (entryNameIndex < 0)
                {
                    entryNameIndex = viewResult.GetColumnIndex($"{entryPrefix}Name");
                    if (entryNameIndex < 0)
                    {
                        return null;
                    }
                }

                foreach (var row in viewResult.Rows)
                {
                    string? name;
                    if (row.Count <= entryIDIndex
                        || row[entryIDIndex] is not Guid
                        || row.Count <= entryNameIndex
                        || (name = row[entryNameIndex] as string) is null)
                    {
                        return null;
                    }

                    var id = (Guid) row[entryIDIndex]!;
                    var entry = new DirectoryEntry { ID = id, Name = name };
                    result.Add(entry);
                }

                if (viewResult.Rows.Count < pageLimit)
                {
                    break;
                }
            }

            return result;
        }

        private async Task<Tuple<ValidationResult, string>?> TryCreateMultipleCardsAsync(
            int cardCount,
            bool changePartner,
            bool changeAuthor,
            Card templateCard,
            CancellationToken cancellationToken = default)
        {
            var successCount = 0;
            TimeSpan elapsed;
            ValidationResult result;

            using (var splash = TessaSplash.CreateLazy())
            {
                DirectoryIterator? partnerIterator = null;
                if (changePartner)
                {
                    splash.Text = "$UI_Cards_CreateMultipleTemplate_LoadingPartnersSplash";

                    var partners = await this.TryLoadDirectoryAsync(PartnersViewAlias, PartnersViewPrefix, cancellationToken).ConfigureAwait(false);
                    if (partners is { Count: > 0 })
                    {
                        partnerIterator = new DirectoryIterator(partners);
                    }
                }

                DirectoryIterator? userIterator = null;
                if (changeAuthor)
                {
                    splash.Text = "$UI_Cards_CreateMultipleTemplate_LoadingEmployeesSplash";

                    var users = await this.TryLoadDirectoryAsync(UsersViewAlias, UsersViewPrefix, cancellationToken).ConfigureAwait(false);
                    if (users is not null)
                    {
                        var systemIndex = users.IndexOf(x => x.ID == Session.SystemID);
                        if (systemIndex >= 0)
                        {
                            users.RemoveAt(systemIndex);
                        }

                        if (users.Count > 0)
                        {
                            userIterator = new DirectoryIterator(users);
                        }
                    }
                }

                var splashTemplate = await LocalizeNameAsync("UI_Cards_CreateMultipleTemplate_CreateCardSplash");
                var prevPercentage = -1;

                var validationResult = new ValidationResultBuilder();

                var stopwatch = new Stopwatch();
                stopwatch.Start();

                try
                {
                    for (var i = 0; i < cardCount; i++)
                    {
                        // i - количество уже созданных карточек, именно это количество надо отобразить пользователю
                        // поэтому выводим в сплэше i, а не i + 1
                        var percentage = (int) Math.Round(i * 100.0 / cardCount);
                        if (percentage != prevPercentage || (i % 100) == 0)
                        {
                            // изменяем текст при изменении процента или для каждой сотой записи
                            splash.Text = string.Format(splashTemplate, i, cardCount, percentage);
                            prevPercentage = percentage;
                        }

                        var (_, newResponse) = await CardUIHelper.CreateFromTemplateAsync(
                            templateCard, this.cardManager, cancellationToken: cancellationToken).ConfigureAwait(false);

                        var card = newResponse.Card;

                        // удаляем Permissions, которые могли быть неактуальными и вызывать предупреждения при валидации
                        card.Permissions = null;

                        if (!CheckResponseAndSetIDSilent(newResponse, validationResult)
                            || newResponse.CancelOpening)
                        {
                            // CancelOpening проверяем после того, как выведем сообщения об ошибках, если они были
                            break;
                        }

                        if ((partnerIterator is not null || userIterator is not null)
                            // ReSharper disable once PossibleNullReferenceException
                            // здесь card is not null, т.к. он мог быть равен null только в случае ошибок запроса,
                            // тогда метод CheckResponseAndSetIDSilent вернул бы false
                            && card.Sections.TryGetValue("DocumentCommonInfo", out var section))
                        {
                            var fields = section.RawFields;

                            if (partnerIterator is not null)
                            {
                                var partner = partnerIterator.GetNext();
                                fields["PartnerID"] = partner.ID;
                                fields["PartnerName"] = partner.Name;
                            }

                            if (userIterator is not null)
                            {
                                var author = userIterator.GetNext();
                                fields["AuthorID"] = author.ID;
                                fields["AuthorName"] = author.Name;
                            }
                        }

                        CardStoreResponse response;
                        await using (var container = await this.cardFileManager.CreateContainerAsync(card, cancellationToken: cancellationToken).ConfigureAwait(false))
                        {
                            response = await container.StoreAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                        }

                        if (!CheckResponseSilent(response, validationResult))
                        {
                            break;
                        }

                        successCount++;
                    }
                }
                finally
                {
                    stopwatch.Stop();
                }

                elapsed = stopwatch.Elapsed;
                result = validationResult.Build();
            }

            var finalMessage = new StringBuilder();

            finalMessage
                .Append(successCount == cardCount
                    ? string.Format(await LocalizeNameAsync("UI_Cards_CreateMultipleTemplate_StatisticsSucceeded"), cardCount)
                    : string.Format(await LocalizeNameAsync("UI_Cards_CreateMultipleTemplate_StatisticsFailed"), successCount, cardCount));

            if (elapsed > TimeSpan.Zero)
            {
                finalMessage
                    .AppendLine()
                    .AppendFormat(await LocalizeNameAsync("UI_Cards_CreateMultipleTemplate_TimeElapsed"), FormatTime(elapsed));
            }

            var elapsedPerCard = successCount > 0
                ? TimeSpan.FromMilliseconds(elapsed.TotalMilliseconds / successCount)
                : TimeSpan.Zero;

            if (elapsedPerCard > TimeSpan.Zero)
            {
                finalMessage
                    .AppendLine()
                    .AppendFormat(await LocalizeNameAsync("UI_Cards_CreateMultipleTemplate_AverageTimeToCreateCard"), elapsedPerCard.TotalMilliseconds);
            }

            return Tuple.Create(result, finalMessage.ToString());
        }

        #endregion

        #region Base Overrides

        public override Task InitializingLocal(ITileLocalExtensionContext context)
        {
            if (this.session.User.AccessLevel != UserAccessLevel.Administrator)
            {
                return Task.CompletedTask;
            }

            var panel = context.Workspace.LeftPanel;
            var editor = panel.Context.CardEditor;
            ICardModel model;
            CardType cardInTemplateType;
            ITile? cardTools;
            if (editor is not null
                && (model = editor.CardModel) is not null
                && model.CardType.ID == CardHelper.TemplateTypeID
                && (cardInTemplateType = model.TryGetCardInTemplateType()) is not null
                && TypeIsAllowedForMultipleCreation(cardInTemplateType)
                && (cardTools = panel.Tiles.TryGet(TileNames.CardTools)) is not null)
            {
                cardTools.Tiles.Add(
                    new Tile(
                        TileNames.CreateMultipleCards,
                        "$UI_Tiles_CreateMultipleCards",
                        context.Icons.Get("Thin64"),
                        panel,
                        new DelegateCommand(this.CreateMultipleCardsActionAsync),
                        TileGroups.Cards,
                        order: 100,
                        evaluating: (s, e) =>
                            e.SetIsEnabledWithCollapsing(
                                e.CurrentTile,
                                editor.CardModel is not null
                                && !editor.CardModel.InSpecialMode()
                                && editor.CardModel.Card.StoreMode == CardStoreMode.Update)));
            }

            return Task.CompletedTask;
        }

        #endregion
    }
}
