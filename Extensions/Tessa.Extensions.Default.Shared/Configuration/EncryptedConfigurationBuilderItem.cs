#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Platform.Configuration;
using Tessa.Platform.Encryption;
using Tessa.Platform.IO;

namespace Tessa.Extensions.Default.Shared.Configuration
{
    /// <summary>
    /// Объект, соответствующий зашифрованному конфигурационному файлу.
    /// </summary>
    /// <param name="filePath"><inheritdoc cref="FilePath" path="/summary"/></param>
    /// <param name="certificatePath"><inheritdoc cref="CertificatePath" path="/summary"/></param>
    /// <param name="baseCurrentFolder"><inheritdoc cref="BaseCurrentFolder" path="/summary"/></param>
    /// <param name="encryptionService"><inheritdoc cref="EncryptionService" path="/summary"/></param>
    /// <param name="encryptionCertificateLoader"><inheritdoc cref="EncryptionCertificateLoader" path="/summary"/></param>
    /// <param name="certificatePassword"><inheritdoc cref="CertificatePassword" path="/summary"/></param>
    /// <param name="certificateKeyPemFilePath"><inheritdoc cref="CertificateKeyPemFilePath" path="/summary"/></param>
    /// <param name="isUnique"><inheritdoc cref="IsUnique" path="/summary"/></param>
    /// <remarks>
    /// Два объекта <see cref="EncryptedConfigurationBuilderItem"/> равны при совпадении типов
    /// и значений свойств <see cref="FilePath"/> и <see cref="CertificatePath"/> без учёта регистра,
    /// если ни у одного из объектов не установлен признак <see cref="IsUnique"/>.
    /// </remarks>
    public class EncryptedConfigurationBuilderItem(
        string filePath,
        string certificatePath,
        string? baseCurrentFolder,
        IEncryptionService encryptionService,
        IEncryptionCertificateLoader encryptionCertificateLoader,
        string? certificatePassword = null,
        string? certificateKeyPemFilePath = null,
        bool isUnique = false)
        : ConfigurationBuilderItemBase, IFileConfigurationEqualityInfo
    {
        #region Properties

        /// <summary>
        /// Путь к зашифрованному файлу конфигурации, который может быть относительным путём и содержать символы.
        /// </summary>
        public string FilePath { get; } = NotEmptyOrThrow(filePath);

        /// <summary>
        /// Путь к файлу сертификата, который может быть относительным путём и содержать символы.
        /// Относительный путь будет рассчитан от папки файла, расшифровка которого производится <see cref="FilePath"/>.
        /// </summary>
        public string CertificatePath { get; } = NotEmptyOrThrow(certificatePath);

        /// <summary>
        /// Текущая папка <see cref="IConfigurationBuilderContext.CurrentFolder"/> на момент обработки директивы <see cref="IncludeConfigurationDirective"/>,
        /// которая создаёт данный объект в <see cref="EncryptedConfigurationLoader"/>.
        /// </summary>
        public string? BaseCurrentFolder { get; } = baseCurrentFolder;

        /// <summary>
        /// Пароль к сертификату или <c>null</c>/пустая строка, если сертификат не имеет пароля.
        /// </summary>
        public string? CertificatePassword { get; } = certificatePassword;

        /// <summary>
        /// Путь к файлу приватного ключа, который может быть относительным путём и содержать символы.
        /// Относительный путь будет рассчитан от папки файла, расшифровка которого производится <see cref="FilePath"/>.
        /// Актуален только для формата сертификата PEM.
        /// </summary>
        public string? CertificateKeyPemFilePath { get; } = certificateKeyPemFilePath;

        #endregion

        #region Protected Declarations

        /// <inheritdoc cref="IEncryptionService"/>
        protected IEncryptionService EncryptionService { get; } = NotNullOrThrow(encryptionService);

        /// <inheritdoc cref="IEncryptionService"/>
        protected IEncryptionCertificateLoader EncryptionCertificateLoader { get; } = NotNullOrThrow(encryptionCertificateLoader);

        /// <summary>
        /// Актуальный полный путь к зашифрованному файлу конфигурации или <c>null</c>, если актуальный путь неизвестен на текущий момент времени.
        /// </summary>
        /// <remarks>
        /// Заполняется в стандартной реализации метода <see cref="PrepareContextCoreAsync"/>.
        /// </remarks>
        protected string? ResolvedFilePath { get; set; }

        /// <summary>
        /// Получает путь к файлу с учетом относительных путей и текущую папку для установки в свойстве <see cref="IConfigurationBuilderContext.CurrentFolder"/>.
        /// </summary>
        /// <param name="filePath">Исходный путь к файлу. Может быть относительным путём. Не должен содержать символы.</param>
        /// <param name="defaultCurrentFolder">
        /// Папка по умолчанию, если <see cref="IConfigurationFileProvider"/> недоступен
        /// или его метод <see cref="IConfigurationFileProvider.GetDirectoryNameAsync"/> вернул <c>null</c>/пустую строку.
        /// </param>
        /// <param name="context"><inheritdoc cref="IConfigurationBuilderContext" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Актуальный путь к зашифрованному файлу и текущая папка для установки в свойстве <see cref="IConfigurationBuilderContext.CurrentFolder"/>.</returns>
        protected virtual async ValueTask<(string ActualFilePath, string? CurrentFolder)> ResolveFilePathAsync(
            string filePath,
            string? defaultCurrentFolder,
            IConfigurationBuilderContext context,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(context.FileProvider);
            cancellationToken.ThrowIfCancellationRequested();

            if (filePath.Length > 1 && filePath[0] == '@')
            {
                // "at sign" is ignored, path is relative to the current configuration file anyway
                filePath = filePath[1..];
            }

            // subfolder path in the setting
            var fileDirectoryName = await context.FileProvider.GetDirectoryNameAsync(filePath, context, cancellationToken).ConfigureAwait(false);

            // subfolder above is relative either to (1) defaultCurrentFolder (encrypted file path when parsing certificate path),
            // or to (2) BaseCurrentFolder (current folder containing config file, which includes encrypted file)
            var currentFolder = !string.IsNullOrEmpty(defaultCurrentFolder)
                ? await context.FileProvider.CombinePathAsync(
                    defaultCurrentFolder,
                    fileDirectoryName, context, cancellationToken).ConfigureAwait(false)
                : await context.FileProvider.CombinePathAsync(
                    !string.IsNullOrEmpty(this.BaseCurrentFolder) ? this.BaseCurrentFolder : ConfigurationHelper.ConfigRoot.ResolvePath(),
                    fileDirectoryName, context, cancellationToken).ConfigureAwait(false);

            var fileName = await context.FileProvider.GetFileNameAsync(filePath, context, cancellationToken).ConfigureAwait(false);

            var actualFilePath = await context.FileProvider.CombinePathAsync(currentFolder, fileName, context, cancellationToken).ConfigureAwait(false);
            ThrowIfNullOrEmpty(actualFilePath);

            // returned paths are normalized according to FileProvider logic
            return (actualFilePath, currentFolder);
        }

        /// <summary>
        /// Возвращает сертификат, загруженный по указанному пути.
        /// </summary>
        /// <param name="certificatePath">Полный путь к файлу сертификата.</param>
        /// <param name="certificatePassword">Пароль к файлу сертификата или <c>null</c>/пустая строка, если пароль не задан.</param>
        /// <param name="certificateKeyPemFilePath">Путь к файлу приватного ключа. Актуален только для формата сертификата PEM.</param>
        /// <param name="context"><inheritdoc cref="IConfigurationBuilderContext" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Загруженный сертификат.</returns>
        protected virtual ValueTask<X509Certificate2> LoadCertificateAsync(
            string certificatePath,
            string? certificatePassword,
            string? certificateKeyPemFilePath,
            IConfigurationBuilderContext context,
            CancellationToken cancellationToken = default) =>
            this.EncryptionCertificateLoader.LoadCertificateAsync(certificatePath, certificatePassword, certificateKeyPemFilePath, cancellationToken);

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override string FilePathForError => this.ResolvedFilePath ?? this.FilePath;

        /// <inheritdoc/>
        public override string ToString() => this.FilePath;

        /// <inheritdoc/>
        protected override bool EqualsCore(IConfigurationBuilderItem other) =>
            !this.IsUnique
            && (other is EncryptedConfigurationBuilderItem { IsUnique: false } otherItem
                && string.Equals(this.FilePath, otherItem.FilePath, StringComparison.OrdinalIgnoreCase)
                && string.Equals(this.CertificatePath, otherItem.CertificatePath, StringComparison.OrdinalIgnoreCase)
                && string.Equals(this.BaseCurrentFolder, otherItem.BaseCurrentFolder, StringComparison.OrdinalIgnoreCase)
                || this.ResolvedFilePath is not null
                && other is IFileConfigurationEqualityInfo { IsUnique: false } otherInfo
                && string.Equals(this.ResolvedFilePath, otherInfo.FilePath, StringComparison.OrdinalIgnoreCase));

        /// <inheritdoc/>
        public override int GetHashCode() => this.IsUnique ? base.GetHashCode() : HashCode.Combine(this.FilePath, this.CertificatePath, this.BaseCurrentFolder);

        /// <inheritdoc/>
        protected override async ValueTask PrepareContextCoreAsync(
            IConfigurationBuilderContext context,
            CancellationToken cancellationToken = default)
        {
            if (context.JsonSerializer is null || context.FileProvider is null)
            {
                // LoadStorageCoreAsync will be unable to parse the file anyway
                return;
            }

            var filePath = context.SymbolManager is { } sm ? sm.ReplaceSymbols(this.FilePath) : this.FilePath;
            var (actualFilePath, currentFolder) = await this.ResolveFilePathAsync(filePath, null, context, cancellationToken).ConfigureAwait(false);

            this.ResolvedFilePath = actualFilePath;
            context.CurrentFolder = currentFolder;
        }

        /// <inheritdoc/>
        protected override async ValueTask<Dictionary<string, object?>?> LoadStorageCoreAsync(
            IConfigurationBuilderContext context,
            CancellationToken cancellationToken = default)
        {
            if (context.JsonSerializer is null || context.FileProvider is null || this.ResolvedFilePath is not { Length: > 0 } filePath)
            {
                return null;
            }

            string certificatePath;
            string? certificatePassword;
            string? certificateKeyPemFilePath;
            if (context.SymbolManager is { } sm)
            {
                certificatePath = sm.ReplaceSymbols(this.CertificatePath);
                certificatePassword = sm.ReplaceSymbols(this.CertificatePassword);
                certificateKeyPemFilePath = sm.ReplaceSymbols(this.CertificateKeyPemFilePath);
            }
            else
            {
                certificatePath = this.CertificatePath;
                certificatePassword = this.CertificatePassword;
                certificateKeyPemFilePath = this.CertificateKeyPemFilePath;
            }

            // current folder for certificate path defaults to a current folder of file path to decrypt (it's set up in PrepareContextCoreAsync)
            (certificatePath, _) = await this.ResolveFilePathAsync(certificatePath, context.CurrentFolder, context, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(certificateKeyPemFilePath))
            {
                (certificateKeyPemFilePath, _) = await this.ResolveFilePathAsync(
                    certificateKeyPemFilePath, context.CurrentFolder, context, cancellationToken).ConfigureAwait(false);
            }

            string json;
            var outputStream = StreamHelper.AcquireMemoryStream(8192);
            await using (outputStream.ConfigureAwait(false))
            {
                using (var certificate = await this.LoadCertificateAsync(
                           certificatePath, certificatePassword, certificateKeyPemFilePath, context, cancellationToken).ConfigureAwait(false))
                {
                    var inputStream = await context.FileProvider.OpenReadAsync(filePath, context, cancellationToken).ConfigureAwait(false);
                    await using var _ = inputStream.ConfigureAwait(false);

                    await this.EncryptionService.DecryptDataAsync(certificate, inputStream, outputStream, cancellationToken).ConfigureAwait(false);
                }

                outputStream.Position = 0L;

                using (var streamReader = new StreamReader(outputStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true))
                {
                    json = await streamReader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
                }
            }

            return await context.JsonSerializer.DeserializeAsync(json, context, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        protected override ValueTask FinalizeContextCoreAsync(
            IConfigurationBuilderContext context,
            CancellationToken cancellationToken = default)
        {
            context.CurrentFolder = null;
            return ValueTask.CompletedTask;
        }

        #endregion

        #region IFileConfigurationEqualityInfo Members

        /// <inheritdoc/>
        public bool IsUnique { get; } = isUnique;

        /// <inheritdoc/>
        string IFileConfigurationEqualityInfo.FilePath => this.ResolvedFilePath ?? string.Empty;
        // when checking equality of two items, the duplicate should be skipped, only if there is the same file by full path that was processed beforehand

        #endregion
    }
}
