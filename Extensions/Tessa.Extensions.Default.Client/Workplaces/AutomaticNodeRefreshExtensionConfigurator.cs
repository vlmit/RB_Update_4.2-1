#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Extensions.Default.Shared.Workplaces;
using Tessa.Platform;
using Tessa.Platform.Storage;
using Tessa.UI;
using Tessa.UI.Cards;
using Tessa.UI.Views.Extensions;

namespace Tessa.Extensions.Default.Client.Workplaces
{
    /// <summary>
    /// Конфигуратор для расширения <see cref="AutomaticNodeRefreshExtension" />.
    /// </summary>
    public sealed class AutomaticNodeRefreshExtensionConfigurator(CreateDialogFormFuncAsync createDialogFormFunc)
        : ExtensionSettingsConfiguratorBase(ViewExtensionConfiguratorType.Form, null, "$AutomaticNodeRefreshExtension_Description")
    {
        #region Private Fields

        private readonly CreateDialogFormFuncAsync createDialogFormFunc = NotNullOrThrow(createDialogFormFunc);

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override async ValueTask<(IFormViewModelBase?, Action?)> GetConfiguratorFormAsync(
            IExtensionConfigurationContext context,
            Action markedAsDirtyAction,
            CancellationToken cancellationToken = default)
        {
            var settings = context.GetSettings().FromSerializedDictionary<AutomaticNodeRefreshSettings>() ?? new();

            var (form, cardModel) = await this.createDialogFormFunc(
                "ViewExtensions",
                "AutomaticNodeRefreshExtension",
                cancellationToken: cancellationToken);

            if (form is null || cardModel is null)
            {
                TessaDialog.ShowError("$CardTypes_MetadataEditor_ViewExtensionDialog_NotFound");
                return (null, null);
            }

            var section = cardModel.Card.Sections["AutomaticNodeRefreshExtension"];
            section.Fields[nameof(IAutomaticNodeRefreshSettings.RefreshInterval)] = settings.RefreshInterval;
            section.Fields[nameof(IAutomaticNodeRefreshSettings.WithContentDataRefreshing)] = BooleanBoxes.Box(settings.WithContentDataRefreshing);
            section.Fields[nameof(IAutomaticNodeRefreshSettings.AlwaysRefresh)] = BooleanBoxes.Box(settings.AlwaysRefresh);

            section.FieldChanged += (o, e) =>
            {
                markedAsDirtyAction();
                switch (e.FieldName)
                {
                    case nameof(IAutomaticNodeRefreshSettings.RefreshInterval):
                        settings.RefreshInterval = (int) e.FieldValue!;
                        break;
                    case nameof(IAutomaticNodeRefreshSettings.WithContentDataRefreshing):
                        settings.WithContentDataRefreshing = (bool) e.FieldValue!;
                        break;

                    case nameof(IAutomaticNodeRefreshSettings.AlwaysRefresh):
                        settings.AlwaysRefresh = (bool) e.FieldValue!;
                        break;
                }
            };

            return (form, () => context.SaveSettings(settings.ToSerializedDictionary()));
        }

        /// <inheritdoc />
        public override void Initialize(IExtensionConfigurationContext context) =>
            context.SaveSettings(new AutomaticNodeRefreshSettings().ToSerializedDictionary());

        #endregion
    }
}
