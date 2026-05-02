#nullable enable
using System;
using System.Collections.Generic;
using Tessa.Cards;
using Tessa.Platform.Storage;
using Tessa.UI;
using Tessa.UI.Cards;
using Tessa.UI.Views;
using Tessa.UI.Views.Content;
using Tessa.UI.Views.Extensions;

namespace Tessa.Extensions.Default.Client.Views
{
    /// <summary>
    /// Analogues to <see cref="CreateCardExtension"/> but instead of
    /// creation shiny new Card, creates a copy of currently selected Card in view.<br/>
    /// Extension has no settings.
    /// </summary>
    public sealed class CreateCardCopyExtension(
        IUIHost uiHost,
        Func<ICardEditorModel> createEditorFunc,
        ICardRepository cardRepository,
        ICardTemplateManager templateManager,
        IIconContainer iconContainer)
        : IWorkplaceViewComponentExtension, IWorkplaceExtensionSettingsRestore
    {
        #region Fields

        private readonly IUIHost uiHost = NotNullOrThrow(uiHost);

        private readonly Func<ICardEditorModel> createEditorFunc = NotNullOrThrow(createEditorFunc);

        private readonly ICardRepository cardRepository = NotNullOrThrow(cardRepository);

        private readonly ICardTemplateManager templateManager = NotNullOrThrow(templateManager);

        private readonly IIconContainer iconContainer = NotNullOrThrow(iconContainer);

        private CreateCardCopyExtensionSettings settings = new();

        #endregion

        #region IWorkplaceViewComponentExtension Members

        /// <inheritdoc />
        public void Initialize(IWorkplaceViewComponent model) =>
            model.ContentFactories[nameof(CreateCardCopyExtension)] = component =>
                new CreateCardCopyExtensionViewModel(
                    this.settings,
                    this.uiHost,
                    this.createEditorFunc,
                    this.cardRepository,
                    this.templateManager,
                    this.iconContainer,
                    component,
                    ContentPlaceAreas.ToolbarPlaces);

        /// <inheritdoc />
        public void Initialized(IWorkplaceViewComponent? model)
        {
        }

        /// <inheritdoc />
        public void Clone(IWorkplaceViewComponent source, IWorkplaceViewComponent cloned, ICloneableContext context)
        {
        }

        #endregion

        #region IWorkplaceExtensionSettingsRestore Members

        /// <inheritdoc />
        public void Restore(Dictionary<string, object?> metadata) =>
            this.settings = metadata.FromSerializedDictionary<CreateCardCopyExtensionSettings>();

        #endregion
    }
}
