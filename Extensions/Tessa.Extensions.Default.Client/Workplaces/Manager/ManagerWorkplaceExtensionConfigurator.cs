#nullable enable
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Extensions.Default.Shared.Workplaces;
using Tessa.Platform.Storage;
using Tessa.UI;
using Tessa.UI.Cards;
using Tessa.UI.Cards.Controls.AutoComplete;
using Tessa.UI.Controls.Helpers;
using Tessa.UI.Views;
using Tessa.UI.Views.Content;
using Tessa.UI.Views.Extensions;
using Tessa.Views;
using Unity;

namespace Tessa.Extensions.Default.Client.Workplaces.Manager
{
    /// <summary>
    /// Конфигуратор расширения <see cref="ManagerWorkplaceExtension"/>.
    /// </summary>
    /// <param name="container"><inheritdoc cref="IUnityContainer" path="/summary"/></param>
    /// <param name="createDialogFormFunc"><inheritdoc cref="CreateDialogFormFuncAsync" path="/summary"/></param>
    /// <param name="autoCompleteDialogProvider"><inheritdoc cref="AutoCompleteDialogProvider" path="/summary"/></param>
    public sealed class ManagerWorkplaceExtensionConfigurator(
        IUnityContainer container,
        CreateDialogFormFuncAsync createDialogFormFunc,
        AutoCompleteDialogProvider autoCompleteDialogProvider)
        : ExtensionSettingsConfiguratorBase(ViewExtensionConfiguratorType.Form, null, "$ManagerWorkplaceExtension_Description")
    {
        #region Fields

        private readonly CreateDialogFormFuncAsync createDialogFormFunc = NotNullOrThrow(createDialogFormFunc);

        private readonly AutoCompleteDialogProvider autoCompleteDialogProvider = NotNullOrThrow(autoCompleteDialogProvider);

        private readonly IUnityContainer container = NotNullOrThrow(container);

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override async ValueTask<(IFormViewModelBase?, Action?)> GetConfiguratorFormAsync(
            IExtensionConfigurationContext context,
            Action markedAsDirtyAction,
            CancellationToken cancellationToken = default)
        {
            using var localContainer = this.container.CreateChildContainer();
            var dataSourceMetadata = await context.GetPropertyValueAsync<IDataSourceMetadata>("Metadata", cancellationToken);
            var item = await GetViewComponentAsync(dataSourceMetadata, localContainer, cancellationToken);
            var columnNames = Enumerable.Empty<string>();
            if (item != null)
            {
                var metadata = await item.GetViewMetadataAsync(item, cancellationToken);
                if (metadata != null)
                {
                    columnNames = metadata.Columns.Select(c => c.Alias).ToArray();
                }
            }

            var settings = context.GetSettings().FromSerializedDictionary<ManagerWorkplaceSettings>() ?? new();

            var (form, cardModel) = await this.createDialogFormFunc(
                "ViewExtensions",
                "ManagerWorkplaceExtension",
                cancellationToken: cancellationToken,
                modifyModelAsync: (cm, ct) =>
                {
                    cm.ControlInitializers.Add(
                        (controlViewModel, cm2, cr, ct2) =>
                        {
                            switch (controlViewModel)
                            {
                                case AutoCompleteEntryViewModel autoComplete:
                                    switch (autoComplete.Name)
                                    {
                                        case "ActiveImageColumnName"
                                            or "CountColumnName"
                                            or "HoverImageColumnName"
                                            or "InactiveImageColumnName"
                                            or "TileColumnName":
                                            var columnsView = new NamedRecordsView<string>(
                                                "NamedValue",
                                                columnNames,
                                                x => [Guid.Empty, x],
                                                x => x,
                                                localize: false);
                                            autoComplete.View = columnsView;
                                            autoComplete.ViewComboBox = columnsView;

                                            this.autoCompleteDialogProvider.ChangeAutoCompleteDialog(autoComplete);
                                            break;
                                    }

                                    break;
                            }

                            return ValueTask.CompletedTask;
                        });
                    return ValueTask.CompletedTask;
                });

            if (form is null)
            {
                TessaDialog.ShowError("$CardTypes_MetadataEditor_ViewExtensionDialog_NotFound");
                return (null, null);
            }

            ThrowIfNull(cardModel);

            var section = cardModel.Card.Sections["ManagerWorkplaceExtension"];
            section.Fields["CardId"] = settings.CardId.ToString();
            section.Fields["ActiveImageColumnName"] = settings.ActiveImageColumnName;
            section.Fields["CountColumnName"] = settings.CountColumnName;
            section.Fields["HoverImageColumnName"] = settings.HoverImageColumnName;
            section.Fields["InactiveImageColumnName"] = settings.InactiveImageColumnName;
            section.Fields["TileColumnName"] = settings.TileColumnName;

            section.FieldChanged += (o, e) =>
            {
                markedAsDirtyAction();
                switch (e.FieldName)
                {
                    case "CardId":
                        try
                        {
                            settings.CardId = Guid.Parse(e.FieldValue?.ToString() ?? string.Empty);
                        }
                        catch (FormatException)
                        {
                            throw new FormatException("$UI_Common_FormatException");
                        }

                        break;
                    case "ActiveImageColumnName":
                        settings.ActiveImageColumnName = e.FieldValue?.ToString();
                        break;
                    case "CountColumnName":
                        settings.CountColumnName = e.FieldValue?.ToString();
                        break;
                    case "HoverImageColumnName":
                        settings.HoverImageColumnName = e.FieldValue?.ToString();
                        break;
                    case "InactiveImageColumnName":
                        settings.InactiveImageColumnName = e.FieldValue?.ToString();
                        break;
                    case "TileColumnName":
                        settings.TileColumnName = e.FieldValue?.ToString();
                        break;
                }
            };

            return (form, () => context.SaveSettings(settings.ToSerializedDictionary()));
        }

        /// <inheritdoc />
        public override void Initialize(IExtensionConfigurationContext context) =>
            context.SaveSettings(
                new ManagerWorkplaceSettings
                {
                    CardId = new(0x3db19fa0, 0x228a, 0x497f, 0x87, 0x3a, 0x02, 0x50, 0xbf, 0x0a, 0x4c, 0xcb), // 3db19fa0-228a-497f-873a-0250bf0a4ccb
                    TileColumnName = "Caption",
                    CountColumnName = "Count",
                    ActiveImageColumnName = "ActiveImage",
                    HoverImageColumnName = "ActiveImage",
                    InactiveImageColumnName = "InactiveImage",
                }.ToSerializedDictionary());

        #endregion

        #region Private Methods

        /// <summary>
        /// Создает и возвращает компонент отображения данных представления.
        /// </summary>
        /// <param name="dataSourceMetadata">
        /// Метаданные источника данных.
        /// </param>
        /// <param name="container">
        /// Контейнер.
        /// </param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>
        /// Компонент отображения данных.
        /// </returns>
        private static async ValueTask<IWorkplaceViewComponent?> GetViewComponentAsync(
            IDataSourceMetadata dataSourceMetadata,
            IUnityContainer container,
            CancellationToken cancellationToken = default)
        {
            return await WorkplaceViewComponentHelper.CreateWorkplaceViewComponentAsync(
                container.Resolve<ContentFactory>(),
                dataSourceMetadata,
                null,
                [],
                new(StringComparer.OrdinalIgnoreCase),
                container.Resolve<IWorkplaceExtensionExecutorFactory>(),
                cancellationToken);
        }

        #endregion
    }
}
