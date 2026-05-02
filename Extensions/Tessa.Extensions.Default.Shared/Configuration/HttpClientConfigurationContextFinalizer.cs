#nullable enable

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Tessa.Platform;
using Tessa.Platform.Configuration;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Shared.Configuration
{
    /// <summary>
    /// Класс для освобождения ресурсов, после использования HttpClient.
    /// </summary>
    [Order(2)]
    public class HttpClientConfigurationContextFinalizer : ConfigurationContextFinalizerBase
    {
        #region Base Overrides

        /// <inheritdoc/>
        protected override async ValueTask FinalizeCoreAsync(IConfigurationBuilderContext context)
        {
            var httpClient = context.Info.TryGet<HttpClient?>(DefaultConfigurationHelper.HttpClientKey);
            if (httpClient is null)
            {
                return;
            }

            try
            {
                httpClient.Dispose();
            }
            catch (OperationCanceledException)
            {
                // игнорируем
            }
            catch (Exception e)
            {
                await context.ReportExceptionAsync(e).ConfigureAwait(false);
            }

            context.Info.Remove(DefaultConfigurationHelper.HttpClientKey);
        }

        #endregion
    }
}
