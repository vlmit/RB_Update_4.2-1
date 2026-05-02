using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Platform.ConsoleApps;
using Tessa.Views;
using Unity;

namespace Tessa.Extensions.Default.Console.Scripts
{
    [ConsoleScript]
    public class FixViewFolders :
        ServerConsoleScriptBase
    {
        #region Constants

        private const string ViewFilePattern = ViewFilePersistent.FileSearchPattern;

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        protected override async ValueTask ExecuteCoreAsync(CancellationToken cancellationToken)
        {
            var path = this.TryGetParameter("path");
            if (string.IsNullOrEmpty(path))
            {
                await this.Logger.ErrorAsync(
                    "Pass \"path\" parameter to start operation");
                this.Result = -1;
                return;
            }

            if (!Directory.Exists(path))
            {
                await this.Logger.ErrorAsync(
                    $"Directory \"{path}\" doesn't exist");
                this.Result = -2;
                return;
            }

            await this.FixViewFoldersAsync(path, cancellationToken);
        }

        /// <inheritdoc/>
        protected override async ValueTask ShowHelpCoreAsync(CancellationToken cancellationToken)
        {
            await this.Logger.WriteLineAsync($"Change folder structure for {ViewFilePattern} files to use views groups as subfolders.");
            await this.Logger.WriteLineAsync();
            await this.Logger.WriteLineAsync("-pp:path=C:\\Repository\\Configuration - path to the configuration folder.");
            await this.Logger.WriteLineAsync();
            await this.Logger.WriteLineAsync("Example:");
            await this.Logger.WriteLineAsync(
                $"{Assembly.GetEntryAssembly()?.GetName().Name} Script {nameof(FixViewFolders)}" +
                " -pp:path=C:\\Repository\\Configuration");
        }

        #endregion

        #region Private Methods

        private async Task FixViewFoldersAsync(
            string path,
            CancellationToken cancellationToken = default)
        {
            var files = Directory.GetFiles(path, ViewFilePattern, SearchOption.AllDirectories);
            if (files.Length == 0)
            {
                await this.Logger.InfoAsync("No views to process.");
                return;
            }

            var viewFilePersistent = this.Container.Resolve<ViewFilePersistent>();
            var pathComparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            foreach (var sourceFilePath in files.OrderBy(f => f))
            {
                if (await this.TryGetViewAsync(viewFilePersistent, sourceFilePath, cancellationToken) is not { } view
                    || view.GroupName?.Trim() is not { Length: > 0 } groupName)
                {
                    continue;
                }

                var part = sourceFilePath[path.Length..];
                var currentDirectory = Path.GetDirectoryName(part)?[1..];
                var currentDirectoryName = Path.GetFileName(currentDirectory);
                if (string.Equals(currentDirectoryName, groupName, pathComparison))
                {
                    continue;
                }

                string destDirectory;
                var viewFileName = Path.GetFileName(part);

                var restOfThePath = Path.GetDirectoryName(currentDirectory);
                if (!string.IsNullOrEmpty(restOfThePath))
                {
                    destDirectory = Path.Combine(path, restOfThePath, groupName);
                    if (Directory.Exists(destDirectory))
                    {
                        File.Move(sourceFilePath, Path.Combine(destDirectory, viewFileName));
                        await this.Logger.InfoAsync($"View file \"{viewFileName}\" is moved to \"{destDirectory}\"");
                        continue;
                    }
                }

                destDirectory = !string.IsNullOrEmpty(currentDirectory)
                    ? Path.Combine(path, currentDirectory, groupName)
                    : Path.Combine(path, groupName);
                if (!Directory.Exists(destDirectory))
                {
                    Directory.CreateDirectory(destDirectory);
                }

                var destFilePath = Path.Combine(destDirectory, viewFileName);
                if (!File.Exists(destFilePath))
                {
                    File.Move(sourceFilePath, destFilePath);
                    await this.Logger.InfoAsync($"View file \"{viewFileName}\" is moved to \"{destDirectory}\"");
                }
            }

            await this.Logger.InfoAsync("Operation is completed");
        }

        private async ValueTask<TessaViewModel?> TryGetViewAsync(
            ViewFilePersistent viewFilePersistent,
            string viewsFolderPath,
            CancellationToken cancellationToken = default)
            => (await viewFilePersistent.ReadAsync(
                    viewsFolderPath,
                    async (fileName, ex, ct) =>
                    {
                        await this.Logger.LogExceptionAsync($"Error reading view file: \"{fileName}\".", ex);
                        return true;
                    },
                    includeSubfolders: true,
                    cancellationToken: cancellationToken))
                .FirstOrDefault();

        #endregion
    }
}
