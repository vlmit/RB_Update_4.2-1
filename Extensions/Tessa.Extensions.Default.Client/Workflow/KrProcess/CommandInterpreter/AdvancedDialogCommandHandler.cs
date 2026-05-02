#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess.ClientCommandInterpreter;
using Tessa.Extensions.Platform.Client.UI;
using Tessa.Files;
using Tessa.Platform;
using Tessa.Platform.Runtime;
using Tessa.Platform.Validation;
using Tessa.UI;
using Tessa.UI.Cards;

namespace Tessa.Extensions.Default.Client.Workflow.KrProcess.CommandInterpreter
{
    /// <summary>
    /// Базовый класс обработчика клиентской команды отображения диалога.
    /// </summary>
    public abstract class AdvancedDialogCommandHandler : ClientCommandHandlerBase
    {
        #region Fields

        private readonly Func<IAdvancedCardDialogManager> createAdvancedCardDialogManagerFunc;

        private readonly ISession session;

        private readonly ICardMetadata cardMetadata;

        #endregion

        #region Constructor

        protected AdvancedDialogCommandHandler(
            Func<IAdvancedCardDialogManager> createAdvancedCardDialogManagerFunc,
            ISession session,
            ICardMetadata cardMetadata)
        {
            this.createAdvancedCardDialogManagerFunc = NotNullOrThrow(createAdvancedCardDialogManagerFunc);
            this.session = NotNullOrThrow(session);
            this.cardMetadata = NotNullOrThrow(cardMetadata);
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc />
        public override Task Handle(
            IClientCommandHandlerContext context)
        {
            var coSettings = this.PrepareDialogCommand(context);

            if (coSettings is null)
            {
                return Task.CompletedTask;
            }

            var editor = UIContext.Current.CardEditor;
            if (editor is not null)
            {
                this.SetDialogNonTaskCompletionOptionSettings(
                    editor,
                    editor,
                    coSettings,
                    context);
            }
            else
            {
                _ = this.ShowGlobalDialogAsync(
                    coSettings,
                    context);
            }

            return Task.CompletedTask;
        }

        #endregion

        #region Protected Abstract Methods

        /// <summary>
        /// Метод для подготовки диалога для выполнения.
        /// </summary>
        /// <param name="context">Контекст обработки клиентской команды.</param>
        /// <returns>Информация для формирования диалога, или значение <see langword="null"/>, если невозможно сформировать диалог.</returns>
        protected abstract CardTaskCompletionOptionSettings? PrepareDialogCommand(IClientCommandHandlerContext context);

        /// <summary>
        /// Метод выполнения диалога.
        /// </summary>
        /// <param name="actionResult">Результат выполнения диалога.</param>
        /// <param name="context">Контекст команды диалога.</param>
        /// <param name="cardEditor">Редактор карточки диалога.</param>
        /// <param name="parentCardEditor">Редактор карточки, для которой открывается диалог, если диалог открывается в рамках карточки.</param>
        /// <returns>Значение <see langword="true"/>, если необходимо закрыть диалог, иначе - <see langword="false"/>.</returns>
        protected abstract ValueTask<bool> CompleteDialogCoreAsync(
            CardTaskDialogActionResult actionResult,
            IClientCommandHandlerContext context,
            ICardEditorModel cardEditor,
            ICardEditorModel? parentCardEditor = null);

        #endregion

        #region Private Methods

        /// <summary>
        /// Отображает карточку в окне диалога.
        /// </summary>
        /// <param name="coSettings">Параметры диалога.</param>
        /// <param name="context">Контекст обработки клиентской команды.</param>
        /// <returns>Асинхронная задача.</returns>
        private async Task ShowGlobalDialogAsync(
            CardTaskCompletionOptionSettings coSettings,
            IClientCommandHandlerContext context)
        {
            var info = coSettings.Info;
            if (coSettings.PreparedNewCard is not null
                && coSettings.PreparedNewCardSignature is not null)
            {
                info[CardHelper.NewCardBilletKey] = coSettings.PreparedNewCard;
                info[CardHelper.NewCardBilletSignatureKey] = coSettings.PreparedNewCardSignature;
            }

            info[CardTaskDialogHelper.StoreMode] = Int32Boxes.Box((int) coSettings.StoreMode);

            await this.CreateNewCardAsync(
                advancedCardDialogManager: this.createAdvancedCardDialogManagerFunc(),
                completionOptionSettings: coSettings,
                info,
                cardModifierActionAsync: ctx =>
                {
                    this.SetDialogNonTaskCompletionOptionSettings(
                        ctx.Editor,
                        null,
                        coSettings,
                        context);
                    ctx.Editor.Info[CardTaskDialogUIExtension.OnlyConfigureDialogKey] = BooleanBoxes.True;

                    return ValueTask.CompletedTask;
                },
                cancellationToken: CancellationToken.None).ConfigureAwait(false);
        }

        private async Task CompleteDialogAsync(
            ICardEditorModel dialogCardEditor,
            ICardEditorModel? parentCardEditor,
            CardTaskCompletionOptionSettings coSettings,
            string? buttonName,
            bool completeDialog,
            IClientCommandHandlerContext context)
        {
            var closeDialog = false;
            using (dialogCardEditor.SetOperationInProgress(true))
            {
                var dialogCard = dialogCardEditor.CardModel.Card.Clone();
                var files = dialogCardEditor.CardModel.FileContainer.Files;

                var validationResult = await this.PrepareFilesForStoreAsync(
                    dialogCard,
                    new ReadOnlyCollection<IFile>(files));

                await TessaDialog.ShowNotEmptyAsync(validationResult);
                if (validationResult.HasErrors)
                {
                    return;
                }

                var actionResult = new CardTaskDialogActionResult
                {
                    MainCardID = Guid.Empty,
                    PressedButtonName = buttonName,
                    StoreMode = coSettings.StoreMode,
                    KeepFiles = coSettings.KeepFiles,
                    CompleteDialog = completeDialog,
                };
                actionResult.SetDialogCard(dialogCard);

                closeDialog = await this.CompleteDialogCoreAsync(
                    actionResult,
                    context,
                    dialogCardEditor,
                    parentCardEditor);
            }

            if (closeDialog)
            {
                await dialogCardEditor.CloseAsync();
            }
        }

        /// <summary>
        /// Асинхронно задаёт контент указанных файлов в соответствующие <see cref="CardInfoStorageObject.Info"/> карточки файлов.
        /// </summary>
        /// <param name="dialogCard">Карточка диалога.</param>
        /// <param name="files">Коллекция файлов.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Результат выполнения операции, агрегированный для всех файлов.</returns>
        private async Task<ValidationResult> PrepareFilesForStoreAsync(
            Card dialogCard,
            IReadOnlyCollection<IFile> files,
            CancellationToken cancellationToken = default)
        {
            var validationResults = new ValidationResultBuilder
            {
                await files.EnsureAllContentModifiedAsync(cancellationToken)
            };

            if (validationResults.IsSuccessful())
            {
                await CardTaskDialogHelper.SetFileContentToInfoAsync(
                    dialogCard,
                    files,
                    this.session,
                    validationResults,
                    cancellationToken);
            }

            return validationResults.Build();
        }

        /// <summary>
        /// Асинхронно создаёт и открывает карточку в диалоге. Карточка создаётся в режиме по умолчанию или по шаблону.
        /// </summary>
        /// <param name="advancedCardDialogManager"><inheritdoc cref="IAdvancedCardDialogManager" path="/summary"/></param>
        /// <param name="completionOptionSettings"><inheritdoc cref="CardTaskCompletionOptionSettings" path="/summary"/></param>
        /// <param name="info"><inheritdoc cref="CreateCardOptions.Info" path="/summary"/></param>
        /// <param name="cardModifierActionAsync"><inheritdoc cref="CreateCardOptions.CardModifierActionAsync" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns><inheritdoc cref="Task" path="/summary"/></returns>
        private async Task CreateNewCardAsync(
            IAdvancedCardDialogManager advancedCardDialogManager,
            CardTaskCompletionOptionSettings completionOptionSettings,
            Dictionary<string, object?>? info = null,
            CardEditorCreationActionAsync? cardModifierActionAsync = null,
            CancellationToken cancellationToken = default)
        {
            var dialogDisplayValue = completionOptionSettings.DisplayValue;
            if (completionOptionSettings.CardNewMethod == CardTaskDialogNewMethod.CardType
                && string.IsNullOrEmpty(dialogDisplayValue)
                && (await this.cardMetadata.GetCardTypesAsync(cancellationToken)).TryGetValue(completionOptionSettings.DialogTypeID, out var dialogType))
            {
                dialogDisplayValue = dialogType.Caption;
            }

            var createCardOptions = new CreateCardOptions
            {
                Info = info,
                DisplayValue = dialogDisplayValue,
                UIContext = UIContext.Current,
                CardModifierActionAsync = async ctx =>
                {
                    ctx.Editor.DialogName = completionOptionSettings.DialogName;

                    if (cardModifierActionAsync is not null)
                    {
                        await cardModifierActionAsync(ctx);
                    }
                },
                SaveCreationRequest = false,
                ShowOnlyFirstTab = completionOptionSettings.NotDisplayTabs,
            };

            switch (completionOptionSettings.CardNewMethod)
            {
                case CardTaskDialogNewMethod.CardType:
                    await advancedCardDialogManager.CreateCardAsync(
                        completionOptionSettings.DialogTypeID,
                        options: createCardOptions,
                        cancellationToken: cancellationToken).ConfigureAwait(false);
                    break;
                case CardTaskDialogNewMethod.Template:
                    await advancedCardDialogManager.CreateFromTemplateAsync(
                        completionOptionSettings.DialogTypeID,
                        createCardOptions,
                        cancellationToken).ConfigureAwait(false);
                    break;
                default:
                    throw ArgumentOutOfRange(completionOptionSettings.CardNewMethod);
            }
        }

        private void SetDialogNonTaskCompletionOptionSettings(
            ICardEditorModel currentCardEditor,
            ICardEditorModel? parentCardEditor,
            CardTaskCompletionOptionSettings coSettings,
            IClientCommandHandlerContext context)
        {
            currentCardEditor.Info[CardTaskDialogUIExtension.DialogNonTaskCompletionOptionSettingsKey] = coSettings;
            currentCardEditor.Info[CardTaskDialogUIExtension.OnDialogButtonPressedKey] =
                (Func<ICardEditorModel, CardTaskCompletionOptionSettings, string?, bool, ValueTask>) (
                    (
                        dialogCardEditor,
                        cos,
                        buttonName,
                        completeTask) => new(this.CompleteDialogAsync(
                            dialogCardEditor,
                            parentCardEditor,
                            cos,
                            buttonName,
                            completeTask,
                            context)));
        }

        #endregion

    }
}
