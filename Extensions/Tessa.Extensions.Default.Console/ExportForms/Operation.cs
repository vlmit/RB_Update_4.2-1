using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.FormEditor;
using Tessa.Platform;
using Tessa.Platform.ConsoleApps;
using Tessa.Platform.IO;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Console.ExportForms
{
    public sealed class Operation(
        IConsoleSessionManager sessionManager,
        IConsoleLogger logger,
        IFormEditorModelsRepository formEditorModelsRepository)
        : ConsoleOperation<OperationContext>(logger, sessionManager)
    {
        #region Fields

        private readonly IFormEditorModelsRepository formEditorModelsRepository = NotNullOrThrow(formEditorModelsRepository);

        #endregion

        #region Private Constants

        private const string ExportingAddOperationPrefix = "UI_Cards_ExportingAddOperationPrefix";

        #endregion

        #region Base overrides

        public override async Task<int> ExecuteAsync(OperationContext context, CancellationToken cancellationToken = default)
        {
            if (!this.SessionManager.IsOpened)
            {
                return -1;
            }

            try
            {
                var formInfos = (await this.formEditorModelsRepository.QueryAsync(cancellationToken: cancellationToken))
                    .Select(static l => new CardInfo(l.ID, l.Alias))
                    .ToList();

                if (formInfos is not { Count: > 0 })
                {
                    await this.Logger.InfoAsync("No forms to export");
                    return 0;
                }

                string exportPath = DefaultConsoleHelper.NormalizeFolderAndCreateIfNotExists(context.OutputFolder);
                if (string.IsNullOrEmpty(exportPath))
                {
                    exportPath = Directory.GetCurrentDirectory();
                }

                await this.Logger.InfoAsync("Removing existent forms from output folder \"{0}\"", exportPath);
                FileHelper.DeleteFilesByPatterns(exportPath, includeSubfolders: true, "*.jform");

                await this.Logger.InfoAsync(
                    "Exporting forms ({0}):{1}{2}",
                    formInfos.Count,
                    Environment.NewLine,
                    string.Join(Environment.NewLine, formInfos.Select(static x => $"\"{x.CardName}\"")));

                bool result = await this.ExportFormsAsync(
                    formInfos,
                    context.OutputFolder!,
                    cancellationToken);

                if (!result)
                {
                    return -1;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                await this.Logger.LogExceptionAsync("Error exporting forms", e);
                return -1;
            }

            await this.Logger.InfoAsync("Forms are exported successfully");
            return 0;
        }

        #endregion

        #region Private methods

        private async Task<bool> ExportFormsAsync(
            List<CardInfo> formInfoList,
            string outputFolder,
            CancellationToken cancellationToken = default)
        {
            IValidationResultBuilder validationResult = new ValidationResultBuilder();
            var successfulFormNames = new List<string>(formInfoList.Count);

            try
            {
                foreach (var formInfo in formInfoList)
                {
                    var formName = formInfo.CardName;
                    var fileName = $"{CardHelper.GetCardFileNameWithoutExtension(formName)}.jform";
                    var filePath = Path.Combine(outputFolder, fileName);

                    var result = await this.ExportFormCoreAsync(
                        filePath,
                        formInfo.CardID,
                        cancellationToken);

                    if (result.IsSuccessful)
                    {
                        successfulFormNames.Add(formName);
                    }
                    else
                    {
                        DefaultConsoleHelper.AddOperationToValidationResult(
                            ExportingAddOperationPrefix,
                            formName,
                            result,
                            validationResult);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                validationResult.AddException(this, ex);
            }

            if (successfulFormNames.Count != 0)
            {
                validationResult = GetExportingResultWithPreamble(validationResult, successfulFormNames);
            }

            ValidationResult totalResult = validationResult.Build();
            await this.Logger.LogResultAsync(totalResult);

            return totalResult.IsSuccessful;
        }

        private async Task<ValidationResult> ExportFormCoreAsync(
            string fileName,
            Guid formID,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var form = await this.formEditorModelsRepository.TryGetAsync(formID, cancellationToken);
                if (form is null)
                {
                    return ValidationResult.FromText($"Form with ID: {formID} has not been found.", ValidationResultType.Error);
                }

                form.States = await this.formEditorModelsRepository.TryGetStatesAsync(formID, cancellationToken: cancellationToken);
                form.SubForms = await this.formEditorModelsRepository.TryGetSubFormsAsync(formID, cancellationToken: cancellationToken);
                if (!Path.IsPathFullyQualified(fileName))
                {
                    fileName = Path.GetFullPath(fileName);
                }

                var json = StorageHelper.SerializeToTypedJson(form.ToSerializedDictionary(), true);
                if (string.IsNullOrEmpty(json))
                {
                    return ValidationResult.FromText(this, $"Got empty json from {form.Alias}.", ValidationResultType.Error);
                }

                await File.WriteAllTextAsync(fileName, json, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return ValidationResult.FromException(ex);
            }

            return ValidationResult.Empty;
        }

        private static IValidationResultBuilder GetExportingResultWithPreamble(
            IValidationResultBuilder validationResult,
            ICollection<string> successfulCardNames)
        {
            string infoText =
                DefaultConsoleHelper.GetQuotedItemsText(
                        StringBuilderHelper.Acquire(1024),
                        "UI_Cards_CardExported",
                        "UI_Cards_MultipleCardsExported",
                        successfulCardNames)
                    .ToStringAndRelease();

            return new ValidationResultBuilder()
                .AddInfo(typeof(Operation), infoText)
                .Add(validationResult);
        }

        #endregion
    }
}
