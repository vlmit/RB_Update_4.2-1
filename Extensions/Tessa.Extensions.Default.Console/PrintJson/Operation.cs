using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Tessa.Platform;
using Tessa.Platform.Configuration;
using Tessa.Platform.ConsoleApps;
using Tessa.Platform.Json;
using Tessa.Platform.Storage;
using Tessa.Themes;

namespace Tessa.Extensions.Default.Console.PrintJson
{
    public static class Operation
    {
        public static async Task<int> ExecuteAsync(
            IConsoleLogger logger,
            string filePath,
            PrintJsonMode mode,
            bool indented)
        {
            ThrowIfNullOrEmpty(filePath);

            filePath = DefaultConsoleHelper.NormalizeFilePath(filePath);

            if (!File.Exists(filePath))
            {
                await logger.ErrorAsync("File does not exist: {0}", filePath);
                return -1;
            }

            Dictionary<string, object?>?[] fileStorage = [null];
            if (mode == PrintJsonMode.Config)
            {
                await using var companion = new UnityContainerCompanion { UseConfiguration = true };
                int result = await companion.ProcessAndGetAsync(
                    async (c, ct) =>
                    {
                        await c.Container.RegisterDatabaseForConsoleAsync(cancellationToken: ct);
                        c.Container.RegisterConfigurationManagerForFile(filePath);
                    },
                    async (c, ct) =>
                    {
                        if (c.ConfigurationManager?.Errors is { Count: > 0 } errors)
                        {
                            foreach (var error in errors)
                            {
                                await logger.ErrorAsync(error.ToString());
                            }

                            return -2;
                        }

                        fileStorage[0] = c.ConfigurationManager?.Configuration.GetStorage();
                        return 0;
                    });

                if (result != 0)
                {
                    return result;
                }
            }
            else
            {
                var storage = new Dictionary<string, object?>();
                var result = await StorageHelper.TryLoadStorageWithSubFilesAsync(
                    storage,
                    filePath,
                    mode == PrintJsonMode.Theme
                        ? (x, y) => ThemeStorageHelper.MergeThemeStorage(x, y)
                        : null);

                if (!result.IsSuccessful)
                {
                    await logger.LogResultAsync(result);
                    return -2;
                }

                fileStorage[0] = storage;
            }

            await logger.InfoAsync("Printing json file: {0}", Path.GetFullPath(filePath));
            await logger.WriteAsync(TessaSerializer.Instance.SerializeJson(NotNullOrThrow(fileStorage[0]), indented));
            return 0;
        }
    }
}
