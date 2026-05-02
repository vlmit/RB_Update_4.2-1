#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Properties.Resharper;
using Tessa.Roles;
using Tessa.Scheme;
using Tessa.Views;
using Tessa.Views.Metadata;

namespace Tessa.Extensions.Default.Server.AbTest
{
    /// <summary>
    /// Провайдер для тестового представления <c>AbTransientView</c>.
    /// </summary>
    public sealed class AbTransientViewProvider :
        IExtraViewListProvider
    {
        #region TransientView Private Class

        private sealed class TransientView : ITessaViewWithAccessControl
        {
            #region Fields and Constants

            private const string ViewAlias = "AbTransientView";

            private readonly Lazy<ViewMetadata> metadata = new(CreateViewMetadata, LazyThreadSafetyMode.PublicationOnly);

            [UsedImplicitly]
            private static readonly Guid[] roleIdentifiers =
            [
                RoleHelper.AllEmployeesDynamicRoleID
            ];

            #endregion

            #region Private Methods

            private static ViewMetadata CreateViewMetadata() =>
                new()
                {
                    Alias = ViewAlias,
                    Caption = "Transient view example",
                    Columns =
                    {
                        new ViewColumnMetadata { Alias = "Name", Caption = "Caption", SchemeType = SchemeType.String },
                        new ViewColumnMetadata { Alias = "Count", Caption = "Quantity", SchemeType = SchemeType.Int32 }
                    },
                    Parameters =
                    {
                        new ViewParameterMetadata { Alias = "Name", Caption = "Caption", SchemeType = SchemeType.String },
                        new ViewParameterMetadata { Alias = "Count", Caption = "Quantity", SchemeType = SchemeType.Int32 }
                    }
                };

            private static List<List<object?>> GetRows(ITessaViewRequest request)
            {
                var count = request.GetFirstParameterValue<int>("Count");
                var name = request.GetFirstParameterValue<string>("Name");

                var result = new List<List<object?>>(count);
                for (var i = 0; i < count; i++)
                {
                    result.Add([$"{name}: {i}", i]);
                }

                return result;
            }

            #endregion

            #region ITessaView Members

            /// <inheritdoc/>
            public string Alias => ViewAlias;

            /// <inheritdoc cref="ITessaView.GetMetadataAsync" />
            public ValueTask<IViewMetadata> GetMetadataAsync(CancellationToken cancellationToken = default) => new(this.metadata.Value);

            /// <inheritdoc/>
            public ValueTask<ITessaViewResult> GetDataAsync(ITessaViewRequest request, CancellationToken cancellationToken = default) =>
                new(new TessaViewResult
                {
                    Columns = { ("Name", SchemeType.String), ("Count", SchemeType.Int32) },
                    Rows = GetRows(request)
                });

            #endregion

            #region ITessaViewAccess Members

            // разкомментируйте, чтобы роли проверялись; если метод отсутствует, то представление доступно только администраторам
            // public ValueTask<IReadOnlyList<Guid>> GetRoleIdentifiersAsync(CancellationToken cancellationToken = default) => new(roleIdentifiers);

            #endregion
        }

        #endregion

        #region Fields

        private readonly TransientView cachedView = new();

        #endregion

        #region IExtraViewListProvider Members

        /// <inheritdoc/>
        public ValueTask<IReadOnlyList<ITessaView>> GetExtraViewsAsync(ViewDatabaseInfo defaultDatabaseInfo, CancellationToken cancellationToken = default) =>
            new([this.cachedView]);

        #endregion
    }
}
