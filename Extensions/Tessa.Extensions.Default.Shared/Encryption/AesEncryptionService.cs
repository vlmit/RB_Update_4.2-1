#nullable enable

using System;
using System.Buffers;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Platform.Encryption;
using Tessa.Platform.IO;

namespace Tessa.Extensions.Default.Shared.Encryption
{
    /// <summary>
    /// Реализация сервиса шифрования и расшифрования данных, которая использует алгоритм AES256.
    /// </summary>
    public sealed class AesEncryptionService : IEncryptionService
    {
        #region Constants

        /// <summary>
        /// Длина ключа для использования алгоритма AES256.
        /// </summary>
        public const int AesKeySize = 256;

        /// <summary>
        /// Максимальный размер читаемого из потока объекта.
        /// </summary>
        private const int MaxVectorLength = 10_000;

        #endregion

        #region IEncryptionService Members

        /// <inheritdoc/>
        /// <exception cref="InvalidOperationException">Возникает в случае отсутствия у сертификата открытого ключа.</exception>
        public async ValueTask EncryptDataAsync(
            X509Certificate2 certificate,
            Stream inputStream,
            Stream outputStream,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(certificate);
            ThrowIfNull(inputStream);
            ThrowIfNull(outputStream);

            var rsaKey = certificate.GetRSAPublicKey();
            if (rsaKey is null)
            {
                throw new InvalidOperationException("The certificate does not contain a public key.");
            }

            RSAPKCS1KeyExchangeFormatter keyFormatter = new(rsaKey);

            Aes? aes = null;
            try
            {
                aes = Aes.Create();
                aes.KeySize = AesKeySize;
                aes.Mode = CipherMode.CBC;
                var blockSizeBytes = aes.BlockSize / 8;

                byte[] keyEncrypted = keyFormatter.CreateKeyExchange(aes.Key, aes.GetType());

                int lKey = keyEncrypted.Length;
                int lIV = aes.IV.Length;
                var aesIv = aes.IV;

                await outputStream.WriteInt32Async(keyEncrypted.Length, cancellationToken).ConfigureAwait(false);
                await outputStream.WriteInt32Async(lIV, cancellationToken).ConfigureAwait(false);
                await outputStream.WriteAsync(keyEncrypted.AsMemory(0, lKey), cancellationToken).ConfigureAwait(false);
                await outputStream.WriteAsync(aesIv.AsMemory(0, lIV), cancellationToken).ConfigureAwait(false);

                using var transform = aes.CreateEncryptor();

                aes.Dispose();
                aes = null;

                var outStreamEncrypted = new CryptoStream(outputStream, transform, CryptoStreamMode.Write);
                await using var _ = outStreamEncrypted.ConfigureAwait(false);
                int count;

                byte[] data = ArrayPool<byte>.Shared.Rent(blockSizeBytes);

                do
                {
                    count = await inputStream.ReadAsync(data.AsMemory(0, blockSizeBytes), cancellationToken).ConfigureAwait(false);
                    await outStreamEncrypted.WriteAsync(data.AsMemory(0, count), cancellationToken).ConfigureAwait(false);
                } while (count > 0);

                ArrayPool<byte>.Shared.Return(data);
                await outStreamEncrypted.FlushFinalBlockAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                aes?.Dispose();
            }
        }

        /// <inheritdoc/>
        /// <exception cref="InvalidOperationException">Возникает в случае отсутствия у сертификата закрытого ключа
        /// или невозможности получения ключа шифрования или вектора инициализации из потока данных.</exception>
        public async ValueTask DecryptDataAsync(
            X509Certificate2 certificate,
            Stream inputStream,
            Stream outputStream,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(certificate);
            ThrowIfNull(inputStream);
            ThrowIfNull(outputStream);

            var lenK = await inputStream.ReadInt32Async(cancellationToken).ConfigureAwait(false);
            ThrowIf(lenK, lenK is <= 0 or > MaxVectorLength);

            var lenIV = await inputStream.ReadInt32Async(cancellationToken).ConfigureAwait(false);
            ThrowIf(lenIV, lenIV is <= 0 or > MaxVectorLength);

            var keyEncrypted = ArrayPool<byte>.Shared.Rent(lenK);
            var iv = new byte[lenIV]; // aes.CreateDecryptor() below requires byte array

            int read = await inputStream.ReadExactAsync(keyEncrypted.AsMemory(0, lenK), cancellationToken).ConfigureAwait(false);
            if (read != lenK)
            {
                throw new InvalidOperationException("Failed to read the encryption key.");
            }

            read = await inputStream.ReadExactAsync(iv, cancellationToken).ConfigureAwait(false);
            if (read != lenIV)
            {
                throw new InvalidOperationException("Failed to read the initialization vector.");
            }

            var rsaKey = certificate.GetRSAPrivateKey()
                ?? throw new InvalidOperationException("The certificate does not contain a private key.");

            var keyDecrypted = rsaKey.Decrypt(keyEncrypted.AsSpan(0, lenK), RSAEncryptionPadding.Pkcs1);

            ArrayPool<byte>.Shared.Return(keyEncrypted);
            Aes? aes = null;
            try
            {
                aes = Aes.Create();
                aes.KeySize = AesKeySize;
                aes.Mode = CipherMode.CBC;
                int blockSizeBytes = aes.BlockSize / 8;

                using var transform = aes.CreateDecryptor(keyDecrypted, iv);
                aes.Dispose();
                aes = null;

                var outStreamDecrypted = new CryptoStream(outputStream, transform, CryptoStreamMode.Write);
                await using var _ = outStreamDecrypted.ConfigureAwait(false);
                byte[] data = ArrayPool<byte>.Shared.Rent(blockSizeBytes);
                int count;
                do
                {
                    count = await inputStream.ReadAsync(data.AsMemory(0, blockSizeBytes), cancellationToken).ConfigureAwait(false);
                    await outStreamDecrypted.WriteAsync(data.AsMemory(0, count), cancellationToken).ConfigureAwait(false);
                } while (count > 0);

                ArrayPool<byte>.Shared.Return(data);
                await outStreamDecrypted.FlushFinalBlockAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                aes?.Dispose();
            }
        }

        #endregion
    }
}
