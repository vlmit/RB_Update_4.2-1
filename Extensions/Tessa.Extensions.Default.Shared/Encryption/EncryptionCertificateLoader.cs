#nullable enable

using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Platform.EDS;
using Tessa.Platform.Encryption;

namespace Tessa.Extensions.Default.Shared.Encryption
{
    /// <inheritdoc cref="IEncryptionCertificateLoader"/>
    public sealed class EncryptionCertificateLoader : IEncryptionCertificateLoader
    {
        #region IEncryptionCertificateLoader Members

        /// <inheritdoc/>
        public ValueTask<X509Certificate2> LoadCertificateAsync(
            string certificatePath,
            string? certificatePassword,
            string? certificateKeyPemFilePath = null,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNullOrEmpty(certificatePath);

            cancellationToken.ThrowIfCancellationRequested();

            if (!File.Exists(certificatePath))
            {
                throw new FileNotFoundException($"The certificate file could not be found: {certificatePath}", certificatePath);
            }

            if (!string.IsNullOrEmpty(certificateKeyPemFilePath) && !File.Exists(certificateKeyPemFilePath))
            {
                throw new FileNotFoundException($"The certificate key file for PEM format could not be found: {certificateKeyPemFilePath}", certificateKeyPemFilePath);
            }

            return new(SignatureHelper.LoadCertificateFromFileWithPemSupport(certificatePath, certificatePassword, certificateKeyPemFilePath));
        }

        #endregion
    }
}
