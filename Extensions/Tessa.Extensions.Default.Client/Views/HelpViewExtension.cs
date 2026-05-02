#nullable enable

using System.Collections.Generic;
using Tessa.Cards;
using Tessa.Extensions.Default.Shared.Views.HelpExtensionConfigurator;
using Tessa.Platform.Runtime;
using Tessa.Platform.Storage;
using Tessa.UI;
using Tessa.UI.Cards;
using Tessa.UI.Views;
using Tessa.UI.Views.Content;
using Tessa.UI.Views.Extensions;
using Unity;

namespace Tessa.Extensions.Default.Client.Views
{
    /// <summary>
    /// Расширение для добавления кнопки со справкой для представления.
    /// </summary>
    public sealed class HelpViewExtension(
        IUIHost uiHost,
        IAdvancedCardDialogManager advancedCardDialogManager,
        ICardRepository cardRepository,
        ISession session,
        CreateDialogFormFuncAsync createDialogFormFuncAsync,
        ICardFileManager fileManager,
        [OptionalDependency] IApplicationDescriptor? applicationDescriptor = null)
        : IWorkplaceViewComponentExtension, IWorkplaceExtensionSettingsRestore
    {
        #region Fields

        private readonly IUIHost uiHost = NotNullOrThrow(uiHost);
        private readonly IAdvancedCardDialogManager advancedCardDialogManager = NotNullOrThrow(advancedCardDialogManager);
        private readonly ICardRepository cardRepository = NotNullOrThrow(cardRepository);
        private readonly ISession session = NotNullOrThrow(session);
        private readonly CreateDialogFormFuncAsync createDialogFormFuncAsync = NotNullOrThrow(createDialogFormFuncAsync);
        private readonly ICardFileManager fileManager = NotNullOrThrow(fileManager);
        private HelpViewExtensionSettings settings = new();

        #endregion

        #region IWorkplaceViewComponentExtension Implementation

        /// <inheritdoc />
        public void Initialize(IWorkplaceViewComponent model) =>
            model.ContentFactories[nameof(HelpViewExtension)] = component =>
                new HelpViewExtensionViewModel(
                    this.settings,
                    this.uiHost,
                    this.advancedCardDialogManager,
                    this.cardRepository,
                    this.session,
                    this.createDialogFormFuncAsync,
                    this.fileManager,
                    ContentPlaceAreas.ToolbarPlaces,
                    applicationDescriptor);

        /// <inheritdoc/>
        public void Clone(IWorkplaceViewComponent source, IWorkplaceViewComponent cloned, ICloneableContext context)
        {
        }

        /// <inheritdoc/>
        public void Initialized(IWorkplaceViewComponent model)
        {
        }

        #endregion

        #region IWorkplaceExtensionSettingsRestore Implementation

        /// <inheritdoc />
        public void Restore(Dictionary<string, object?> metadata) =>
            this.settings = metadata.FromSerializedDictionary<HelpViewExtensionSettings>();

        #endregion
    }
}
