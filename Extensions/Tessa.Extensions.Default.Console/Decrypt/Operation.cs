using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Platform;
using Tessa.Platform.ConsoleApps;
using Tessa.Platform.Encryption;
using Tessa.Platform.IO;

namespace Tessa.Extensions.Default.Console.Decrypt
{
    public static class Operation
    {
        #region Base Overrides

        public static async Task<int> ExecuteAsync(
            IConsoleLogger logger,
            string filePath,
            string certificatePath,
            string? certificatePassword,
            string? certificateKeyPemFilePath,
            string? decryptedFilePath,
            CancellationToken cancellationToken = default)
        {
            filePath = DefaultConsoleHelper.NormalizeFilePath(filePath);
            if (!File.Exists(filePath))
            {
                await logger.ErrorAsync("The file to decrypt does not exist: {0}", filePath);
                return -1;
            }

            certificatePath = DefaultConsoleHelper.NormalizeFilePath(certificatePath);
            if (!File.Exists(certificatePath))
            {
                await logger.ErrorAsync("The certificate file could not be found: {0}", certificatePath);
                return -4;
            }

            certificateKeyPemFilePath = DefaultConsoleHelper.NormalizeFilePath(certificateKeyPemFilePath);
            if (!string.IsNullOrEmpty(certificateKeyPemFilePath) && !File.Exists(certificateKeyPemFilePath))
            {
                await logger.ErrorAsync("The certificate key file for PEM format could not be found: {0}", certificateKeyPemFilePath);
                return -5;
            }

            decryptedFilePath = DefaultConsoleHelper.NormalizeFilePath(decryptedFilePath);

            ITempFile? tempInputFile = null;
            if (string.IsNullOrEmpty(decryptedFilePath))
            {
                tempInputFile = TempFile.Acquire(Path.GetFileName(filePath));
                File.Move(filePath, tempInputFile.Path);
            }

            await using var companion = new UnityContainerCompanion { UseConfiguration = true };
            return await companion.ProcessAndGetAsync(
                (c, ct) => c.Container.RegisterDatabaseForConsoleAsync(cancellationToken: ct),
                async (c, ct) =>
                {
                    if (c.Container.TryResolve<IEncryptionService>() is not { } encryptionService
                        || c.Container.TryResolve<IEncryptionCertificateLoader>() is not { } encryptionCertificateLoader)
                    {
                        await logger.ErrorAsync("File encryption API is not registered");
                        return -2;
                    }

                    try
                    {
                        using var certificate = await encryptionCertificateLoader.LoadCertificateAsync(
                            certificatePath, certificatePassword, certificateKeyPemFilePath, ct);

                        await using var inputStream = FileHelper.OpenRead(tempInputFile is not null ? tempInputFile.Path : filePath);
                        await using var outputStream = FileHelper.Create(string.IsNullOrEmpty(decryptedFilePath) ? filePath : decryptedFilePath);

                        await encryptionService.DecryptDataAsync(certificate, inputStream, outputStream, ct);
                    }
                    catch (Exception e) when (e is not OperationCanceledException)
                    {
                        await logger.LogExceptionAsync("Error decrypting file", e);
                        if (tempInputFile is not null)
                        {
                            FileHelper.DeleteFileSafe(filePath);
                            File.Move(tempInputFile.Path, filePath);
                        }

                        return -3;
                    }
                    finally
                    {
                        tempInputFile?.Dispose();
                    }

                    await logger.InfoAsync("File is decrypted successfully.");
                    return 0;
                }, cancellationToken);
        }

        #endregion
    }
}
