#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Platform.Runtime;

namespace Tessa.Extensions.Default.Server.Login
{
    /// <summary>
    /// Предоставляет объект, который выполняет конфигурацию настроек типа
    /// двухфакторной аутентификации с использованием одноразового пароля на основе времени.
    /// </summary>
    public sealed class TwoFactorAuthTotpConfigurator :
        ITwoFactorAuthConfigurator
    {
        #region ITwoFactorAuthConfigurator Implementation

        /// <inheritdoc/>
        public ValueTask GetTypeSettingsAsync(Dictionary<string, object?> settings, CancellationToken cancellationToken = default)
        {
            if (settings.TryGetValue("Key", out var value)
                && value is string { Length: > 2 } key)
            {
                settings["Key"] = $"{key[..3]}...";
            }

            settings["Uri"] = null;

            return ValueTask.CompletedTask;
        }

        #endregion;
    }
}
