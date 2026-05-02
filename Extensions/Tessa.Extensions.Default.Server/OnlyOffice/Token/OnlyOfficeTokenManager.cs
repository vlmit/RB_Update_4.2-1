#nullable enable

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;

namespace Tessa.Extensions.Default.Server.OnlyOffice.Token
{
    /// <inheritdoc cref="IOnlyOfficeTokenManager"/>
    public sealed class OnlyOfficeTokenManager(IOnlyOfficeSettingsProvider settingsProvider) :
        IOnlyOfficeTokenManager
    {
        #region Fields

        private readonly IOnlyOfficeSettingsProvider settingsProvider = NotNullOrThrow(settingsProvider);

        #endregion

        #region IOnlyOfficeTokenManager Members

        /// <inheritdoc/>
        public async Task<string> CreateTokenAsync(
            Guid ID,
            Guid versionID,
            int expiresPeriod,
            OnlyOfficeTokenPermissionFlags flags,
            CancellationToken cancellationToken)
        {
            var settings = await this.settingsProvider.GetSettingsAsync(cancellationToken);

            // Формирование кастомных клеймов.
            var claims = new List<Claim>(6)
            {
                new Claim(OnlyOfficeClaimNames.ID, ID.ToString(OnlyOfficeTokenHelper.GuidFormat), ClaimValueTypes.String),
                new Claim(OnlyOfficeClaimNames.VersionID, versionID.ToString(OnlyOfficeTokenHelper.GuidFormat), ClaimValueTypes.String),
                new Claim(OnlyOfficeClaimNames.Access, flags.ToString(OnlyOfficeTokenHelper.EnumFormat), ClaimValueTypes.Integer32),
            };

            var identity = new ClaimsIdentity(
                new GenericIdentity(OnlyOfficeTokenHelper.UserName),
                claims);

            // Подписание ключом SignatureKey.
            var securityKey = new SymmetricSecurityKey(settings.SignatureKey);
            var signingCredentials = new SigningCredentials(securityKey, OnlyOfficeTokenHelper.FileTokenSignatureAlgorithm);

            // Формирование токена.
            var handler = new JwtSecurityTokenHandler();
            var token = handler.CreateJwtSecurityToken(
                issuer: settings.ServerCode,
                audience: OnlyOfficeTokenHelper.UserName,
                subject: identity,
                expires: DateTime.UtcNow.AddMinutes(expiresPeriod),
                signingCredentials: signingCredentials);

            return handler.WriteToken(token);
        }

        /// <inheritdoc/>
        public async Task<OnlyOfficeJwtTokenInfo?> VerifyTokenAsync(
            string token,
            CancellationToken cancellationToken)
        {
            ThrowIfNullOrWhiteSpace(token);

            var settings = await this.settingsProvider.GetSettingsAsync(cancellationToken);

            var securityKey = new SymmetricSecurityKey(settings.SignatureKey);

            // Проверяем код сервера и время жизни токена.
            var validationParameters = new TokenValidationParameters
            {
                ValidateLifetime = true,
                ValidateAudience = false,
                ValidateIssuer = true,
                ValidIssuer = settings.ServerCode,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = securityKey,
                ValidAlgorithms = new[] { OnlyOfficeTokenHelper.FileTokenSignatureAlgorithm },
                ClockSkew = TimeSpan.Zero
            };

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var claimsPrincipal = handler.ValidateToken(token, validationParameters, out var validatedToken);

                var tokenInfo = new OnlyOfficeJwtTokenInfo(claimsPrincipal.Claims);

                return tokenInfo.IsValid() ? tokenInfo : null;
            }
            catch (SecurityTokenException)
            {
                return null;
            }
        }

        #endregion
    }
}
