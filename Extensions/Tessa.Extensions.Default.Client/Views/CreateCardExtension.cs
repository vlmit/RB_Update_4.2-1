using System.Collections.Generic;
using Tessa.Cards;
using Tessa.Extensions.Default.Shared.Views;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform.Storage;
using Tessa.UI;
using Tessa.UI.Cards;
using Tessa.UI.Views;
using Tessa.UI.Views.Content;
using Tessa.UI.Views.Extensions;

namespace Tessa.Extensions.Default.Client.Views
{
    /// <remarks>
    /// У расширения есть конфигуратор <see cref="CreateCardExtensionConfigurator"/>
    /// </remarks>
    public sealed class CreateCardExtension(
        IUIHost uiHost,
        IAdvancedCardDialogManager advancedCardDialogManager,
        ICardRepository cardRepository,
        IIconContainer iconContainer,
        IKrTypesCache krTypesCache)
        : IWorkplaceViewComponentExtension, IWorkplaceExtensionSettingsRestore
    {
        #region Fields

        private readonly IUIHost uiHost = NotNullOrThrow(uiHost);

        private readonly IAdvancedCardDialogManager advancedCardDialogManager = NotNullOrThrow(advancedCardDialogManager);

        private readonly ICardRepository cardRepository = NotNullOrThrow(cardRepository);

        private readonly IIconContainer iconContainer = NotNullOrThrow(iconContainer);

        private readonly IKrTypesCache krTypesCache = NotNullOrThrow(krTypesCache);

        private CreateCardExtensionSettings settings = new();

        #endregion

        #region IWorkplaceViewComponentExtension Members

        /// <inheritdoc />
        public void Initialize(IWorkplaceViewComponent model) =>
            model.ContentFactories[nameof(CreateCardExtension)] = component =>
                new CreateCardExtensionViewModel(
                    this.settings,
                    this.uiHost,
                    this.advancedCardDialogManager,
                    this.cardRepository,
                    this.iconContainer,
                    this.krTypesCache,
                    component,
                    ContentPlaceAreas.ToolbarPlaces);


        /// <inheritdoc />
        public void Initialized(IWorkplaceViewComponent model)
        {
        }


        /// <inheritdoc />
        public void Clone(IWorkplaceViewComponent source, IWorkplaceViewComponent cloned, ICloneableContext context)
        {
        }

        /// <inheritdoc />
        public void Restore(Dictionary<string, object> metadata) =>
            this.settings = metadata.FromSerializedDictionary<CreateCardExtensionSettings>();

        #endregion
    }
}
