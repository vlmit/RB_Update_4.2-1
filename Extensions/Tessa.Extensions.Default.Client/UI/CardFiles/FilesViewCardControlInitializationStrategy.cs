#nullable enable

using System.Threading.Tasks;
using Tessa.UI.Cards.Controls;
using Tessa.UI.Files;
using Tessa.UI.Menu;
using Tessa.Views;
using Tessa.Views.Metadata;

namespace Tessa.Extensions.Default.Client.UI.CardFiles
{
    /// <summary>
    /// Стратегия инициализации для представления с файлами.
    /// </summary>
    public class FilesViewCardControlInitializationStrategy : ViewCardControlInitializationStrategy
    {
        #region Constructor

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="FilesViewCardControlInitializationStrategy"/>.
        /// </summary>
        /// <param name="viewService"><inheritdoc cref="IViewService" path="/summary"/></param>
        /// <param name="createMenuContextFunc"><inheritdoc cref="CreateMenuContextFunc" path="/summary"/></param>
        /// <param name="contentItemsFactory"><inheritdoc cref="IViewCardControlContentItemsFactory" path="/summary"/></param>
        public FilesViewCardControlInitializationStrategy(
            IViewService viewService,
            CreateMenuContextFunc createMenuContextFunc,
            IViewCardControlContentItemsFactory contentItemsFactory)
            : base(viewService, createMenuContextFunc, contentItemsFactory)
        {
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc />
        public override async ValueTask InitializeMetadataAsync(CardViewControlInitializationContext context)
        {
            ThrowIfNull(context);
            context.ControlViewModel.ViewMetadata = await this.CreateViewMetadataAsync(context).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public override async ValueTask InitializeDataProviderAsync(CardViewControlInitializationContext context)
        {
            var info = NotNullOrThrow(context?.Model?.Info);
            var metadata = NotNullOrThrow(context.ControlViewModel?.ViewMetadata);
            var filerControl = NotNullOrThrow(CardFilesHelper.TryGetFileControl(info, context.ControlViewModel.Name));

            context.ControlViewModel.DataProvider =
                await this.CreateDataProviderAsync(
                    context,
                    metadata,
                    filerControl).ConfigureAwait(false);
        }

        /// <inheritdoc />
        /// <remarks>Не запускать базовую реализацию. 
        /// Представление генерируемое, настройки пагинации берутся из настроек создания файлового контрола,
        /// которые заполняются в FilesViewGeneratorBaseUIExtension на основании настроек, установленных для расширения.</remarks>
        public override ValueTask InitializePagingAsync(CardViewControlInitializationContext context) => 
            ValueTask.CompletedTask;


        /// <summary>
        /// Создает метаданные представления с файлами.
        /// </summary>
        /// <param name="context"><inheritdoc cref="CardViewControlInitializationContext" path="/summary"/></param>
        /// <returns>Метаданные представления с файлами.</returns>
        protected virtual ValueTask<IViewMetadata> CreateViewMetadataAsync(CardViewControlInitializationContext context) =>
            new(FilesViewMetadata.Create());

        /// <summary>
        /// Создает провайдер данных для представления с файлами.
        /// </summary>
        /// <param name="context"><inheritdoc cref="CardViewControlInitializationContext" path="/summary"/></param>
        /// <param name="viewMetadata"><inheritdoc cref="IViewMetadata" path="/summary"/></param>
        /// <param name="fileControl"><inheritdoc cref="IFileControl" path="/summary"/></param>
        /// <returns>Провайдер данных для представления с файлами.</returns>
        protected virtual ValueTask<IDataProvider> CreateDataProviderAsync(
            CardViewControlInitializationContext context,
            IViewMetadata viewMetadata,
            IFileControl fileControl) =>
            new(new CardFilesDataProvider(viewMetadata, fileControl));

        #endregion
    }
}
