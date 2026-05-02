#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Platform;
using Tessa.Platform.Licensing;
using Tessa.Scheme;
using Tessa.Views;
using Tessa.Views.Metadata;

namespace Tessa.Extensions.Default.Server.Views
{
    /// <summary>
    /// Возвращает список программных представлений правил доступа, построенных на основе базовых реализаций представлений,
    /// в метаданных которых удаляются колонки, параметры и сабсеты, относящиеся к ACL, если отсутствует модуль лицензии <see cref="LicenseModules.AclID"/>.
    /// </summary>
    public sealed class KrPermissionsViewListProvider(
        IViewDataAccessor viewDataAccessor,
        CreateTessaViewFunc createTessaViewFunc,
        ILicenseManager licenseManager)
        : IExtraViewListProvider
    {
        #region Fields

        private readonly IViewDataAccessor viewDataAccessor = NotNullOrThrow(viewDataAccessor);
        private readonly CreateTessaViewFunc createTessaViewFunc = NotNullOrThrow(createTessaViewFunc);
        private readonly ILicenseManager licenseManager = NotNullOrThrow(licenseManager);

        private static readonly string[] viewAliases = ["KrPermissions", "KrPermissionsReport"];

        #endregion

        #region IExtraViewListProvider Implementation

        /// <inheritdoc/>
        public async ValueTask<IReadOnlyList<ITessaView>> GetExtraViewsAsync(ViewDatabaseInfo defaultDatabaseInfo, CancellationToken cancellationToken = default)
        {
            var license = await this.licenseManager.GetLicenseAsync(cancellationToken);
            if (license.Modules.HasEnterpriseOrContains(LicenseModules.AclID))
            {
                return [];
            }

            var extraViews = new List<ITessaView>(viewAliases.Length);
            foreach (var viewName in viewAliases)
            {
                if (await this.viewDataAccessor.GetViewByAliasAsync(viewName, cancellationToken) is { } originalViewModel)
                {
                    var originalView = this.createTessaViewFunc(defaultDatabaseInfo, originalViewModel);
                    extraViews.Add(new KrPermissionsViewDecorator(originalView));
                }
            }

            return extraViews;
        }

        #endregion

        #region Nested Types

        /// <summary>
        /// Декоратор для представлений правил доступа, которое удаляет все упоминания об ACL
        /// из метаданных и результата, если данный модуль не включён в лицензию.
        /// </summary>
        private sealed class KrPermissionsViewDecorator : ITessaView
        {
            #region Constructors

            public KrPermissionsViewDecorator(ITessaView originalView)
            {
                this.originalView = NotNullOrThrow(originalView);
                this.metadataLazy = new(this.CreateMetadataAsync);
            }

            #endregion

            #region Fields and Constants

            private readonly ITessaView originalView;

            private readonly AsyncLazy<IViewMetadata> metadataLazy;

            private const string AclGenerationRuleParameter = "AclGenerationRule";

            private const string ByAclGenerationRuleSubset = "ByAclGenerationRule";

            #endregion

            #region Private Methods

            private async Task<IViewMetadata> CreateMetadataAsync()
            {
                var metadata = (await this.originalView.GetMetadataAsync()).Clone();

                if (metadata.Columns.FindByName("KrPermissionsAclGenerationRules") is { } column)
                {
                    column.Hidden = true;
                }

                // SQL expressions in the original view reference that subset and parameter, so we can't modify original metadata in order to call original SQL
                metadata.Parameters.RemoveByName(AclGenerationRuleParameter);
                metadata.Subsets.RemoveByName(ByAclGenerationRuleSubset);

                metadata.Seal();
                return metadata;
            }

            #endregion

            #region ITessaView Members

            /// <inheritdoc/>
            public string Alias => this.originalView.Alias;

            /// <inheritdoc/>
            public ValueTask<IViewMetadata> GetMetadataAsync(CancellationToken cancellationToken = default) => new(this.metadataLazy.Value);

            /// <inheritdoc/>
            public ValueTask<ITessaViewResult> GetDataAsync(ITessaViewRequest request, CancellationToken cancellationToken = default)
            {
                // prevent calling original view with a hidden parameter even if it's passed regardless of its absence in the metadata
                request.Parameters.RemoveAllByName(AclGenerationRuleParameter);

                // prevent calling original view with a hidden subset even if it's passed regardless of its absence in the metadata
                if (ParserNames.IsEquals(request.SubsetName, ByAclGenerationRuleSubset))
                {
                    return new(new TessaViewResult
                    {
                        Columns = [("RuleID", SchemeType.Guid), ("RuleName", SchemeType.String), ("CntRule", SchemeType.Int32)]
                    });
                }

                return this.originalView.GetDataAsync(request, cancellationToken);
            }

            #endregion
        }

        #endregion
    }
}
