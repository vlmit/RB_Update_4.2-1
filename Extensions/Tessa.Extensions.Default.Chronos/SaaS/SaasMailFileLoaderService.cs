
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Content;
using Tessa.Content.Files;
using Tessa.Extensions.Default.Server.Plugins.Notices;
using Tessa.Notices;
using Tessa.Platform;
using Tessa.Platform.Validation;
using Tessa.Platform.Web;
using Tessa.Tokens;
using Unity;

namespace Tessa.Extensions.Default.Chronos.SaaS
{
    /// <summary>
    /// Mail file content loader for SaaS.
    /// </summary>
    public sealed class SaasMailFileLoaderService :
        IMailFileLoaderService,
        IDisposable
    {
        #region Fields

        private AccessTokenInfo? tokenInfo;
        private readonly AsyncLock asyncLock = new();

        private readonly IWebProxyFactory webProxyFactory;
        private readonly IDiscoveryKeyProvider keyProvider;
        private readonly MailSenderConfig config;

        #endregion

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="webProxyFactory"><inheritdoc cref="IWebProxyFactory" path="/summary"/></param>
        /// <param name="keyProvider"><inheritdoc cref="IDiscoveryKeyProvider" path="/summary"/></param>
        /// <param name="config"><inheritdoc cref="MailSenderConfig" path="/summary"/></param>
        /// <param name="unityDisposableContainer"><inheritdoc cref="IUnityDisposableContainer" path="/summary"/></param>
        public SaasMailFileLoaderService(
            IWebProxyFactory webProxyFactory,
            IDiscoveryKeyProvider keyProvider,
            MailSenderConfig config,
            [OptionalDependency] IUnityDisposableContainer? unityDisposableContainer = null)
        {
            this.webProxyFactory = NotNullOrThrow(webProxyFactory);
            this.keyProvider = NotNullOrThrow(keyProvider);
            this.config = NotNullOrThrow(config);

            unityDisposableContainer?.Register(this);
        }

        #region IMailFileLoaderService Implementation

        /// <inheritdoc/>
        public async Task<CardGetFileContentResponse?> TryLoadContentAsync(
            Guid? cardID,
            Guid? cardTypeID,
            string? cardTypeName,
            MailFile file,
            Func<Stream, CancellationToken, ValueTask> processContentActionAsync,
            Guid? userID = null,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(file);
            ThrowIfNull(processContentActionAsync);
            ThrowIf(cardID, !cardID.HasValue && file.IsVirtual);

            var token = await this.TryGetTokenAsync(cancellationToken);

            await using var proxy = await this.webProxyFactory.UseProxyAsync<ContentWebProxy>(cancellationToken: cancellationToken);
            Stream? contentStream = null;

            try
            {
                var contentResult = await proxy.GetContentAsync(
                    file.VersionID == Guid.Empty
                        ? PlatformResourceTypes.Files
                        : PlatformResourceTypes.FileVersions,
                    file.VersionID == Guid.Empty
                        ? ContentHelper.GetGuidContentID(file.FileID)
                        : ContentHelper.GetGuidContentID(file.VersionID),
                    token,
                    userID,
                    new()
                    {
                        [FileContentHelper.CardIDKey] = $"{cardID:N}",
                        [FileContentHelper.CardTypeIDKey] = $"{cardTypeID:N}",
                        [FileContentHelper.CardTypeNameKey] = cardTypeName,
                        [FileContentHelper.FileIDKey] = $"{file.FileID:N}",
                        [FileContentHelper.FileTypeNameKey] = file.FileTypeName,
                        [FileContentHelper.FileVersionIDKey] = $"{file.VersionID:N}",
                        [FileContentHelper.FileVirtualKey] = $"{file.IsVirtual}",
                    },
                    cancellationToken: cancellationToken);

                contentStream = contentResult.Content;

                await processContentActionAsync(contentStream, cancellationToken);

                var response = new CardGetFileContentResponse
                {
                    Size = contentResult.Size,
                    HasContent = true,
                };
                response.SetSuggestedFileName(contentResult.Name);

                return response;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                var errorResponse = new CardGetFileContentResponse
                {
                    HasContent = false,
                    Size = 0L,
                };

                if (ex is ValidationException validationException)
                {
                    errorResponse.ValidationResult.Add(validationException.Result);
                }
                else
                {
                    errorResponse.ValidationResult.AddException(this, ex);
                }

                return errorResponse;
            }
            finally
            {
                if (contentStream is not null)
                {
                    await contentStream.DisposeAsync();
                }
            }
        }

        #endregion

        #region IDisposable Implementation

        /// <inheritdoc/>
        public void Dispose() => this.asyncLock.Dispose();

        #endregion

        #region Private Methods

        private async Task<string> TryGetTokenAsync(CancellationToken cancellationToken = default)
        {
            var info = this.tokenInfo;
            if (info is not null && !info.IsExpired())
            {
                return info.Token;
            }

            using var _ = await this.asyncLock.EnterAsync(cancellationToken);
            // double check
            if (info is not null && !info.IsExpired())
            {
                return info.Token;
            }

            await using var proxy = await this.webProxyFactory.UseProxyAsync<TokensWebProxy>(cancellationToken: cancellationToken);
            var jwt = this.keyProvider.GetKey().GenerateAuthToken(PlatformTokenScopes.SaasFiles, this.config.SaasAuthTokenExpiration);
            var request = new TokenRequest { Scope = PlatformTokenScopes.SaasFiles };
            var accessToken = await proxy.GetTokenByJwtAuthAsync(PlatformResourceTypes.Files, jwt, request, cancellationToken);

            info = this.tokenInfo = NotNullOrThrow(accessToken);

            return info.Token;
        }

        #endregion
    }
}
