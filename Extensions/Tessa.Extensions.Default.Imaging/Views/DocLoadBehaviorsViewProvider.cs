using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Imaging.DocLoad;
using Tessa.Platform;
using Tessa.Platform.Runtime;
using Tessa.Platform.Storage;
using Tessa.Scheme;
using Tessa.Views;
using Tessa.Views.Metadata;
using Tessa.Views.Metadata.Criteria;

namespace Tessa.Extensions.Default.Imaging.Views
{
    /// <summary>
    /// Провайдер для представления со списком обработчиков поведения для потокового ввода.
    /// </summary>
    public sealed class DocLoadBehaviorsViewProvider(ISession session, IDocLoadBehaviorResolver docLoadBehaviorResolver)
        : IExtraViewListProvider
    {
        #region Fields

        private readonly DocLoadBehaviorsView cachedView = new(NotNullOrThrow(session), NotNullOrThrow(docLoadBehaviorResolver));

        #endregion

        #region IExtraViewListProvider

        /// <inheritdoc />
        public ValueTask<IReadOnlyList<ITessaView>> GetExtraViewsAsync(ViewDatabaseInfo defaultDatabaseInfo, CancellationToken cancellationToken = default) =>
            new([this.cachedView]);

        #endregion

        #region Nested Class

        private sealed class DocLoadBehaviorsView(
            ISession session,
            IDocLoadBehaviorResolver docLoadBehaviorResolver) : ITessaViewWithAccessControl
        {
            #region Fields

            private readonly Lazy<ViewMetadata> metadata = new(CreateViewMetadata, LazyThreadSafetyMode.PublicationOnly);
            private readonly Lazy<Dictionary<string, IDocLoadFilesBehavior>>? data = new(() => GenerateData(docLoadBehaviorResolver), LazyThreadSafetyMode.PublicationOnly);
            private readonly Dictionary<string, Dictionary<string, Dictionary<string, string?>>> localizedData = new();

            #endregion

            #region Private Methods

            private static ViewMetadata CreateViewMetadata()
            {
                var metadata = new ViewMetadata
                {
                    Alias = "DocLoadBehaviors",
                    Caption = "$Views_Names_DocLoadBehaviors",
                    QuickSearchParam = "Name"
                };

                metadata.Columns.Add(new ViewColumnMetadata
                {
                    Alias = "BehaviorID",
                    Hidden = true,
                    SchemeType = SchemeType.Guid
                });
                metadata.Columns.Add(new ViewColumnMetadata
                {
                    Alias = "BehaviorName",
                    Caption = "$Views_DocLoadBehaviors_Name",
                    SchemeType = SchemeType.String,
                    Localizable = true,
                    SortBy = "Name"
                });
                metadata.Columns.Add(new ViewColumnMetadata
                {
                    Alias = "BehaviorAlias",
                    SchemeType = SchemeType.String,
                    Hidden = true
                });
                metadata.Columns.Add(new ViewColumnMetadata
                {
                    Alias = "BehaviorDescription",
                    Caption = "$Views_DocLoadBehaviors_Description",
                    SchemeType = SchemeType.String,
                    Localizable = true
                });
                metadata.Columns.Add(new ViewColumnMetadata
                {
                    Alias = "BehaviorSettings",
                    SchemeType = SchemeType.String,
                    Hidden = true
                });

                metadata.DefaultSortingColumns.Add(new SortingColumn
                {
                    Alias = "BehaviorName",
                    Descending = false
                });

                metadata.Parameters.Add(new ViewParameterMetadata
                {
                    Alias = "Name",
                    Caption = "$Views_DocLoadBehaviors_Name",
                    SchemeType = SchemeType.String,
                    AllowedOperands = { CriteriaOperatorConst.Contains },
                    Multiple = false
                });

                metadata.References.Add(new ViewReferenceMetadata
                {
                    RefSection = { "DocLoadBehaviors" },
                    ColPrefix = "Behavior",
                    DisplayValueColumn = "BehaviorName"
                });

                return metadata;
            }

            private static string? SerializeTypedJson(Dictionary<string, object?>? obj) =>
                obj?.Count >= 0
                    ? StorageHelper.SerializeToTypedJson(obj)
                    : null;

            private static Dictionary<string, IDocLoadFilesBehavior> GenerateData(IDocLoadBehaviorResolver docLoadBehaviorResolver) => 
                docLoadBehaviorResolver.GetAllKeys()
                    .Select(docLoadBehaviorResolver.Resolve)
                    .ToDictionary(behavior => behavior.GetType().Name);

            private async ValueTask<List<List<object?>>?> GetRowsAsync(
                ITessaViewRequest request,
                CancellationToken cancellationToken = default)
            {
                var name = request.GetFirstParameterValue<string?>("Name");
                var locale = request.GetFirstParameterValue<string>(ViewSpecialParametersConst.Locale);
                var culture = TryGetCultureInfo(locale) ?? session.ClientUICulture;

                if (!this.localizedData.TryGetValue(culture.TwoLetterISOLanguageName, out var localizedData) 
                    && this.data?.Value is not null)
                {
                    this.localizedData[culture.TwoLetterISOLanguageName] = localizedData = await this.data.Value.ToDictionaryAsync(
                        static behavior => behavior.Key,
                        async behavior =>
                            new Dictionary<string, string?>
                            {
                                { "BehaviorName", behavior.Value.Name },
                                { "BehaviorNameLocalized", await LocalizeAsync(behavior.Value.Name, culture) ?? string.Empty },
                                { "BehaviorDescription", await LocalizeAsync(behavior.Value.Description) ?? string.Empty },
                                { "BehaviorSettings", SerializeTypedJson(behavior.Value.DefaultSettings) },
                            });
                }

                var filteredData = !string.IsNullOrEmpty(name)
                    ? localizedData?.Where(behaviors => behaviors.Value["BehaviorNameLocalized"]!.Contains(name, StringComparison.OrdinalIgnoreCase))
                    : localizedData;

                var orderedData = request.SortingColumns.FirstOrDefault()?.Descending is true
                    ? filteredData?.OrderByDescending(static behaviors => behaviors.Value["BehaviorNameLocalized"])
                    : filteredData?.OrderBy(static behaviors => behaviors.Value["BehaviorNameLocalized"]);


                return orderedData?
                    .Select(x => new List<object?> { x.Key.ToGuid(), x.Value["BehaviorName"], x.Key, x.Value["BehaviorDescription"], x.Value["BehaviorSettings"] })
                    .ToList();
            }

            private static CultureInfo? TryGetCultureInfo(string? cultureName)
            {
                try
                {
                    return !string.IsNullOrEmpty(cultureName) ? CultureInfo.GetCultureInfo(cultureName) : null;
                }
                catch (CultureNotFoundException)
                {
                    return null;
                }
            }

            #endregion

            #region ITessaView Members

            /// <inheritdoc/>
            public string Alias => this.metadata.Value.Alias;

            /// <inheritdoc cref="ITessaView.GetMetadataAsync" />
            public ValueTask<IViewMetadata> GetMetadataAsync(CancellationToken cancellationToken = default) =>
                new(this.metadata.Value);

            /// <inheritdoc/>
            public async ValueTask<ITessaViewResult> GetDataAsync(ITessaViewRequest request, CancellationToken cancellationToken = default) =>
                new TessaViewResult(this.metadata.Value) { Rows = await this.GetRowsAsync(request, cancellationToken) };

            #endregion
        }

        #endregion
    }
}
