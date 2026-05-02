using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ITfoxtec.Identity.Saml2;
using ITfoxtec.Identity.Saml2.MvcCore;
using ITfoxtec.Identity.Saml2.Schemas;
using ITfoxtec.Identity.Saml2.Schemas.Metadata;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using NLog;
using Tessa.Cards;
using Tessa.Platform;
using Tessa.Platform.Data;
using Tessa.Platform.Runtime;
using Tessa.Roles;
using Tessa.Web;
using Tessa.Web.Client;
using Tessa.Web.Client.Services;
using Tessa.Web.Services;

namespace Tessa.Extensions.Default.Server.Web.Services
{
    /// <inheritdoc />
    public class SamlService(
        IDbScope dbScope,
        ISessionServer sessionServer,
        ICardRepository cardRepository,
        ITessaServerSettings serverSettings,
        IOptions<WebOptions> webOptions)
        : ISamlService
    {
        #region Fields

        private readonly IDbScope dbScope = NotNullOrThrow(dbScope);

        private readonly ISessionServer sessionServer = NotNullOrThrow(sessionServer);

        private readonly ICardRepository cardRepository = NotNullOrThrow(cardRepository);

        private readonly ITessaServerSettings serverSettings = NotNullOrThrow(serverSettings);

        private readonly IOptions<WebOptions> webOptions = NotNullOrThrow(webOptions);

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        #endregion

        #region Protected Methods

        private static void AppendTokenCookie(ISamlContext context, string value, DateTime expires, string tokenCookiesName) =>
            WebClientHelper.AppendTokenCookie(
                value,
                expires,
                context.ClientOptions?.GuyFawkesAuth,
                context.Options?.CookiesSameSite ?? default,
                NotNullOrThrow(context.Request),
                NotNullOrThrow(context.Response),
                tokenCookiesName,
                !context.IsDevelopmentHotEnvironment);

        #endregion

        #region ISamlService Members

        /// <inheritdoc />
        public ValueTask<Saml2Metadata> GetMetadataAsync(ISamlContext context, CancellationToken cancellationToken = default)
        {
            Saml2Configuration samlConfig = NotNullOrThrow(context.SamlConfig);
            string? siteUrl = context.SiteUrl;
            var entityDescriptor = new EntityDescriptor(samlConfig)
            {
                SPSsoDescriptor = new SPSsoDescriptor
                {
                    AuthnRequestsSigned = true,
                    WantAssertionsSigned = true,
                    SigningCertificates = new[] { samlConfig.SigningCertificate },
                    EncryptionCertificates = samlConfig.DecryptionCertificates,
                    NameIDFormats = new[] { NameIdentifierFormats.Unspecified, NameIdentifierFormats.Transient },
                    AssertionConsumerServices = new[]
                    {
                        new AssertionConsumerService
                        {
                            Binding = ProtocolBindings.HttpPost,
                            Location = new Uri($"{siteUrl}/SAML/AssertionConsumerService")
                        },
                    },
                    SingleLogoutServices = new[]
                    {
                        new SingleLogoutService
                        {
                            Binding = ProtocolBindings.HttpPost,
                            Location = new Uri($"{siteUrl}/SAML/SingleLogout"),
                            ResponseLocation = new Uri($"{siteUrl}/SAML/LoggedOut")
                        },
                        new SingleLogoutService
                        {
                            Binding = ProtocolBindings.HttpRedirect,
                            Location = new Uri($"{siteUrl}/SAML/SingleLogout"),
                            ResponseLocation = new Uri($"{siteUrl}/SAML/LoggedOut")
                        },
                    },
                }
            };
            return new(new Saml2Metadata(entityDescriptor).CreateMetadata());
        }

        /// <inheritdoc />
        public ValueTask<Saml2RedirectBinding> LoginAsync(ISamlContext context, string returnUrl, CancellationToken cancellationToken = default)
        {
            Saml2Configuration samlConfig = NotNullOrThrow(context.SamlConfig);
            string base64ReturnUrl = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(returnUrl));
            var binding = new Saml2RedirectBinding();
            binding.SetRelayStateQuery(new Dictionary<string, string> { { "ReturnUrl", base64ReturnUrl } });
            var request = new Saml2AuthnRequest(samlConfig)
            {
                NameIdPolicy = MapDictionaryToNameIdPolicy(context.NameIDPolicy)
            };
            return new(binding.Bind(request));
        }

        /// <inheritdoc />
        public async ValueTask<string?> AssertionConsumerServiceAsync(ISamlContext context, CancellationToken cancellationToken = default)
        {
            Saml2Configuration samlConfig = NotNullOrThrow(context.SamlConfig);
            var binding = new Saml2PostBinding();
            var saml2AuthnResponse = new Saml2AuthnResponse(samlConfig);

            binding.Unbind(context.Request.ToGenericHttpRequest(), saml2AuthnResponse);

            // Для случая, когда пользователь зашел на роут "/login", перед этим не завершив сессию.
            if (context.HttpContext?.User.Identity?.IsAuthenticated == true)
            {
                context.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
            }

            var principal = await saml2AuthnResponse.CreateSession(
                    context.HttpContext,
                    lifetime: context.ClientOptions?.ExpireTimeSpan,
                    isPersistent: context.ClientOptions?.SetSAMLCookie == true,
                    claimsTransform: WebClientHelper.TransformPrincipalForSaml);

            if (principal.Identity?.IsAuthenticated == true)
            {
                var loginClaim = principal.Claims.FirstOrDefault(c => c.Type == context.ClientOptions?.LoginClaimType);
                if (loginClaim == null || string.IsNullOrEmpty(loginClaim.Value))
                {
                    // не смогли найти login в claims
                    throw new InvalidOperationException("Can't find login claim in SAML authentication response.");
                }

                string login = loginClaim.Value;
                bool isUserExists;

                await using (this.dbScope.Create())
                {
                    DbManager db = this.dbScope.Db;
                    db
                        .SetCommand(
                            this.dbScope.BuilderFactory
                                .Select().C("Login")
                                .From("PersonalRoles").NoLock()
                                .Where().LowerC("Login").Equals().LowerP("Login")
                                .Build(),
                            db.Parameter("Login", login))
                        .LogCommand();

                    await using DbDataReader reader = await db.ExecuteReaderAsync(cancellationToken);
                    isUserExists = await reader.ReadAsync(cancellationToken);
                }

                // пытаемся найти пользователя по email (fallback для старых версий)
                if (!isUserExists && context.ClientOptions?.UpdateEmailLoginUsers == true)
                {
                    var emailClaim = principal.Claims.FirstOrDefault(c => c.Type == context.ClientOptions.EmailClaimType);
                    if (emailClaim == null || string.IsNullOrEmpty(emailClaim.Value))
                    {
                        throw new InvalidOperationException("Can't find email claim in SAML authentication response.");
                    }

                    string email = emailClaim.Value;

                    await using (this.dbScope.Create())
                    {
                        DbManager db = this.dbScope.Db;
                        db
                            .SetCommand(
                                this.dbScope.BuilderFactory
                                    .Select().C("Login")
                                    .From("PersonalRoles").NoLock()
                                    .Where().LowerC("Login").Equals().LowerP("Email")
                                    .Build(),
                                db.Parameter("Email", email))
                            .LogCommand();

                        await using DbDataReader reader = await db.ExecuteReaderAsync(cancellationToken);
                        isUserExists = await reader.ReadAsync(cancellationToken);
                    }

                    // если нашли, то обновляем login пользователя с email на актуальный login
                    if (isUserExists)
                    {
                        logger.Error("ADFS auth: login was changed from \"{0}\" to \"{1}\".", email, login);

                        await using (this.dbScope.Create())
                        {
                            DbManager db = this.dbScope.Db;
                            await db
                                .SetCommand(
                                    this.dbScope.BuilderFactory
                                        .Update("PersonalRoles")
                                        .C("Login").Assign().P("Login")
                                        .Where().LowerC("Login").Equals().LowerP("Email")
                                        .Build(),
                                    db.Parameter("Login", login),
                                    db.Parameter("Email", email))
                                .LogCommand()
                                .ExecuteNonQueryAsync(cancellationToken);
                        }
                    }
                }

                if (!isUserExists)
                {
                    if (context.ClientOptions?.CreateUserAfterAuthenticationIfNotExists != true)
                    {
                        throw new InvalidOperationException(
                            "Can't find user by login claim from SAML authentication response.");
                    }

                    // проверяем, что все нужные данные есть в ответе
                    Claim? emailClaim = principal.Claims.FirstOrDefault(c => c.Type == context.ClientOptions.EmailClaimType);
                    Claim? nameClaim = principal.Claims.FirstOrDefault(c => c.Type == context.ClientOptions.NameClaimType);

                    string email = emailClaim != null ? emailClaim.Value : string.Empty;
                    string name = nameClaim != null ? nameClaim.Value : login;

                    // создаем юзера если его нет
                    await using (SessionContext.Create(Session.CreateSystemToken(this.serverSettings)))
                    {
                        var newResponse = await this.cardRepository.NewAsync(
                            new CardNewRequest
                            {
                                ServiceType = CardServiceType.Client,
                                CardTypeID = RoleHelper.PersonalRoleTypeID,
                                CardTypeName = RoleHelper.PersonalRoleTypeName
                            },
                            cancellationToken);

                        if (!newResponse.ValidationResult.IsSuccessful())
                        {
                            logger.LogResult(newResponse.ValidationResult, "Can't create user during ADFS auth (New request): {0:D}");
                            throw new InvalidOperationException("Can't create user during ADFS auth (New request).");
                        }

                        Card card = newResponse.Card;
                        card.ID = Guid.NewGuid();
                        card.Sections["PersonalRoles"].Fields["FirstName"] = name;
                        card.Sections["PersonalRoles"].Fields["Email"] = email;
                        card.Sections["PersonalRoles"].Fields["Login"] = login;

                        var request = new CardStoreRequest
                        {
                            ServiceType = CardServiceType.Client,
                            Card = card,
                            Info = new Dictionary<string, object?>()
                        };

                        request.SetADFSAuthenticationResponse(saml2AuthnResponse.ToXml().OuterXml);
                        request.SetAddToRolesIDList(context.ClientOptions.AddNewSAMLUserToRoles);

                        CardStoreResponse storeResponse = await this.cardRepository.StoreAsync(request, cancellationToken);
                        if (!storeResponse.ValidationResult.IsSuccessful())
                        {
                            logger.LogResult(storeResponse.ValidationResult, "Can't create user during ADFS auth (Store request): {0:D}");
                            throw new InvalidOperationException("Can't create user during ADFS auth (Store request).");
                        }
                    }
                }

                var httpContext = NotNullOrThrow(context.HttpContext);
                var result = await this.sessionServer.OpenSessionAsync(
                    login,
                    applicationID: WebClientHelper.TryGetApplicationID(httpContext) ?? ApplicationIdentifiers.WebClient,
                    consumeClientLicense: true,
                    twoFactorAuthSupport: false,
                    parameters: WebClientHelper.GetSessionClientParameters(httpContext),
                    expectedLoginTypes: UserLoginTypes.WindowsOrLdap,
                    loginMethod: "$ActionHistory_Sessions_LoginMethod_SAML",
                    info: new()
                    {
                        [RuntimeHelper.SkipWindowsLoginValidationKey] = BooleanBoxes.True,
                        [RuntimeHelper.SkipTwoFactorAuthKey] = BooleanBoxes.True
                    },
                    cancellationToken: cancellationToken);

                // сервер сообщает версию и токен только после того, как сессия была успешно открыта, т.е. это пользователь системы, а не случайный человек
                ThrowIfNull(context.Response);
                context.Response.Headers[SessionHttpRequestHeader.Version] = BuildInfo.VersionObjectString;
                context.Response.Headers[SessionHttpRequestHeader.Session] = result.Token.SerializeToXml(SessionSerializationOptions.Auth);

                AppendTokenCookie(
                    context,
                    WebClientHelper.SerializeToCookie(result.Token),
                    DateTime.UtcNow.AddYears(1),
                    this.webOptions.Value.TokenCookiesName);
            }
            else
            {
                // если пользователь не авторизован, то что-то пошло не так
                throw new InvalidOperationException("User isn't authenticated in AssertionConsumerService call.");
            }

            binding.GetRelayStateQuery().TryGetValue("ReturnUrl", out string? returnUrl);
            if (!string.IsNullOrEmpty(returnUrl))
            {
                returnUrl = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(returnUrl));
            }

            return returnUrl;
        }

        /// <inheritdoc />
        public async ValueTask<Saml2Binding> LogoutAsync(ISamlContext context, CancellationToken cancellationToken = default)
        {
            Saml2Configuration samlConfig = NotNullOrThrow(context.SamlConfig);
            Saml2Binding binding = context.LogoutBinding == SamlLogoutBinding.Post ? new Saml2PostBinding() : new Saml2RedirectBinding();
            var saml2LogoutRequest = await new Saml2LogoutRequest(samlConfig, context.User)
                .DeleteSession(context.HttpContext);
            return binding.Bind(saml2LogoutRequest);
        }

        /// <inheritdoc />
        public ValueTask<string?> LoggedOutAsync(ISamlContext context, bool redirectBinding, CancellationToken cancellationToken = default)
        {
            Saml2Configuration samlConfig = NotNullOrThrow(context.SamlConfig);
            AppendTokenCookie(
                context,
                "closed",
                DateTime.UtcNow.AddDays(-1),
                this.webOptions.Value.TokenCookiesName);

            Saml2Binding binding = redirectBinding ? new Saml2RedirectBinding() : new Saml2PostBinding();
            binding.Unbind(context.Request.ToGenericHttpRequest(), new Saml2LogoutResponse(samlConfig));

            // default redirect
            return new((string?) null);
        }

        /// <inheritdoc />
        public async ValueTask<Saml2Binding> SingleLogoutAsync(ISamlContext context, bool redirectBinding, CancellationToken cancellationToken = default)
        {
            Saml2Configuration samlConfig = NotNullOrThrow(context.SamlConfig);
            Saml2StatusCodes status;

            Saml2Binding requestBinding = context.LogoutBinding == SamlLogoutBinding.Post ? new Saml2PostBinding() : new Saml2RedirectBinding();
            var logoutRequest = new Saml2LogoutRequest(samlConfig, context.User);

            try
            {
                requestBinding.Unbind(context.Request.ToGenericHttpRequest(), logoutRequest);
                status = Saml2StatusCodes.Success;
                await logoutRequest.DeleteSession(context.HttpContext);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                status = Saml2StatusCodes.RequestDenied;
            }

            Saml2Binding responsebinding = redirectBinding ? new Saml2RedirectBinding() : new Saml2PostBinding();
            responsebinding.RelayState = requestBinding.RelayState;

            var saml2LogoutResponse = new Saml2LogoutResponse(samlConfig)
            {
                InResponseToAsString = logoutRequest.IdAsString,
                Status = status,
            };

            return responsebinding.Bind(saml2LogoutResponse);
        }

        #endregion

        #region Private Methods

        private static NameIdPolicy? MapDictionaryToNameIdPolicy(Dictionary<string, object?>? dict)
        {
            if (dict is null)
            {
                return null;
            }
            var nameIdPolicy = new NameIdPolicy();
            if (dict.TryGetValue("AllowCreate", out var allowCreateValue) && allowCreateValue is bool allowCreate)
            {
                nameIdPolicy.AllowCreate = allowCreate;
            }
            if (dict.TryGetValue("Format", out var formatValue) && formatValue is string format)
            {
                nameIdPolicy.Format = format;
            }
            if (dict.TryGetValue("SPNameQualifier", out var sPNameQualifierValue) && sPNameQualifierValue is string sPNameQualifier)
            {
                nameIdPolicy.SPNameQualifier = sPNameQualifier;
            }
            return nameIdPolicy;
        }

        #endregion
    }
}
