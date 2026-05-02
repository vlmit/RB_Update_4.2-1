#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Extensions.Default.Shared.Views.HelpExtensionConfigurator;
using Tessa.Platform.Storage;
using Tessa.UI;
using Tessa.UI.Cards;
using Tessa.UI.Cards.Controls.AutoComplete;
using Tessa.UI.Controls.Helpers;
using Tessa.UI.Views.Extensions;

namespace Tessa.Extensions.Default.Client.Views
{
    /// <summary>
    /// Конфигуратор расширения <see cref="HelpViewExtension"/>.
    /// </summary>
    /// <param name="createDialogFormFunc"><inheritdoc cref="CreateDialogFormFuncAsync" path="/summary"/></param>
    /// <param name="autoCompleteDialogProvider"><inheritdoc cref="AutoCompleteDialogProvider" path="/summary"/></param>
    public sealed class HelpViewExtensionConfigurator(
        CreateDialogFormFuncAsync createDialogFormFunc,
        AutoCompleteDialogProvider autoCompleteDialogProvider)
        : ExtensionSettingsConfiguratorBase(ViewExtensionConfiguratorType.Form, null, "$HelpViewExtension_Description")
    {
        #region Fields

        private readonly CreateDialogFormFuncAsync createDialogFormFunc = NotNullOrThrow(createDialogFormFunc);

        private readonly AutoCompleteDialogProvider autoCompleteDialogProvider = NotNullOrThrow(autoCompleteDialogProvider);

        private static readonly Dictionary<CardHelpMode, string> helpModes =
            new()
            {
                [CardHelpMode.Url] = "$UI_Cards_TypesEditor_UrlHelpMode",
                [CardHelpMode.Card] = "$UI_Cards_TypesEditor_CardHelpMode",
            };

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override async ValueTask<(IFormViewModelBase?, Action?)> GetConfiguratorFormAsync(
            IExtensionConfigurationContext context,
            Action markedAsDirtyAction,
            CancellationToken cancellationToken = default)
        {
            var settings = context.GetSettings().FromSerializedDictionary<HelpViewExtensionSettings>() ?? new();

            var (form, cardModel) = await this.createDialogFormFunc(
                "ViewExtensions",
                "HelpViewExtension",
                cancellationToken: cancellationToken,
                modifyModelAsync: this.ModifyCardModelAsync);

            if (form is null || cardModel is null)
            {
                TessaDialog.ShowError("$CardTypes_MetadataEditor_ViewExtensionDialog_NotFound");
                return (null, null);
            }

            var section = cardModel.Card.Sections["HelpViewExtension"];
            section.Fields["Value"] = settings.Value;
            section.Fields["HelpModeName"] = helpModes[settings.HelpMode];

            section.FieldChanged += (o, e) =>
            {
                markedAsDirtyAction();
                switch (e.FieldName)
                {
                    case "Value":
                        settings.Value = e.FieldValue?.ToString();
                        break;
                    case "HelpModeName":
                        if (e.FieldValue is not null)
                        {
                            var mode = helpModes.First(x => x.Value == e.FieldValue.ToString()).Key;
                            settings.HelpMode = mode;
                        }

                        break;
                }
            };

            return (form, () => context.SaveSettings(settings.ToSerializedDictionary()));
        }

        /// <inheritdoc />
        public override void Initialize(IExtensionConfigurationContext context) =>
            context.SaveSettings(new HelpViewExtensionSettings().ToSerializedDictionary());

        #endregion

        #region Private Methods

        private ValueTask ModifyCardModelAsync(ICardModel cardModel, CancellationToken cancellationToken = default)
        {
            cardModel.ControlInitializers.Add(
                (controlViewModel, cm, cr, ct) =>
                {
                    if (controlViewModel is AutoCompleteEntryViewModel { Name: "HelpMode" } autoComplete)
                    {
                        var modes = new NamedRecordsView<string>(
                            "NamedValue",
                            helpModes.Values,
                            x => [Guid.Empty, x],
                            x => x);
                        autoComplete.View = modes;
                        autoComplete.ViewComboBox = modes;

                        this.autoCompleteDialogProvider.ChangeAutoCompleteDialog(autoComplete);
                    }

                    return ValueTask.CompletedTask;
                });

            return ValueTask.CompletedTask;
        }

        #endregion
    }
}
