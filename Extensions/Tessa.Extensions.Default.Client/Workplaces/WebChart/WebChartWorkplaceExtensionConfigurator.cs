#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Extensions.Default.Client.Workplaces.WebChart.Palette;
using Tessa.Extensions.Default.Shared.Workplaces;
using Tessa.Platform.Storage;
using Tessa.UI.Views;
using Tessa.UI.Views.Content;
using Tessa.UI.Views.Extensions;
using Tessa.Views;
using Unity;

namespace Tessa.Extensions.Default.Client.Workplaces.WebChart
{
    /// <summary>
    /// Конфигуратор расширения <see cref="WebChartWorkplaceExtension"/>.
    /// </summary>
    public sealed class WebChartWorkplaceExtensionConfigurator(IUnityContainer container)
        : ExtensionSettingsConfiguratorBase(ViewExtensionConfiguratorType.Custom, null, "$WebChartWorkplaceExtension_Description")
    {
        #region Fields

        private readonly IUnityContainer container = NotNullOrThrow(container);

        #endregion

        #region Base Overrides

        /// <inheritdoc />
        public override async ValueTask<bool> ConfigureAsync(IExtensionConfigurationContext context, CancellationToken cancellationToken = default)
        {
            using var localContainer = this.container.CreateChildContainer();
            var dataSourceMetadata = await context.GetPropertyValueAsync<IDataSourceMetadata>("Metadata", cancellationToken);
            var columnNames = Enumerable.Empty<string>();
            if (await GetViewComponentAsync(dataSourceMetadata, localContainer, cancellationToken) is { } item
                && await item.GetViewMetadataAsync(item, cancellationToken) is { } metadata)
            {
                columnNames = metadata.Columns.Select(c => c.Alias).ToArray();
            }

            var settings = context.GetSettings().FromSerializedDictionary<WebChartWorkplaceSettings>() ?? new();
            var settingsDictionaryBefore = new Dictionary<string, object?>();
            settings.Serialize(settingsDictionaryBefore);
            var settingsViewModel = WebChartSettingsViewModel.Create(settings, columnNames);
            var settingsDialog = new WebChartSettingsDialog { DataContext = settingsViewModel };
            if (settingsDialog.ShowDialog() != true)
            {
                return false;
            }

            settings.DiagramType = settingsViewModel.DiagramType;
            settings.DiagramDirection = settingsViewModel.DiagramDirection;
            settings.LegendPosition = settingsViewModel.LegendPosition;
            settings.YColumn = settingsViewModel.YColumn?.Length == 0 ? null : settingsViewModel.YColumn;
            settings.LegendItemMinWidth = settingsViewModel.LegendItemMinWidth;
            settings.ColumnCount = settingsViewModel.ColumnCount;
            settings.LegendNotWrap = settingsViewModel.LegendNotWrap;
            settings.DoesntShowZeroValues = settingsViewModel.DoesntShowZeroValues;
            settings.SelectedColor = settingsViewModel.SelectedColor;
            settings.XColumn = settingsViewModel.XColumn?.Length == 0 ? null : settingsViewModel.XColumn;
            settings.CaptionColumn = settingsViewModel.CaptionColumn?.Length == 0 ? null : settingsViewModel.CaptionColumn;
            settings.Caption = settingsViewModel.Caption;
            settings.PaletteTypeId = settingsViewModel.SelectedPaletteTypeId.ToString();

            var settingsDictionary = settings.ToSerializedDictionary();
            if (!StorageHelper.Equals(settingsDictionaryBefore, settingsDictionary))
            {
                context.SaveSettings(settingsDictionary);
                context.ResetViewComponent();
                return true;
            }

            return false;
        }

        /// <inheritdoc />
        public override void Initialize(IExtensionConfigurationContext context) =>
            context.SaveSettings(
                new WebChartWorkplaceSettings
                {
                    XColumn = string.Empty,
                    DiagramType = Enum.GetValues(typeof(WebChartDiagramType)).Cast<WebChartDiagramType>().First(),
                    DiagramDirection = Enum.GetValues(typeof(WebChartDiagramDirection)).Cast<WebChartDiagramDirection>().First(),
                    LegendPosition = WebChartLegendPosition.Bottom,
                    YColumn = string.Empty,
                    LegendItemMinWidth = null,
                    ColumnCount = 1,
                    LegendNotWrap = false,
                    DoesntShowZeroValues = false,
                    SelectedColor = string.Empty,
                    CaptionColumn = string.Empty,
                    Caption = string.Empty,
                    PaletteTypeId = PaletteConstants.AccentPalette.TypeId.ToString()
                }.ToSerializedDictionary());

        #endregion

        #region Private Methods

        private static ValueTask<IWorkplaceViewComponent?> GetViewComponentAsync(
            IDataSourceMetadata dataSourceMetadata,
            IUnityContainer container,
            CancellationToken cancellationToken = default) =>
            WorkplaceViewComponentHelper.CreateWorkplaceViewComponentAsync(
                container.Resolve<ContentFactory>(),
                dataSourceMetadata,
                null,
                [],
                new(StringComparer.OrdinalIgnoreCase),
                container.Resolve<IWorkplaceExtensionExecutorFactory>(),
                cancellationToken);

        #endregion
    }
}
