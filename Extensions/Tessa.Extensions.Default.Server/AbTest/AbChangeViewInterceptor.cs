#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Views;

namespace Tessa.Extensions.Default.Server.AbTest
{
    /// <summary>
    /// Пример реализации перехватчика представлений, который вызывает представление с алиасом <see cref="OtherViewAlias"/> на каждый запрос к другому представлению,
    /// алиас которого передан в конструкторе.
    /// </summary>
    /// <param name="viewService">
    /// Сервис представлений. Передаётся функция для отложенного запроса зависимости, иначе при создании перехватчика возникла бы циклическая связь.
    /// </param>
    public sealed class AbChangeViewInterceptor(Func<IViewService> viewService)
        : ViewInterceptorBase(["PutInterceptedViewAliasHere"])
    {
        #region Constants

        /// <summary>
        /// Алиас вызываемого представления.
        /// </summary>
        private const string OtherViewAlias = "OtherViewAlias";

        #endregion

        #region Fields

        private readonly Func<IViewService> viewService = NotNullOrThrow(viewService);

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async ValueTask<ITessaViewResult> GetDataAsync(ITessaViewRequest request, CancellationToken cancellationToken = default)
        {
            var view = await this.viewService().GetByNameAsync(OtherViewAlias, cancellationToken)
                ?? throw new InvalidOperationException($"Can't find view by alias \"{OtherViewAlias}\".");

            request.ViewAlias = OtherViewAlias;
            return await view.GetDataAsync(request, cancellationToken);
        }

        #endregion
    }
}
