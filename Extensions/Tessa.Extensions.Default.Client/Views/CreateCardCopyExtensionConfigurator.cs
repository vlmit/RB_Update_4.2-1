#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Platform.Storage;
using Tessa.UI;
using Tessa.UI.Cards;
using Tessa.UI.Views.Extensions;

namespace Tessa.Extensions.Default.Client.Views
{
    /// <summary>
    /// Configurator for <see cref="CreateCardCopyExtension"/>.
    /// </summary>
    public sealed class CreateCardCopyExtensionConfigurator(CreateDialogFormFuncAsync createDialogFormFunc)
        : ExtensionSettingsConfiguratorBase(ViewExtensionConfiguratorType.Form, null, "$CreateCardCopyExtension_Description")
    {
        #region Fields

        private readonly CreateDialogFormFuncAsync createDialogFormFunc = NotNullOrThrow(createDialogFormFunc);

        #endregion

        #region Base Overrides

        /// <inheritdoc />
        public override void Initialize(IExtensionConfigurationContext context) =>
            context.SaveSettings(new CreateCardCopyExtensionSettings().ToSerializedDictionary());

        /// <inheritdoc/>
        public override async ValueTask<(IFormViewModelBase?, Action?)>
            GetConfiguratorFormAsync(
                IExtensionConfigurationContext context,
                Action modifiedAction,
                CancellationToken cancellationToken = default)
        {
            var settings = context.GetSettings().FromSerializedDictionary<CreateCardCopyExtensionSettings>() ?? new();
            var (form, cardModel) = await this.createDialogFormFunc(
                "ViewExtensions",
                "CreateCardCopyExtension",
                cancellationToken: cancellationToken);

            if (form is null)
            {
                TessaDialog.ShowError("$CardTypes_MetadataEditor_ViewExtensionDialog_NotFound");
                return (null, null);
            }

            var section = cardModel?.Card.Sections.TryGet("CreateCardCopyExtension");
            if (section is null)
            {
                TessaDialog.ShowError("$CardTypes_MetadataEditor_CreateCardCopySettings_BrokenCardData");
                return (null, null);
            }

            section.Fields[nameof(settings.IDParam)] = settings.IDParam;
            section.FieldChanged += (o, e) =>
            {
                modifiedAction();
                switch (e.FieldName)
                {
                    case nameof(settings.IDParam):
                        settings.IDParam = (string?) e.FieldValue;
                        break;
                }
            };

            return (form, () => context.SaveSettings(settings.ToSerializedDictionary()));
        }

        #endregion
    }
}
