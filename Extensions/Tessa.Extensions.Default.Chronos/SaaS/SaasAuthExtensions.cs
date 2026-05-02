using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using Tessa.Discovery;

namespace Tessa.Extensions.Default.Chronos.SaaS
{
    public static class SaasAuthExtensions
    {
        /// <summary>
        /// Generates JWT for authentication in different access token providers.
        /// </summary>
        /// <param name="key"><inheritdoc cref="DiscoveryKey" path="/summary"/></param>
        /// <param name="scope">Desired scope.</param>
        /// <param name="expiration">Token life time.</param>
        /// <returns>JWT</returns>
        public static string GenerateAuthToken(this DiscoveryKey key, string scope, TimeSpan expiration)
        {
            var claims = new List<Claim>(1)
            {
                new ("Scope", scope, ClaimValueTypes.String),
            };

            var identity = new ClaimsIdentity(claims);

            // key
            var signingCredentials = key.GetSigningCredentials();

            // token
            var handler = new JwtSecurityTokenHandler();
            var token = handler.CreateJwtSecurityToken(
                issuer: "chronos",
                subject: identity,
                expires: DateTime.UtcNow.Add(expiration),
                signingCredentials: signingCredentials);

            return handler.WriteToken(token);
        }

        /// <summary>
        /// Get <see cref="SigningCredentials"/> for JWT generation based on <see cref="DiscoveryKey"/>.
        /// </summary>
        /// <param name="key"><inheritdoc cref="DiscoveryKey" path="/summary"/></param>
        /// <returns><see cref="SigningCredentials"/> for JWT generation</returns>
        public static SigningCredentials GetSigningCredentials(this DiscoveryKey key)
        {
            var dsa = ECDsa.Create();
            dsa.ImportSubjectPublicKeyInfo(key.PublicKey, out _);
            dsa.ImportPkcs8PrivateKey(key.PrivateKey, out _);
            
            var securityKey = new ECDsaSecurityKey(dsa){KeyId = Convert.ToBase64String(NotNullOrThrow(key.KeyID))};
            return new SigningCredentials(securityKey, "ES512");
        }
    }
}
