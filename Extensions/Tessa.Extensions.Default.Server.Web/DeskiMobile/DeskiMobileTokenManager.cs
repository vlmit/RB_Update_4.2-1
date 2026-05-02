using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Principal;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using Tessa.Extensions.Default.Server.Web.DeskiMobile.Models;
using Tessa.Platform;
using Tessa.Platform.Runtime;
using Tessa.Web;

namespace Tessa.Extensions.Default.Server.Web.DeskiMobile
{
    /// <inheritdoc cref="IDeskiMobileTokenManager"/>
    /// <param name="tessaServerSettings"><inheritdoc cref="ITessaServerSettings" path="/summary"/></param>
    /// <param name="tessaServerKeyProvider"><inheritdoc cref="ITessaServerKeyProvider" path="/summary"/></param>
    public sealed class DeskiMobileTokenManager(
        ITessaServerSettings tessaServerSettings,
        ITessaServerKeyProvider tessaServerKeyProvider)
        : IDeskiMobileTokenManager
    {
        #region Fields

        private readonly ITessaServerSettings tessaServerSettings = NotNullOrThrow(tessaServerSettings);

        private readonly ITessaServerKeyProvider tessaServerKeyProvider = NotNullOrThrow(tessaServerKeyProvider);

        #endregion

        #region Public methods

        /// <inheritdoc />
        public string CreateToken(
            TimeSpan jwtLifeTime, IUser user,
            Guid operationID, DeskiMobileTokenPermissionFlags flags)
        {
            // Формирование кастомных клеймов.
            var claims = new List<Claim>(4)
            {
                new(DeskiMobileClaimNames.UserID, user.ID.ToString("N"), ClaimValueTypes.String),
                new(DeskiMobileClaimNames.UserName, user.Name, ClaimValueTypes.String),
                new(DeskiMobileClaimNames.OperationID, operationID.ToString("N"), ClaimValueTypes.String),
                new(DeskiMobileClaimNames.Access, flags.ToString("D"), ClaimValueTypes.Integer32),
            };
            var identity = new ClaimsIdentity(new GenericIdentity("deski_mobile"), claims);

            // Подписание ключом SignatureKey.
            var securityKey = new SymmetricSecurityKey(this.tessaServerKeyProvider.GetSignatureKey());
            var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // Формирование токена.
            var handler = new JwtSecurityTokenHandler();
            var token = handler.CreateJwtSecurityToken(
                issuer: this.tessaServerSettings.ServerCode,
                audience: "deski",
                subject: identity,
                expires: DateTime.UtcNow.Add(jwtLifeTime),
                signingCredentials: signingCredentials);

            // создаем JWT-токен
            return handler.WriteToken(token);
        }

        /// <inheritdoc />
        public TokenInfo GetToken(HttpRequest request)
        {
            if (request.Headers is null)
            {
                throw new InvalidOperationException("Request header is null.");
            }

            var jwtHeaderToken = request.Headers.TryGetBearerAuthToken();

            if (string.IsNullOrWhiteSpace(jwtHeaderToken))
            {
                throw new InvalidOperationException("JWT is null or empty.");
            }

            var token = this.VerifyJwtToken(jwtHeaderToken);

            return token;
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Метод валидирует JWT токен.
        /// </summary>
        private TokenInfo VerifyJwtToken(string token)
        {
            ThrowIfNullOrWhiteSpace(token);

            var securityKey = new SymmetricSecurityKey(this.tessaServerKeyProvider.GetSignatureKey());

            // Параметры для проверки токена
            var validationParameters = new TokenValidationParameters
            {
                ValidateLifetime = true,
                ValidateAudience = false,
                ValidateIssuer = true,
                ValidIssuer = this.tessaServerSettings.ServerCode,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = securityKey,
                ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
                ClockSkew = TimeSpan.Zero
            };

            // Проверяем токен
            var handler = new JwtSecurityTokenHandler();
            var claimsPrincipal = handler.ValidateToken(token, validationParameters, out _);
            return new TokenInfo(claimsPrincipal.Claims);
        }

        #endregion
    }
}
