using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Tessa.FormEditor;
using Tessa.FormEditor.Models;
using Tessa.Platform.ConsoleApps;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Console.ImportForms
{
    /// <summary>
    /// Операция импорта представлений
    /// </summary>
    public sealed class Operation(
        IConsoleLogger logger,
        IConsoleSessionManager sessionManager,
        IFormEditorModelsRepository formEditorModelsRepository)
        : ConsoleOperation<OperationContext>(logger, sessionManager)
    {
        #region Fields

        private readonly IFormEditorModelsRepository formEditorModelsRepository = NotNullOrThrow(formEditorModelsRepository);

        #endregion

        #region Base overrides

        /// <inheritdoc />
        public override async Task<int> ExecuteAsync(OperationContext context, CancellationToken cancellationToken = default)
        {
            if (!this.SessionManager.IsOpened)
            {
                return -1;
            }

            ThrowIfNull(context.Source);

            await this.Logger.InfoAsync($"Importing forms from \"{context.Source}\"");

            var files = GetDirectoryFiles(context.Source);

            if (files.Length == 0)
            {
                await this.Logger.InfoAsync("No files in \"{0}\" to import.", context.Source);
                return 0;
            }

            await this.Logger.InfoAsync("Found forms ({0})", files.Length);

            try
            {
                var forms = new List<FormEditorRootModel>(files.Length);
                foreach (var file in files)
                {
                    var formJson = await File.ReadAllTextAsync(file, cancellationToken);
                    var storage = StorageHelper.DeserializeFromTypedJson(formJson);
                    if (storage is null)
                    {
                        await this.Logger.InfoAsync($"Got empty storage from {file}, file will be skipped.");
                        continue;
                    }

                    forms.Add(storage.FromSerializedDictionary<FormEditorRootModel>());
                }

                if (forms.Count == 0)
                {
                    return 0;
                }

                var result = await this.formEditorModelsRepository.ImportAsync(forms.ToArray(), cancellationToken: cancellationToken);
                if (!result.IsSuccessful)
                {
                    await this.Logger.LogResultAsync(result);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                await this.Logger.LogExceptionAsync("Error importing forms", e);
                return -1;
            }

            await this.Logger.InfoAsync("Forms are imported successfully");
            return 0;
        }

        #endregion

        #region Private methods

        private static string[] GetDirectoryFiles(string path)
        {
            var directory = NotNullOrThrow(Path.GetDirectoryName(path) ?? path);
            var extension = Path.GetExtension(path);
            var fileName = Path.GetFileName(path);

            if (string.IsNullOrEmpty(extension))
            {
                fileName = "*.jform";
                directory = path;
            }

            return Directory.GetFiles(directory, fileName);
        }

        #endregion
    }
}
