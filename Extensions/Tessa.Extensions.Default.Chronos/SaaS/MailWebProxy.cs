using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Content;
using Tessa.Extensions.Default.Server.Plugins.Notices;
using Tessa.Platform.Runtime;
using Tessa.Platform.Validation;
using Tessa.Platform.Web;
using Tessa.Tokens;
using WebProxy = Tessa.Platform.Web.WebProxy;

namespace Tessa.Extensions.Default.Chronos.SaaS
{
    /// <summary>
    /// Notification mail sent proxy.
    /// </summary>
    public sealed class MailWebProxy : WebProxy
    {
        #region Constructor

        public MailWebProxy() :
            base("api/v1/mail", RuntimeHelper.DefaultServiceName,
                new[]
                {
                    WebRequestFlags.AddAcceptLanguageHeader
                })
        {
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Get content access token for mail sent post actions on server.
        /// </summary>
        /// <param name="jwt">Authorization token.</param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Content access token.</returns>
        public async Task<ContentTokenResponse> GetTokenAsync(
            string jwt,
            CancellationToken cancellationToken = default)
        {
            var request = new ContentTokenRequest { Scope = PlatformTokenScopes.SaasMail };
            var response = await this.SendAsync<ContentTokenResponse>(
                    HttpMethod.Post,
                    "token",
                    WebRequestFlags.PerRequest() + WebRequestFlags.TypedJsonRequest + WebRequestFlags.TypedJsonResponse,
                    request,
                    modifyRequestFuncAsync: ctx =>
                    {
                        ctx.Request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
                        return ValueTask.CompletedTask;
                    },
                    cancellationToken: cancellationToken);

            return NotNullOrThrow(response);
        }

        /// <summary>
        /// Notifies server about mail message sent.
        /// </summary>
        /// <param name="message"><inheritdoc cref="MailSenderMessage" path="/summary"/></param>
        /// <param name="token">Server access token.</param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Operation result.</returns>
        public async Task<ValidationResult> NotifyMailSentAsync(
            MailSenderMessage message,
            string token,
            CancellationToken cancellationToken = default)
        {
            var result = await this.SendAsync<PlainValidationResult>(
                HttpMethod.Post,
                "notify",
                WebRequestFlags.PerRequest() + WebRequestFlags.TypedJsonRequest,
                message.ToMailSentRequest(),
                modifyRequestFuncAsync: ctx =>
                {
                    ctx.Request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    return ValueTask.CompletedTask;
                },
                cancellationToken: cancellationToken);

            return NotNullOrThrow(result).ToValidationResult();
        }

        #endregion
    }
}
