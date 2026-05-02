using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Tessa.Cards;
using Tessa.Extensions.Default.Shared.Views;
using Tessa.Platform;
using Tessa.Platform.Storage;
using Tessa.UI;
using Tessa.UI.Cards;
using Tessa.UI.Cards.Controls.AutoComplete;
using Tessa.UI.Controls.Helpers;
using Tessa.UI.Views.Extensions;

namespace Tessa.Extensions.Default.Client.Views
{
    /// <summary>
    /// Конфигуратор расширения <see cref="CreateCardExtension"/>.
    /// </summary>
    /// <param name="createDialogFormFunc"><inheritdoc cref="CreateDialogFormFuncAsync" path="/summary"/></param>
    /// <param name="autoCompleteDialogProvider"><inheritdoc cref="AutoCompleteDialogProvider" path="/summary"/></param>
    public sealed class CreateCardExtensionConfigurator(
        CreateDialogFormFuncAsync createDialogFormFunc,
        AutoCompleteDialogProvider autoCompleteDialogProvider)
        : ExtensionSettingsConfiguratorBase(ViewExtensionConfiguratorType.Form, null, "$CreateCardExtension_Description")
    {
        #region Fields

        private readonly CreateDialogFormFuncAsync createDialogFormFunc = NotNullOrThrow(createDialogFormFunc);

        private readonly AutoCompleteDialogProvider autoCompleteDialogProvider = NotNullOrThrow(autoCompleteDialogProvider);

        private static readonly IReadOnlyDictionary<CardCreationKind, string> cardCreationKindLocalisations =
            new Dictionary<CardCreationKind, string>
            {
                [CardCreationKind.ByDocTypeIdentifier] = "$CreateCardExtensionSettingsViewModel_CardCreationKind_ByDocTypeIdentifier",
                [CardCreationKind.ByTypeAlias] = "$CreateCardExtensionSettingsViewModel_CardCreationKind_ByTypeAlias",
                [CardCreationKind.ByTypeFromSelection] = "$CreateCardExtensionSettingsViewModel_CardCreationKind_ByTypeFromSelection"
            };

        private static readonly IReadOnlyDictionary<CardOpeningKind, string> cardOpeningKindLocalisations =
            new Dictionary<CardOpeningKind, string>
            {
                [CardOpeningKind.ApplicationTab] = "$CreateCardExtensionSettingsViewModel_CardOpeningKind_ApplicationTab",
                [CardOpeningKind.ModalDialog] = "$CreateCardExtensionSettingsViewModel_CardOpeningKind_ModalDialog"
            };

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override async ValueTask<(IFormViewModelBase, Action)> GetConfiguratorFormAsync(
            IExtensionConfigurationContext context,
            Action markedAsDirtyAction,
            CancellationToken cancellationToken = default)
        {
            var settings = context.GetSettings().FromSerializedDictionary<CreateCardExtensionSettings>() ?? new();

            var (form, cardModel) = await this.createDialogFormFunc(
                "ViewExtensions",
                "CreateCardExtension",
                cancellationToken: cancellationToken,
                modifyModelAsync: this.ModifyCardModelAsync);

            if (form is null)
            {
                TessaDialog.ShowError("$CardTypes_MetadataEditor_ViewExtensionDialog_NotFound");
                return (null, null);
            }

            var section = cardModel.Card.Sections["CreateCardExtension"];
            section.Fields["IDParam"] = settings.IDParam;
            section.Fields["CardOpeningKindName"] = cardOpeningKindLocalisations[settings.CardOpeningKind];
            section.Fields["CreateCardKindName"] = cardCreationKindLocalisations[settings.CardCreationKind];
            section.Fields["TypeAlias"] = settings.TypeAlias;
            section.Fields["DocTypeIdentifier"] = settings.DocTypeIdentifier;
            section.Fields[nameof(settings.OpenInFullscreen)] = BooleanBoxes.Box(settings.OpenInFullscreen);
            section.Fields[nameof(settings.OpenOnlyFirstTab)] = BooleanBoxes.Box(settings.OpenOnlyFirstTab);
            section.Fields[nameof(settings.DisplayValue)] = settings.DisplayValue;

            var mainBlock = form switch
            {
                IFormWithBlocksViewModel formWithBlocks => formWithBlocks.Blocks.First(x => x.Name == "MainBlock"),
                IFormWithTabsViewModel formWithTabs => formWithTabs.Tabs.SelectMany(x => x.Blocks).First(x => x.Name == "MainBlock"),
                _ => throw new InvalidOperationException($"Unknown form type created fo form \"ViewExtensions\""),
            };

            section.FieldChanged += (o, e) =>
            {
                markedAsDirtyAction();
                switch (e.FieldName)
                {
                    case "IDParam":
                        settings.IDParam = e.FieldValue?.ToString();
                        break;
                    case "CardOpeningKindName":
                        if (e.FieldValue is not null)
                        {
                            var kind = cardOpeningKindLocalisations.First(x => x.Value == e.FieldValue.ToString()).Key;
                            settings.CardOpeningKind = kind;
                            OnCardOpeningKindChange(settings, section, mainBlock);
                        }

                        break;
                    case "CreateCardKindName":
                        if (e.FieldValue is not null)
                        {
                            var kind = cardCreationKindLocalisations.First(x => x.Value == e.FieldValue.ToString()).Key;
                            settings.CardCreationKind = kind;
                            OnCardCreationKindChange(settings, section, mainBlock);
                        }

                        break;
                    case "TypeAlias":
                        settings.TypeAlias = e.FieldValue?.ToString();
                        break;
                    case "DisplayValue":
                        settings.DisplayValue = e.FieldValue?.ToString();
                        break;
                    case "DocTypeIdentifier":
                        settings.DocTypeIdentifier = e.FieldValue?.ToString();
                        break;
                    case nameof(settings.OpenInFullscreen):
                        settings.OpenInFullscreen = (bool) (e.FieldValue ?? false);
                        break;
                    case nameof(settings.OpenOnlyFirstTab):
                        settings.OpenOnlyFirstTab = (bool) (e.FieldValue ?? false);
                        break;
                }
            };

            OnCardCreationKindChange(settings, section, mainBlock);
            OnCardOpeningKindChange(settings, section, mainBlock);

            return (form, () => context.SaveSettings(settings.ToSerializedDictionary()));
        }

        /// <inheritdoc />
        public override void Initialize(IExtensionConfigurationContext context) =>
            context.SaveSettings(new CreateCardExtensionSettings().ToSerializedDictionary());

        #endregion

        #region Private Methods

        private static void OnCardCreationKindChange(
            CreateCardExtensionSettings settings,
            CardSection section,
            IBlockViewModel block)
        {
            switch (settings.CardCreationKind)
            {
                case CardCreationKind.ByTypeAlias:
                    block.Controls.First(x => x.Name == "TypeAlias").ControlVisibility = Visibility.Visible;
                    block.Controls.First(x => x.Name == "DocTypeIdentifier").ControlVisibility = Visibility.Collapsed;
                    section.Fields["DocTypeIdentifier"] = null;
                    break;
                case CardCreationKind.ByDocTypeIdentifier:
                    block.Controls.First(x => x.Name == "TypeAlias").ControlVisibility = Visibility.Collapsed;
                    block.Controls.First(x => x.Name == "DocTypeIdentifier").ControlVisibility = Visibility.Visible;
                    section.Fields["TypeAlias"] = null;
                    break;
                case CardCreationKind.ByTypeFromSelection:
                    block.Controls.First(x => x.Name == "TypeAlias").ControlVisibility = Visibility.Collapsed;
                    block.Controls.First(x => x.Name == "DocTypeIdentifier").ControlVisibility = Visibility.Collapsed;
                    section.Fields["TypeAlias"] = null;
                    section.Fields["DocTypeIdentifier"] = null;
                    break;
            }

            block.RearrangeSelf();
        }

        private static void OnCardOpeningKindChange(
            CreateCardExtensionSettings settings,
            CardSection section,
            IBlockViewModel block)
        {
            switch (settings.CardOpeningKind)
            {
                case CardOpeningKind.ApplicationTab:
                    block.Controls.First(x => x.Name == nameof(settings.OpenInFullscreen)).ControlVisibility = Visibility.Collapsed;
                    block.Controls.First(x => x.Name == nameof(settings.OpenOnlyFirstTab)).ControlVisibility = Visibility.Collapsed;
                    section.Fields[nameof(settings.OpenInFullscreen)] = BooleanBoxes.False;
                    section.Fields[nameof(settings.OpenOnlyFirstTab)] = BooleanBoxes.False;
                    break;
                case CardOpeningKind.ModalDialog:
                    block.Controls.First(x => x.Name == nameof(settings.OpenInFullscreen)).ControlVisibility = Visibility.Visible;
                    block.Controls.First(x => x.Name == nameof(settings.OpenOnlyFirstTab)).ControlVisibility = Visibility.Visible;
                    break;
            }

            block.RearrangeSelf();
        }

        private ValueTask ModifyCardModelAsync(ICardModel cardModel, CancellationToken cancellationToken = default)
        {
            cardModel.ControlInitializers.Add(
                (controlViewModel, cm, cr, ct) =>
                {
                    switch (controlViewModel)
                    {
                        case AutoCompleteEntryViewModel autoComplete:
                            switch (autoComplete.Name)
                            {
                                case "CardOpeningKind":
                                    var cardOpeningKindsView = new NamedRecordsView<string>(
                                        "NamedValue",
                                        cardOpeningKindLocalisations.Values,
                                        x => [Guid.Empty, x],
                                        x => x);
                                    autoComplete.View = cardOpeningKindsView;
                                    autoComplete.ViewComboBox = cardOpeningKindsView;

                                    this.autoCompleteDialogProvider.ChangeAutoCompleteDialog(autoComplete);
                                    break;
                                case "CreateCardKind":
                                    var cardCreateKindsView = new NamedRecordsView<string>(
                                        "NamedValue",
                                        cardCreationKindLocalisations.Values,
                                        x => [Guid.Empty, x],
                                        x => x);
                                    autoComplete.View = cardCreateKindsView;
                                    autoComplete.ViewComboBox = cardCreateKindsView;

                                    this.autoCompleteDialogProvider.ChangeAutoCompleteDialog(autoComplete);
                                    break;
                            }

                            break;
                    }

                    return ValueTask.CompletedTask;
                });

            return ValueTask.CompletedTask;
        }

        #endregion
    }
}
