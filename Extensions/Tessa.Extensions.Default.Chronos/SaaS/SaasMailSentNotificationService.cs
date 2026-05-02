using System;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Extensions.Default.Server.Plugins.Notices;
using Tessa.Platform;
using Tessa.Platform.Validation;
using Tessa.Platform.Web;
using Tessa.Tokens;

namespace Tessa.Extensions.Default.Chronos.SaaS
{
    public sealed class SaasMailSentNotificationService : IMailSentNotificationService, IDisposable
    {
        #region Fields

        private AccessTokenInfo? tokenInfo;
        private readonly AsyncLock asyncLock = new();

        private readonly IWebProxyFactory webProxyFactory;
        private readonly IDiscoveryKeyProvider keyProvider;
        private readonly MailSenderConfig config;

        #endregion

        public SaasMailSentNotificationService(IWebProxyFactory webProxyFactory, IDiscoveryKeyProvider keyProvider, MailSenderConfig config)
        {
            this.webProxyFactory = NotNullOrThrow(webProxyFactory);
            this.keyProvider = NotNullOrThrow(keyProvider);
            this.config = NotNullOrThrow(config);
        }

        public async Task<ValidationResult> NotifyMailSentAsync(MailSenderMessage message, CancellationToken cancellationToken = default)
        {
            var token = await this.TryGetTokenAsync(cancellationToken);

            await using var proxy = await this.webProxyFactory.UseProxyAsync<MailWebProxy>(cancellationToken: cancellationToken);
            return await proxy.NotifyMailSentAsync(message, token, cancellationToken);
        }

        #region IDisposable Implementation

        public void Dispose()
        {
            this.asyncLock.Dispose();
        }

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

            await using var proxy = await this.webProxyFactory.UseProxyAsync<MailWebProxy>(cancellationToken: cancellationToken);
            var jwt = this.keyProvider.GetKey().GenerateAuthToken(PlatformTokenScopes.SaasMail, this.config.SaasAuthTokenExpiration);
            var response = await proxy.GetTokenAsync(jwt, cancellationToken);
            if (!response.ValidationResult.IsSuccessful())
            {
                throw new ValidationException(response.ValidationResult.Build());
            }

            info = this.tokenInfo = NotNullOrThrow(response.TokenInfo);

            return info.Token;
        }

        #endregion
    }
}
