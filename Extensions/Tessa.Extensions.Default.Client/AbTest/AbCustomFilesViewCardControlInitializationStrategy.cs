#nullable enable

using System.Threading.Tasks;
using Tessa.Extensions.Default.Client.UI.CardFiles;
using Tessa.Scheme;
using Tessa.UI.Cards.Controls;
using Tessa.UI.Files;
using Tessa.UI.Menu;
using Tessa.Views;
using Tessa.Views.Metadata;

namespace Tessa.Extensions.Default.Client.AbTest
{
    /// <summary>
    /// Customized file view initialization strategy.
    /// </summary>
    /// <param name="viewService"><inheritdoc cref="IViewService" path="/summary"/></param>
    /// <param name="createMenuContextFunc"><inheritdoc cref="CreateMenuContextFunc" path="/summary"/></param>
    /// <param name="contentItemsFactory"><inheritdoc cref="IViewCardControlContentItemsFactory" path="/summary"/></param>
    public sealed class AbCustomFilesViewCardControlInitializationStrategy(
        IViewService viewService,
        CreateMenuContextFunc createMenuContextFunc,
        IViewCardControlContentItemsFactory contentItemsFactory)
        : FilesViewCardControlInitializationStrategy(viewService, createMenuContextFunc, contentItemsFactory)
    {
        #region Base Overrides

        /// <inheritdoc/>
        protected override async ValueTask<IViewMetadata> CreateViewMetadataAsync(CardViewControlInitializationContext context)
        {
            var viewMetadata = await base.CreateViewMetadataAsync(context).ConfigureAwait(false);
            viewMetadata.Columns.Add(new ViewColumnMetadata
            {
                Caption = await LocalizeAsync("$CardTypes_Columns_Controls_Date"),
                Alias = "Date",
                SchemeType = SchemeType.DateTime,
                DisableGrouping = true,
                SortBy = "Date",
            });
            viewMetadata.Columns.Add(new ViewColumnMetadata
            {
                Caption = await LocalizeAsync("$CardTypes_Controls_Description"),
                Alias = "Description",
                SchemeType = SchemeType.NullableString,
                DisableGrouping = true,
                SortBy = "Description",
            });
            return viewMetadata;
        }

        /// <inheritdoc/>
        protected override ValueTask<IDataProvider> CreateDataProviderAsync(
            CardViewControlInitializationContext context, IViewMetadata viewMetadata, IFileControl fileControl) =>
            new(new AbCustomCardFilesDataProvider(viewMetadata, fileControl));

        #endregion
    }
}
