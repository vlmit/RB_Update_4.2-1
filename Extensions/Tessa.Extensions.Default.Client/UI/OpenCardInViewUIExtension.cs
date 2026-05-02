#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Extensions.Default.Shared.Cards;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.UI;
using Tessa.UI.Cards;
using Tessa.UI.Cards.Controls;
using Tessa.UI.Controls;

namespace Tessa.Extensions.Default.Client.UI
{
    /// <summary>
    /// Реализация расширения типа карточки для добавления возможности
    /// открывать карточки из представления.
    /// </summary>
    public sealed class OpenCardInViewUIExtension(
        ICardMetadata cardMetadata,
        IUIHost uiHost,
        IAdvancedCardDialogManager cardDialogManager)
        : CardUIExtension
    {
        #region Fields

        private readonly ICardMetadata cardMetadata = NotNullOrThrow(cardMetadata);

        private readonly IUIHost uiHost = NotNullOrThrow(uiHost);

        private readonly IAdvancedCardDialogManager advancedCardDialogManager = NotNullOrThrow(cardDialogManager);

        #endregion

        #region Type extension methods

        /// <summary>
        /// Помощник применения функционала данного расширения.
        /// </summary>
        /// <param name="context">Контекст расширения типа.</param>
        /// <returns>Ассинхронная задача.</returns>
        private async Task ExecuteInitializedAsync(ITypeExtensionContext context)
        {
            if (context.ExternalContext is not ICardUIExtensionContext extensionContext)
            {
                return;
            }

            if (context.CardTask is null)
            {
                // действия для карточки
                await this.AttachDoubleClickHandlerAsync(
                    extensionContext.UIContext,
                    extensionContext.Model,
                    context.Settings,
                    cancellationToken: context.CancellationToken);
            }
            else
            {
                // действия для задания
                await extensionContext.Model.ModifyTasksAsync(async (task, model) =>
                {
                    if (task.TaskModel.CardTask == context.CardTask)
                    {
                        await task.ModifyWorkspaceAsync(async (t, subscribeToTaskModel) =>
                        {
                            await this.AttachDoubleClickHandlerAsync(
                                extensionContext.UIContext,
                                task.TaskModel,
                                context.Settings);
                        });
                    }
                });
            }
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Помощник подключения функционала открытия карточки по двойному клику.
        /// </summary>
        /// <param name="uiContext">Контекст операции с пользовательским интерфейсом.</param>
        /// <param name="cardModel">Модель карточки.</param>
        /// <param name="settings">Настройки расширения.</param>
        /// <param name="cancellationToken">Объект для отмены асинхронной операции.</param>
        /// <returns>Асинхронная задача.</returns>
        private async Task AttachDoubleClickHandlerAsync(
            IUIContext uiContext,
            ICardModel cardModel,
            ISerializableObject? settings,
            CancellationToken cancellationToken = default)
        {
            // получаем имя представления
            var viewControlName = settings?.TryGet<string>(DefaultCardTypeExtensionSettings.ViewControlAlias);
            if (string.IsNullOrEmpty(viewControlName))
            {
                return;
            }

            // пытаемся получить контрол по имени
            if (cardModel.Controls.TryGet<CardViewControlViewModel>(viewControlName) is not { } viewModel)
            {
                return;
            }

            // определяем колонку с идентификатором карточки
            var prefixReference = settings?.TryGet<string>(DefaultCardTypeExtensionSettings.ViewReferencePrefix)?.Trim();
            var mapping = CardControlHelper.TryGetCardReferenceMapping(viewModel, prefixReference);
            var referenceOpenMode = CardControlHelper.GetReferenceMode(settings, true);
            var dialogName = settings?.TryGet<string>(DefaultCardTypeExtensionSettings.CardDialogName);

            // присоединяем наш обработчик
            viewModel.DoubleClickCommand = new DelegateCommand(async p =>
            {
                if (referenceOpenMode is ReferenceOpenMode.None)
                {
                    return;
                }

                if (mapping is null)
                {
                    throw new InvalidOperationException(LocalizeFormat(
                        "$UI_Cards_TypesEditor_Exception_RefSectionInView",
                        prefixReference,
                        viewModel.CardTypeControl.ToString(),
                        viewModel.Block.CardTypeBlock.ToString(),
                        viewModel.Block.Form.CardTypeForm.ToString(),
                        viewModel.CardModel.CardType.ToString()));
                }

                await CardControlHelper.DoubleClickHandlerAsync(
                    uiContext,
                    this.uiHost,
                    this.advancedCardDialogManager,
                    (IViewClickInfo) p,
                    mapping,
                    referenceOpenMode,
                    dialogName,
                    viewModel.ParentControl,
                    cancellationToken);
            });
        }

        #endregion

        #region Base overrides

        /// <inheritdoc/>
        public override async Task Initialized(ICardUIExtensionContext context)
        {
            ValidationResult result = await CardHelper
                .ExecuteTypeExtensionsAsync(
                    DefaultCardTypeExtensionTypes.OpenCardInView,
                    context.Card,
                    this.cardMetadata,
                    this.ExecuteInitializedAsync,
                    context,
                    cancellationToken: context.CancellationToken);

            context.ValidationResult.Add(result);
        }

        #endregion
    }
}
