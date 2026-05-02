using System;
using System.Threading.Tasks;
using Tessa.UI.Views;

namespace Tessa.Extensions.Default.Client.AbTest
{
    /// <summary>
    /// Расширение позволяющее имитировать задержку получения данных представлением.
    /// </summary>
    /// <remarks>
    /// У расширения есть конфигуратор <see cref="AbGetDataWithDelayExtensionConfigurator"/>
    /// </remarks>
    public sealed class AbGetDataWithDelayExtension : IWorkplaceViewComponentExtension
    {
        #region IWorkplaceViewComponentExtension Members

        /// <inheritdoc/>
        public void Initialize(IWorkplaceViewComponent model)
        {
            var wrappedGetDataAsync = model.GetDataAsync;
            model.GetDataAsync = async (component, request, ct) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(3), ct);
                return await wrappedGetDataAsync(component, request, ct);
            };
        }

        #endregion
    }
}
