#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Platform;
using Tessa.Platform.Storage;
using Tessa.UI;
using Tessa.UI.Cards;
using Tessa.UI.Views.Extensions;

namespace Tessa.Extensions.Default.Client.Views
{
    /// <summary>
    /// Configurator for <see cref="InformationLabelViewExtension"/>.
    /// </summary>
    public sealed class InformationLabelViewExtensionConfigurator(CreateDialogFormFuncAsync createDialogFormFunc)
        : ExtensionSettingsConfiguratorBase(ViewExtensionConfiguratorType.Form, null, "$InformationLabelViewExtension_Description")
    {
        #region Fields

        private readonly CreateDialogFormFuncAsync createDialogFormFunc = NotNullOrThrow(createDialogFormFunc);

        #endregion

        #region Base Overrides

        /// <inheritdoc />
        public override void Initialize(IExtensionConfigurationContext context) =>
            context.SaveSettings(new InformationLabelViewExtensionSettings().ToSerializedDictionary());

        public override async ValueTask<(IFormViewModelBase?, Action?)> GetConfiguratorFormAsync(
            IExtensionConfigurationContext context,
            Action modifiedAction,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(context);

            var settings = context.GetSettings().FromSerializedDictionary<InformationLabelViewExtensionSettings>() ?? new();

            var (form, cardModel) = await this.createDialogFormFunc(
                "ViewInformationLabelSettings",
                "MainForm",
                cancellationToken: cancellationToken);

            if (form is null || cardModel is null)
            {
                await TessaDialog.ShowErrorAsync("$CardTypes_MetadataEditor_ViewInformationLabelSettings_NotFound");
                return (null, null);
            }

            var section = cardModel.Card.Sections.TryGet("Settings");
            if (section is null)
            {
                await TessaDialog.ShowErrorAsync("$CardTypes_MetadataEditor_ViewInformationLabelSettings_BrokenCardData");
                return (null, null);
            }

            section.Fields[nameof(settings.LabelText)] = settings.LabelText;
            section.Fields[nameof(settings.RequiredParams)] = settings.RequiredParams;
            section.Fields[nameof(settings.AllParamsRequired)] = BooleanBoxes.Box(settings.AllParamsRequired);

            section.FieldChanged += (o, e) =>
            {
                modifiedAction();
                switch (e.FieldName)
                {
                    case nameof(settings.LabelText):
                        settings.LabelText = (string?) e.FieldValue;
                        break;
                    case nameof(settings.RequiredParams):
                        settings.RequiredParams = (string?) e.FieldValue;
                        break;
                    case nameof(settings.AllParamsRequired):
                        settings.AllParamsRequired = (bool) (e.FieldValue ?? false);
                        break;
                }
            };

            return (form, () => context.SaveSettings(settings.ToSerializedDictionary()));
        }

        #endregion
    }
}
