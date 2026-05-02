#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using Tessa.Extensions.Default.Shared;
using Tessa.Extensions.Platform.Client.Cards;
using Tessa.Platform;
using Tessa.UI;
using Tessa.UI.Cards;
using Tessa.UI.Controls;
using Tessa.UI.Views;

namespace Tessa.Extensions.Default.Client.Cards
{
    /// <summary>
    /// Действие по двойному клику для открытия карточек из списка состояний документа.
    /// </summary>
    public sealed class OpenFromDocStatesDoubleClickAction :
        OpenCardIntegerDoubleClickAction
    {
        #region Fields

        private readonly IAdvancedCardDialogManager advancedCardDialogManager;
        private readonly OnDoubleClickExtensionSettings? settings;

        #endregion
        
        #region Constructors

        public OpenFromDocStatesDoubleClickAction(
            IAdvancedCardDialogManager advancedCardDialogManager,
            OnDoubleClickExtensionSettings? settings)
        {
            this.advancedCardDialogManager = NotNullOrThrow(advancedCardDialogManager);
            this.settings = settings;
        }

        #endregion
        
        #region Base Overrides

        /// <inheritdoc />
        protected override async Task OpenCardAsync(
            long cardID,
            string displayValue,
            IUIContext context,
            ViewDoubleClickInfo info)
        {
            using ISplash splash = TessaSplash.Create(TessaSplashMessage.OpeningCard);
            await this.advancedCardDialogManager.OpenCardAsync(
                cardTypeID: DefaultCardTypes.KrDocStateTypeID,
                options: new OpenCardOptions
                {
                    DisplayValue = displayValue,
                    UIContext = context,
                    Splash = splash,
                    OpenInFullscreen = this.settings?.OpenInFullscreen ?? false,
                    ShowOnlyFirstTab = this.settings?.OpenOnlyFirstTab ?? false,
                    CardEditorModifierActionAsync = openingContext =>
                    {
                        if (this.settings?.RefreshViewOnClose == true)
                        {
                            var uiContext = openingContext.UIContext;
                            // Рефреш представления при закрытии карточки.
                            uiContext.CardEditor.Closed += async (sender, args) =>
                            {
                                if (uiContext.CardEditor.IsUpdatedServer)
                                {
                                    await context.ViewContext.RefreshViewAsync();
                                }
                            };
                        }

                        return ValueTask.CompletedTask;
                    },
                    Info = new Dictionary<string, object?>
                    {
                        [DefaultExtensionHelper.StateIDKey] = (int) cardID,
                    }
                });
        }

        #endregion
    }
}
