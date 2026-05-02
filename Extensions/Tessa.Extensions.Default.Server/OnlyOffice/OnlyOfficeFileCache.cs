#nullable enable

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Tessa.FileConverters;
using Tessa.Files;
using Tessa.Platform.Data;
using Tessa.Platform.IO;
using Tessa.Platform.Runtime;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.OnlyOffice
{
    /// <inheritdoc />
    public sealed class OnlyOfficeFileCache(
        IDbScope dbScope,
        ISession session,
        IOnlyOfficeFileCacheInfoStrategy cacheInfoStrategy,
        IFileConverterCache fileConverterCache,
        IFileConverterComposer fileConverterComposer,
        IOnlyOfficeLockingStrategy lockingStrategy)
        : IOnlyOfficeFileCache
    {
        #region Fields

        private readonly IDbScope dbScope = NotNullOrThrow(dbScope);

        private readonly ISession session = NotNullOrThrow(session);

        private readonly IOnlyOfficeFileCacheInfoStrategy cacheInfoStrategy = NotNullOrThrow(cacheInfoStrategy);

        private readonly IFileConverterCache fileConverterCache = NotNullOrThrow(fileConverterCache);

        private readonly IFileConverterComposer fileConverterComposer = NotNullOrThrow(fileConverterComposer);

        private readonly IOnlyOfficeLockingStrategy lockingStrategy = NotNullOrThrow(lockingStrategy);

        #endregion

        #region IOnlyOfficeFileCache Members

        /// <inheritdoc />
        public async ValueTask<ValidationResult> CreateAsync(
            Guid id,
            Guid sourceFileVersionID,
            string sourceFileName,
            Stream stream,
            CancellationToken cancellationToken = default)
        {
            ValidationResult result;

            await using var _ = this.dbScope.Create();
            await using var acquiredLock = await this.lockingStrategy.AcquireLockAsync(id, readerLock: false, cancellationToken);
            using (var file = TempFile.Acquire(sourceFileName))
            {
                await using (var fileStream = FileHelper.Create(file.Path))
                {
                    await stream.CopyToAsync(fileStream, FileHelper.DefaultFileBufferSize, cancellationToken);
                }

                // пользователь без прав админа не может изменять карточку "Кэш файлов", поскольку это синглтон;
                // временно добавляем ему права администратора
                await using var __ = this.session.User.IsAdministrator()
                    ? null
                    : SessionContext.Create(this.session.CreateNestedSessionToken(this.session.User.ID, this.session.User.Name));

                await using var fileContent = await RemoteFileContent.FromFilePathAsync(
                    file.Path,
                    cancellationToken: cancellationToken);

                var requestHash = this.CalculateRequestHash(id, sourceFileVersionID);
                result = await this.fileConverterCache.StoreFileAsync(
                    sourceFileVersionID,
                    requestHash,
                    id,
                    sourceFileName,
                    fileContent,
                    cancellationToken: cancellationToken);
            }

            if (!result.IsSuccessful)
            {
                return result;
            }

            try
            {
                // для совместного редактирования уже будет строка
                var info = await this.cacheInfoStrategy.TryGetInfoAsync(id, cancellationToken);

                if (info is null)
                {
                    await this.cacheInfoStrategy.InsertAsync(
                        new OnlyOfficeFileCacheInfo
                        {
                            ID = id,
                            SourceFileVersionID = sourceFileVersionID,
                            SourceFileName = sourceFileName,
                            CreatedByID = this.session.User.ID,
                            LastAccessTime = DateTime.UtcNow,
                        },
                        cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                result = ValidationResult.Aggregate(ValidationResult.FromException(this, ex), result);
            }

            return result;
        }

        /// <inheritdoc />
        public async ValueTask<(ValidationResult Result, Func<CancellationToken, ValueTask<Stream>>? GetContentStreamFunc, long Size)> GetContentAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            await using var acquiredLock = await this.lockingStrategy.AcquireLockAsync(id, readerLock: true, cancellationToken);
            var info = await this.cacheInfoStrategy.TryGetInfoAsync(id, cancellationToken);
            ThrowIfNull(info);
            var requestHash = this.CalculateRequestHash(id, info.SourceFileVersionID);
            var convertedFileID = await this.fileConverterCache.TryGetConvertedFileIDAsync(info.SourceFileVersionID, requestHash, cancellationToken)
                ?? throw new InvalidOperationException(
                    $"Can't resolve OnlyOffice converted file ID from cache by {nameof(info.SourceFileVersionID)}={info.SourceFileVersionID:B}" +
                    $", hash={Convert.ToBase64String(requestHash)}.");
            return await this.fileConverterCache.GetFileAsync(convertedFileID, cancellationToken);
        }

        /// <inheritdoc />
        public async ValueTask<ValidationResult> DeleteAsync(
            Guid id,
            Guid? sourceFileVersionID = null,
            CancellationToken cancellationToken = default)
        {
            var result = ValidationResult.Empty;

            await using var acquiredLock = await this.lockingStrategy.AcquireLockAsync(id, readerLock: false, cancellationToken);
            await using var _ = this.dbScope.Create();

            if (!sourceFileVersionID.HasValue)
            {
                var info = await this.cacheInfoStrategy.TryGetInfoAsync(id, cancellationToken);
                sourceFileVersionID = info?.SourceFileVersionID;
            }

            if (sourceFileVersionID.HasValue)
            {
                // пользователь без прав админа не может изменять карточку "Кэш файлов", поскольку это синглтон;
                // временно добавляем ему права администратора
                await using var __ = this.session.User.IsAdministrator()
                    ? null
                    : SessionContext.Create(this.session.CreateNestedSessionToken(this.session.User.ID, this.session.User.Name));

                var requestHash = this.CalculateRequestHash(id, sourceFileVersionID.Value);
                var (convertResult, _) = await this.fileConverterCache
                    .DeleteFileAsync(sourceFileVersionID.Value, requestHash, cancellationToken);

                if (!convertResult.IsSuccessful)
                {
                    return convertResult;
                }

                result = convertResult;
            }

            try
            {
                await this.cacheInfoStrategy.DeleteAsync(id, CancellationToken.None);
            }
            catch (Exception ex)
            {
                result = ValidationResult.Aggregate(ValidationResult.FromException(this, ex), result);
            }

            return result;
        }

        #endregion

        #region Private Methods

        private byte[] CalculateRequestHash(Guid id, Guid sourceFileVersionID)
        {
            var request = new FileConverterRequest { EventName = "OnlyOffice", CardID = id, Parameters = { ["SessionID"] = id }, VersionID = sourceFileVersionID };
            return this.fileConverterComposer.CalculateHash(request);
        }

        #endregion
    }
}
