#nullable enable

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace Tessa.Extensions.Default.Server.OnlyOffice.Token
{
    /// <inheritdoc cref="IOnlyOfficeR7TokenManager"/>
    public sealed class OnlyOfficeR7TokenManager(IOnlyOfficeSettingsProvider settingsProvider) :
        IOnlyOfficeR7TokenManager
    {
        #region Fields

        private readonly IOnlyOfficeSettingsProvider settingsProvider = NotNullOrThrow(settingsProvider);

        #endregion

        #region IOnlyOfficeTokenManager Members

        /// <inheritdoc/>
        public async Task<string?> CreateTokenAsync(
            string config,
            CancellationToken cancellationToken)
        {
            if ((await GetSecurityKey(cancellationToken)) is not { } securityKey)
            {
                return null;
            }

            var claims = JsonConvert.DeserializeObject<IDictionary<string, object>>(config, [new JsonStringToDictionaryConverter()]);

            var descriptor = new SecurityTokenDescriptor
            {
                Claims = claims,
                SigningCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256)
            };

            var handler = new JsonWebTokenHandler { SetDefaultTimesOnTokenCreation = false };
            return handler.CreateToken(descriptor);
        }

        /// <inheritdoc/>
        public async Task<OnlyOfficeJwtTokenInfo?> VerifyTokenAsync(
            string token,
            CancellationToken cancellationToken)
        {
            ThrowIfNullOrWhiteSpace(token);

            if ((await GetSecurityKey(cancellationToken)) is not { } securityKey)
            {
                return null;
            }

            // Проверяем код сервера и время жизни токена.
            var validationParameters = new TokenValidationParameters
            {
                ValidateLifetime = true,
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = securityKey,
                ValidAlgorithms = [OnlyOfficeTokenHelper.FileTokenSignatureAlgorithm],
                ClockSkew = TimeSpan.Zero
            };

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var claimsPrincipal = handler.ValidateToken(token, validationParameters, out var validatedToken);

                var tokenInfo = new OnlyOfficeJwtTokenInfo(claimsPrincipal.Claims);

                return tokenInfo;
            }
            catch (SecurityTokenException)
            {
                return null;
            }
        }

        #endregion

        #region  Properties 

        private async Task<SymmetricSecurityKey?> GetSecurityKey(CancellationToken cancellationToken = default) =>
            (await this.settingsProvider.GetSettingsAsync(cancellationToken)).OnlyOfficeSignatureKey is { } onlyOfficeSignatureKey
            ? new SymmetricSecurityKey(onlyOfficeSignatureKey)
            : null;

        #endregion
    }
}
