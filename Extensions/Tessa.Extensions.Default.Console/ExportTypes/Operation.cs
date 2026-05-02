using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Platform.ConsoleApps;
using Tessa.Platform.IO;

namespace Tessa.Extensions.Default.Console.ExportTypes
{
    public sealed class Operation(
        IConsoleSessionManager sessionManager,
        IConsoleLogger logger,
        ICardTypeClientRepository cardTypeClientRepository)
        : ConsoleOperation<OperationContext>(logger, sessionManager)
    {
        #region Fields

        private readonly ICardTypeClientRepository cardTypeClientRepository = NotNullOrThrow(cardTypeClientRepository);

        #endregion

        #region Private Methods

        private async Task ExportTypeCoreAsync(
            CardType type,
            string exportPath,
            List<(CardInstanceType InstanceType, string Path)>? subfolderPathList,
            CancellationToken cancellationToken)
        {
            await this.Logger.InfoAsync("Saving type \"{0}\"", type.Name);

            var json = await type.SerializeToJsonAsync(indented: true, cancellationToken);
            var fileName = $"{FileHelper.RemoveInvalidFileNameChars(type.Name)}.jtype";

            string? folderPath;
            if (subfolderPathList is not null)
            {
                folderPath = null;

                var instanceType = type.InstanceType;
                foreach (var (subfolderInstanceType, subfolderPath) in subfolderPathList)
                {
                    if (subfolderInstanceType == instanceType)
                    {
                        folderPath = subfolderPath;
                        break;
                    }
                }

                if (folderPath is null)
                {
                    var subfolder = instanceType switch
                    {
                        CardInstanceType.Card => "Cards",
                        CardInstanceType.Dialog => "Dialogs",
                        CardInstanceType.File => "Files",
                        CardInstanceType.Task => "Tasks",
                        _ => throw ArgumentOutOfRange(instanceType)
                    };

                    folderPath = DefaultConsoleHelper.NormalizeFolderAndCreateIfNotExists(Path.Combine(exportPath, subfolder));
                    subfolderPathList.Add((instanceType, folderPath));
                }
            }
            else
            {
                folderPath = exportPath;
            }

            var path = Path.Combine(folderPath, fileName);
            await File.WriteAllTextAsync(path, json, Encoding.UTF8, cancellationToken);
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc />
        public override async Task<int> ExecuteAsync(OperationContext context, CancellationToken cancellationToken = default)
        {
            if (!this.SessionManager.IsOpened)
            {
                return -1;
            }

            var exportedCount = 0;
            var notFoundCount = 0;

            var subfolderPathList = context.CreateTypesSubfolders ? new List<(CardInstanceType InstanceType, string Path)>() : null;

            try
            {
                var exportPath = DefaultConsoleHelper.NormalizeFolderAndCreateIfNotExists(context.OutputFolder);
                if (string.IsNullOrEmpty(exportPath))
                {
                    exportPath = Directory.GetCurrentDirectory();
                }

                if (context.ClearOutputFolder)
                {
                    await this.Logger.InfoAsync("Removing existent types from output folder \"{0}\"{1}", exportPath,
                        context.CreateTypesSubfolders ? " with subfolders" : null);

                    foreach (var filePath in Directory.EnumerateFiles(exportPath, "*.jtype",
                                 context.CreateTypesSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
                    {
                        File.Delete(filePath);
                    }
                }

                await this.Logger.InfoAsync("Loading types from service...");
                var types = await this.cardTypeClientRepository
                    .GetAllCardTypesAsync(cancellationToken: cancellationToken);

                if (context.TypeNamesOrIdentifiers is null || context.TypeNamesOrIdentifiers.Count == 0)
                {
                    await this.Logger.InfoAsync("Exporting all types to folder \"{0}\"", exportPath);

                    if (context.CardInstanceType.HasValue)
                    {
                        await this.Logger.InfoAsync("Filtering by instance type \"{0}\"", context.CardInstanceType.Value);
                    }

                    foreach (var type in types)
                    {
                        if (!context.CardInstanceType.HasValue || type.InstanceType == context.CardInstanceType.Value)
                        {
                            await this.ExportTypeCoreAsync(type, exportPath, subfolderPathList, cancellationToken);
                            exportedCount++;
                        }
                    }
                }
                else
                {
                    await this.Logger.InfoAsync(
                        "Exporting types to folder \"{0}\": {1}",
                        exportPath,
                        string.Join(", ", context.TypeNamesOrIdentifiers.Select(name => $"\"{name}\"")));

                    if (context.CardInstanceType.HasValue)
                    {
                        await this.Logger.InfoAsync("Filtering by instance type \"{0}\"", context.CardInstanceType.Value);
                    }

                    // в отличие от types, здесь мы не учитываем регистр
                    var typesByID = new Dictionary<Guid, CardType>(types.Count);
                    var typesByName = new Dictionary<string, CardType>(types.Count, StringComparer.OrdinalIgnoreCase);
                    foreach (var type in types)
                    {
                        typesByID[type.ID] = type;
                        typesByName[type.Name] = type;
                    }

                    foreach (var nameOrIdentifier in context.TypeNamesOrIdentifiers)
                    {
                        // даже если тип найден по алиасу, но по InstanceType он не соответствует - пишем, что он не найден
                        if ((typesByName.TryGetValue(nameOrIdentifier, out var type)
                                || Guid.TryParse(nameOrIdentifier, out var typeID)
                                && typesByID.TryGetValue(typeID, out type))
                            && (!context.CardInstanceType.HasValue || type.InstanceType == context.CardInstanceType.Value))
                        {
                            await this.ExportTypeCoreAsync(type, exportPath, subfolderPathList, cancellationToken);
                            exportedCount++;
                        }
                        else
                        {
                            await this.Logger.ErrorAsync("Type \"{0}\" isn't found", nameOrIdentifier);
                            notFoundCount++;
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await this.Logger.LogExceptionAsync("Error exporting types", ex);
                return -1;
            }

            // количество экспортированных выводим как при наличии, так и при отсутствии ошибок
            if (exportedCount > 0)
            {
                await this.Logger.InfoAsync("Types ({0}) are exported successfully", exportedCount);
            }

            if (notFoundCount != 0)
            {
                await this.Logger.ErrorAsync("Types ({0}) aren't found by provided names or identifiers", notFoundCount);
            }
            else if (exportedCount == 0)
            {
                await this.Logger.InfoAsync("No types to export");
            }

            return 0;
        }

        #endregion
    }
}
