#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tessa.Platform;
using Tessa.Platform.Configuration;
using Tessa.Platform.Redis;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Shared.Configuration
{
    /// <summary>
    /// Класс для освобождения ресурсов, после подключения к Redis.
    /// </summary>
    [Order(1)]
    public class RedisConfigurationContextFinalizer : ConfigurationContextFinalizerBase
    {
        #region Base Overrides
        
        /// <inheritdoc/>
        protected override async ValueTask FinalizeCoreAsync(IConfigurationBuilderContext context)
        {
            var connections = context.Info.TryGet<Dictionary<string, RedisConnectionProvider>>(
                DefaultConfigurationHelper.RedisConnectionsKey);

            if (connections is null)
            {
                return;
            }

            foreach (var (_, provider) in connections)
            {
                try
                {
                    await provider.DisposeAsync().ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // игнорируем
                }
                catch (Exception ex)
                {
                    // реализация RedisConnectionProvider не выбрасывает исключение,
                    // которое может произойти при закрытии соединения с Redis,
                    // но другой освобождаемый объект мог бы выбросить
                    await context.ReportExceptionAsync(ex).ConfigureAwait(false);
                }
            }

            context.Info.Remove(DefaultConfigurationHelper.RedisConnectionsKey);
        }
        
        #endregion
    }
}
