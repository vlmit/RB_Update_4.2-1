using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Tessa.Applications.Package;
using Tessa.Cards;
using Tessa.Cards.ComponentModel;
using Tessa.Extensions.Platform.Shared.MobileClient;
using Tessa.Platform;
using Tessa.Platform.ConsoleApps;
using Tessa.Platform.IO;
using Tessa.Platform.Json;
using Tessa.Platform.Runtime;

namespace Tessa.Extensions.Default.Console.PackageMobileClientApp
{
    public static class Operation
    {
        #region Methods

        public static async Task<int> ExecuteAsync(
            IConsoleLogger logger,
            string exePath,
            string outputPath,
            string configFilePath,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(exePath))
            {
                await logger.ErrorAsync("Can't package app: no source executable specified.");
                return -1;
            }

            try
            {
                // если File.Exists упадёт (например, невалидные символы в пути), то его перехватит catch
                exePath = Path.GetFullPath(exePath.NormalizePathOnCurrentPlatform());
                if (!File.Exists(exePath))
                {
                    await logger.ErrorAsync("Can't find source executable \"{0}\". Please, check if file exists and application has access to it.", exePath);
                    return -2;
                }

                // если File.Exists упадёт (например, невалидные символы в пути), то его перехватит catch
                string pathConfig = Path.GetFullPath(configFilePath.NormalizePathOnCurrentPlatform());
                if (!File.Exists(pathConfig))
                {
                    await logger.ErrorAsync("Can't find source executable \"{0}\". Please, check if file exists and config.file has access to it.", pathConfig);
                    return -2;
                }

                MobileClientConfig? config = await LoadConfigAsync(configFilePath);

                var app = new MobileClientApplication { Version = config?.Release };
                bool success = await LoadInfoAsync(logger, app, exePath, cancellationToken: cancellationToken);
                if (!success)
                {
                    return -3;
                }

                DateTime utcNow = DateTime.UtcNow;

                var card = new Card
                {
                    ID = MobileClientHelper.MobileApplicationCardID,
                    TypeID = MobileClientHelper.MobileApplicationTypeID,
                    TypeName = MobileClientHelper.MobileApplicationTypeName,
                    TypeCaption = MobileClientHelper.MobileApplicationTypeCaption,
                    Created = utcNow,
                    CreatedByID = Session.SystemID,
                    CreatedByName = Session.SystemName,
                    Modified = utcNow,
                    ModifiedByID = Session.SystemID,
                    ModifiedByName = Session.SystemName,
                };

                var sections = card.Sections;

                var fields = sections.GetOrAddEntry("MobileApplication").RawFields;
                // запись данных в нужные поля карточки, для сохранения меты
                fields["Release"] = config?.Release;
                fields["PlatformVersion"] = config?.PlatformVersion;
                fields["BundleVersion"] = config?.BundleVersion;
                fields["MinSupportNativeVersion"] = config?.MinSupportNativeVersion;
                fields["BundleStrategy"] = config?.BundleStrategy;
                fields["PublicKey"] = config?.PublicKey;

                if (app.Files.Count > 0)
                {
                    var cardFiles = card.Files;
                    using HashAlgorithm hashAlgorithm = HashSignatureProvider.Files.CreateAlgorithm();

                    foreach (MobileClientApplicationFile file in app.Files)
                    {
                        Guid versionRowID = Guid.NewGuid();

                        CardFile cardFile = cardFiles.Add();
                        cardFile.RowID = file.RowID;
                        cardFile.TypeID = CardHelper.FileTypeID;
                        cardFile.TypeName = CardHelper.FileTypeName;
                        cardFile.TypeCaption = CardHelper.FileTypeCaption;
                        cardFile.VersionRowID = versionRowID;
                        cardFile.Name = file.Name;
                        cardFile.CategoryCaption = file.Category;
                        cardFile.Size = file.Size;

                        cardFile.State = CardFileState.Inserted;

                        Card fileCard = cardFile.Card;
                        fileCard.ID = file.RowID;
                        fileCard.TypeID = CardHelper.FileTypeID;
                        fileCard.TypeName = CardHelper.FileTypeName;
                        fileCard.TypeCaption = CardHelper.FileTypeCaption;
                        fileCard.Created = utcNow;
                        fileCard.CreatedByID = Session.SystemID;
                        fileCard.CreatedByName = Session.SystemName;
                        fileCard.Modified = utcNow;
                        fileCard.ModifiedByID = Session.SystemID;
                        fileCard.ModifiedByName = Session.SystemName;
                    }
                }

                var request = new CardStoreRequest { Card = card, Method = CardStoreMethod.Import };
                request.SetImportVersion(1);

                var container = new List<object?> { request.GetStorage() };

                foreach (MobileClientApplicationFile file in app.Files)
                {
                    container.Add(new Dictionary<string, object?>(StringComparer.Ordinal)
                    {
                        { CardComponentHelper.ContentFileIDKey, file.RowID },
                        { CardComponentHelper.ContentFileSizeKey, file.Size },
                        { CardComponentHelper.ContentFileReferenceKey, file.Name },
                    });
                }

                string outputExtension = ".jcard";
                if (string.IsNullOrWhiteSpace(outputPath) || outputPath == ".")
                {
                    outputPath = app + outputExtension;
                }
                else if (outputPath.EndsWith("/", StringComparison.Ordinal)
                         || outputPath.EndsWith("\\", StringComparison.Ordinal))
                {
                    outputPath = Path.Combine(
                        outputPath[..^1],
                        app + outputExtension);
                }
                else if (!outputPath.EndsWith(outputExtension, StringComparison.OrdinalIgnoreCase)
                         && Directory.Exists(outputPath))
                {
                    outputPath = Path.Combine(
                        outputPath,
                        app + outputExtension);
                }

                await logger.InfoAsync("Writing package to: {0}", outputPath);

                string? outputFolder = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(outputFolder) && !Directory.Exists(outputFolder))
                {
                    Directory.CreateDirectory(outputFolder);
                }

                await using FileStream targetStream = FileHelper.Create(outputPath, synchronousOnly: true);
                await using var writer = new StreamWriter(targetStream, Encoding.UTF8, FileHelper.DefaultFileBufferSize, leaveOpen: true) { NewLine = "\n" };
                await using var jsonWriter = new JsonTextWriter(writer) { Formatting = Formatting.Indented };
                TessaSerializer.JsonTyped.Serialize(jsonWriter, container);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                await logger.LogExceptionAsync("Error packaging application", ex);
                return -1;
            }

            await logger.InfoAsync("Packaging is completed");
            return 0;
        }

        #endregion

        #region Private Methods

        private static async Task<bool> LoadInfoAsync(IConsoleLogger logger, MobileClientApplication app, string path, CancellationToken cancellationToken = default)
        {
            await logger.InfoAsync("Packaging mobile application: {0}", path);

            if (string.IsNullOrWhiteSpace(app.FileName))
            {
                app.FileName = Path.GetFileNameWithoutExtension(path);
            }

            // эти свойства точно не null, но могут содержать оконечные пробелы
            app.FileName = Path.GetFileName(path);

            string? appFolder = string.IsNullOrEmpty(path) ? path : Path.GetDirectoryName(Path.GetFullPath(path));
            if (string.IsNullOrEmpty(appFolder))
            {
                appFolder = Directory.GetCurrentDirectory();
            }

            await logger.InfoAsync("Packaging from folder: {0}", appFolder);
            await logger.InfoAsync("ExeFileName = {0}", app.FileName);
            await logger.InfoAsync("Version = {0}", app.Version);

            var ignoredProvider = new FileSystemIgnoredFilesProvider();
            var ignoredPathList = new HashSet<string>(await ignoredProvider.GetIgnoredFileNamesAsync(appFolder, cancellationToken: cancellationToken));

            foreach (string pathIgnored in ignoredPathList)
            {
                await logger.InfoAsync("Ignored: {0}", pathIgnored);
            }

            foreach (string filePath in Directory.EnumerateFiles(appFolder, "*.*", SearchOption.AllDirectories))
            {
                if (!ignoredPathList.Contains(filePath))
                {
                    var file = new MobileClientApplicationFile(filePath, appFolder);

                    file.ReadFileSize();

                    app.Files.Add(file);

                    if (string.IsNullOrEmpty(file.Category))
                    {
                        await logger.InfoAsync("Added: {0}", file.Name);
                    }
                    else
                    {
                        await logger.InfoAsync("Added: {0}", Path.Combine(file.Category.NormalizePathOnCurrentPlatform(), file.Name));
                    }
                }
            }

            return true;
        }

        private static async Task<MobileClientConfig?> LoadConfigAsync(string path)
        {
            string configJson = await File.ReadAllTextAsync(path);
            // десериализация JSON  
            var config = JsonConvert.DeserializeObject<MobileClientConfig>(configJson);
            return config;
        }

        #endregion
    }
}
