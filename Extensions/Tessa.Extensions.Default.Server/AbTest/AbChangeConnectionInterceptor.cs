#nullable enable
using System.Threading;
using System.Threading.Tasks;
using Tessa.Views;

namespace Tessa.Extensions.Default.Server.AbTest
{
    /// <summary>
    /// Перехватчик представлений осуществляющий подмену соединения, на котором требуется выполнить указанное представление.
    /// </summary>
    public sealed class AbChangeConnectionInterceptor() : ViewInterceptorBase(["PutInterceptedViewAliasHere"])
    {
        #region Base Overrides

        /// <inheritdoc />
        public override ValueTask<ITessaViewResult> GetDataAsync(ITessaViewRequest request, CancellationToken cancellationToken = default)
        {
            var view = this.GetInterceptedView(request.ViewAlias);
            request.ConnectionAlias = "test";
            return view.GetDataAsync(request, cancellationToken);
        }

        #endregion
    }
}
