#nullable enable
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Files;
using Tessa.Platform.Runtime;
using Tessa.Platform.Storage;
using Unity;

namespace Tessa.Extensions.Default.Client.AbTest
{
    public class AbExternalFileSource(IFileCache cache, ISession session, [OptionalDependency] IFileManager? manager = null)
        : FileSource(cache, session, manager)
    {
        #region Base Overrides

        /// <inheritdoc/>
        protected override async ValueTask<IFile> CreateFileCoreAsync(IFileCreationToken token, IFileContent? content = null, CancellationToken cancellationToken = default)
        {
            // здесь возможно исключение, связанное с параметрами метода
            var typedToken = (AbExternalFileCreationToken) token;

            var name = NotEmptyOrThrow(token.Name);
            IFileContent? allocatedContent = null;
            var actualContent = content ?? (allocatedContent = await this.Cache.AllocateAsync(name, cancellationToken: cancellationToken).ConfigureAwait(false));

            try
            {
                // все свойства токена проверяются в конструкторе
                var file = new AbExternalFile(
                    token.ID ?? Guid.NewGuid(),
                    name,
                    token.Size,
                    token.Category,
                    NotNullOrThrow(token.Type),
                    actualContent,
                    source: this,
                    token.Modified,
                    token.ModifiedByID,
                    token.ModifiedByName,
                    token.Created,
                    token.CreatedByID,
                    token.CreatedByName,
                    token.Permissions.Clone(),
                    token.IsLocal,
                    hash: token.Hash,
                    description: typedToken.Description);

                file.Options.SetStorage(StorageHelper.Clone(token.Options));
                file.Info.SetStorage(StorageHelper.Clone(token.Info));
                file.RequestInfo.SetStorage(StorageHelper.Clone(token.RequestInfo));

                allocatedContent = null;
                return file;
            }
            finally
            {
                if (allocatedContent is not null)
                {
                    await allocatedContent.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        /// <inheritdoc/>
        protected override ValueTask<IFileCreationToken> GetFileCreationTokenCoreAsync(CancellationToken cancellationToken = default)
        {
            var user = this.Session.User;
            var utcNow = DateTime.UtcNow;
            return new(new AbExternalFileCreationToken
            {
                Modified = utcNow,
                ModifiedByID = user.ID,
                ModifiedByName = user.Name,
                Created = utcNow,
                CreatedByID = user.ID,
                CreatedByName = user.Name
            });
        }

        /// <inheritdoc/>
        protected override async ValueTask<IFileContentResponse> GetContentCoreAsync(IFileContentRequest request, CancellationToken cancellationToken = default)
        {
            // здесь возможно исключение, если передан файл от другого источника
            var typedFile = (AbExternalFile) request.Version.File;

            if (request.ProcessContentActionAsync is { } processContentActionAsync)
            {
                await processContentActionAsync(
                    new MemoryStream(Encoding.UTF8.GetBytes(typedFile.Description ?? string.Empty)),
                    cancellationToken).ConfigureAwait(false);
            }

            return new FileContentResponse(request.Version);
        }

        #endregion
    }
}
