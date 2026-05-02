#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Platform;
using Tessa.Platform.Configuration;
using Tessa.Platform.Encryption;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Shared.Configuration
{
    /// <summary>
    /// Загрузчик конфигурации из зашифрованного файла.
    /// </summary>
    [Order(2)]
    public class EncryptedConfigurationLoader : IConfigurationItemSourceLoader
    {
        #region Public Constants

        /// <summary>
        /// Константа для регистраций в DI.
        /// </summary>
        public const string Key = "decrypt-aes";

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public ValueTask<IConfigurationBuilderItemSource?> GetItemSourceAsync(
            Dictionary<string, object?> parameters,
            IConfigurationBuilderContext context,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(parameters);
            ThrowIfNull(context);

            return new(
                parameters.TryGet<object>("file") is string { Length: > 0 } filePath
                && parameters.TryGet<object>("certificate") is string { Length: > 0 } certificatePath
                && context.ServiceProvider?.TryResolve<IEncryptionService>() is { } encryptionService
                && context.ServiceProvider.TryResolve<IEncryptionCertificateLoader>() is { } encryptionCertificateLoader
                    ? new SingleConfigurationBuilderItemSource(
                        new EncryptedConfigurationBuilderItem(
                            filePath,
                            certificatePath,
                            context.CurrentFolder,
                            encryptionService,
                            encryptionCertificateLoader,
                            certificatePassword: parameters.TryGet<string>("password"),
                            certificateKeyPemFilePath: parameters.TryGet<string>("certificateKey"),
                            isUnique: parameters.TryGet<object>(FileConfigurationBuilderItemSource.AlwaysOptionKey) is true))
                    : null);
        }

        #endregion
    }
}
