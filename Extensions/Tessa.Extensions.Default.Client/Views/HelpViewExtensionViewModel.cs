#nullable enable

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using Tessa.Cards;
using Tessa.Extensions.Default.Shared.Views.HelpExtensionConfigurator;
using Tessa.Platform.Runtime;
using Tessa.UI;
using Tessa.UI.Cards;
using Tessa.UI.Views.Content;

namespace Tessa.Extensions.Default.Client.Views
{
    /// <summary>
    /// Вью-модель кнопки справки.
    /// </summary>
    public sealed class HelpViewExtensionViewModel :
        BaseContentItem
    {
        #region Properties

        private readonly HelpViewExtensionSettings settings;
        private readonly IUIHost uiHost;
        private readonly IAdvancedCardDialogManager advancedCardDialogManager;
        private readonly ICardRepository cardRepository;
        private readonly ISession session;
        private readonly CreateDialogFormFuncAsync createDialogFormFuncAsync;
        private readonly ICardFileManager fileManager;
        private readonly IApplicationDescriptor? applicationDescriptor;

        #endregion

        #region Commands

        /// <summary>
        /// Команда открытия справки.
        /// </summary>
        public ICommand ShowHelpCommand { get; }

        #endregion

        #region Constructor

        public HelpViewExtensionViewModel(
            HelpViewExtensionSettings settings,
            IUIHost uiHost,
            IAdvancedCardDialogManager advancedCardDialogManager,
            ICardRepository cardRepository,
            ISession session,
            CreateDialogFormFuncAsync createDialogFormFuncAsync,
            ICardFileManager fileManager,
            IEnumerable<IPlaceArea> placeAreas,
            IApplicationDescriptor? applicationDescriptor = null,
            Func<IPlaceArea, DataTemplate>? dataTemplateFunc = null,
            int ordering = PlacementOrdering.BeforeAll)
            : base(placeAreas, dataTemplateFunc, ordering)
        {
            this.settings = NotNullOrThrow(settings);
            this.uiHost = NotNullOrThrow(uiHost);
            this.advancedCardDialogManager = NotNullOrThrow(advancedCardDialogManager);
            this.cardRepository = NotNullOrThrow(cardRepository);
            this.session = NotNullOrThrow(session);
            this.createDialogFormFuncAsync = NotNullOrThrow(createDialogFormFuncAsync);
            this.fileManager = NotNullOrThrow(fileManager);
            this.applicationDescriptor = applicationDescriptor;

            this.ShowHelpCommand = new DelegateCommand(this.ShowHelpActionAsync);
        }

        #endregion

        #region Private Methods

        private async void ShowHelpActionAsync(object _) =>
            await CardUIHelper.HandleHelpAsync(
                this.settings.HelpMode,
                this.settings.Value,
                this.uiHost,
                this.advancedCardDialogManager,
                this.cardRepository,
                this.session,
                this.createDialogFormFuncAsync,
                this.fileManager,
                this.applicationDescriptor);

        #endregion
    }
}
