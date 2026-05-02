using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Tessa.Discovery;
using Tessa.Localization;
using Tessa.Platform.CommandLine;
using Tessa.Platform.ConsoleApps;
using Tessa.Platform.Json;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Console.ViewDiscoveryKey
{
    public static class Command
    {
        [Verb("ViewDiscoveryKey")]
        [LocalizableDescription("Common_CLI_ViewDiscoveryKey")]
        public static async Task ViewKey(
            [Output] TextWriter stdOut,
            [Error] TextWriter stdErr,
            [Argument] string key,
            [Argument("p")] string password,
            [Argument("q")] [LocalizableDescription("Common_CLI_Quiet")] bool quiet = false,
            [Argument("nologo")] [LocalizableDescription("CLI_NoLogo")] bool nologo = false)
        {
            if (!nologo && !quiet)
            {
                await ConsoleAppHelper.WriteLogoAsync(stdOut);
            }

            ThrowIfNullOrEmpty(key);
            ThrowIfNullOrEmpty(password);

            var serializedJwtToken = await File.ReadAllTextAsync(key);
            var tokenParts = serializedJwtToken.Split('.', StringSplitOptions.TrimEntries);
            if (tokenParts.Length != 3)
            {
                throw new NotSupportedException();
            }

            // TODO: validate jwt token
            var jwtTokenHeaderBytes = WebEncoders.Base64UrlDecode(tokenParts[0]);
            var jwtTokenHeader = StorageHelper.DeserializeFromJson<JwtTokenHeader>(Encoding.UTF8.GetString(jwtTokenHeaderBytes), TessaSerializer.Json);
            // TODO: check other fields
            if (jwtTokenHeader?.Algorithm != "ES512")
            {
                throw new NotSupportedException();
            }

            var jwtTokenPayloadBytes = WebEncoders.Base64UrlDecode(tokenParts[1]);
            var jwtTokenPayload = StorageHelper.DeserializeFromJson<JwtTokenPayload>(Encoding.UTF8.GetString(jwtTokenPayloadBytes), TessaSerializer.Json);
            if (string.IsNullOrEmpty(jwtTokenPayload?.Data))
            {
                throw new NotSupportedException();
            }

            var jweToken = StorageHelper.DeserializeFromJson<JweToken>(jwtTokenPayload.Data, TessaSerializer.Json)
                ?? throw new NotSupportedException();

            var jweHeader = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(NotNullOrThrow(jweToken.ProtectedHeader)));
            var jwtHeader = StorageHelper.DeserializeFromJson<JweTokenProtectedHeader>(jweHeader, TessaSerializer.Json);
            if (!string.Equals(jwtHeader?.Algorithm, "dir", StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(jwtHeader?.Encryption, "A256GCM", StringComparison.OrdinalIgnoreCase))
            {
                throw new NotSupportedException();
            }

            var iv = WebEncoders.Base64UrlDecode(NotNullOrThrow(jweToken.IV));
            var cipherText = WebEncoders.Base64UrlDecode(NotNullOrThrow(jweToken.CipherText));
            var tag = WebEncoders.Base64UrlDecode(NotNullOrThrow(jweToken.Tag));
            var ad = WebEncoders.Base64UrlDecode(NotNullOrThrow(jweToken.AAD));

            var plainText = new byte[cipherText.Length];

            byte[] derivedKey = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                Array.Empty<byte>(),
                10000,
                HashAlgorithmName.SHA512,
                32);

            using (var gcm = new AesGcm(derivedKey, AesGcm.TagByteSizes.MaxSize))
            {
                gcm.Decrypt(iv, cipherText, tag, plainText, ad);
            }

            // TODO: обрезать публичный и приватный ключи (первые 32 символа?)
            await stdOut.WriteAsync(Encoding.UTF8.GetString(plainText));
        }
    }
}
