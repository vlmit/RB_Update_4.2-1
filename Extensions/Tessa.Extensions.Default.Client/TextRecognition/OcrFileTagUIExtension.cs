#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using Tessa.Files;
using Tessa.Platform.Storage;
using Tessa.TextRecognition.Constants;
using Tessa.UI;
using Tessa.UI.Cards;
using Tessa.UI.Cards.Controls;
using Tessa.UI.Files;

namespace Tessa.Extensions.Default.Client.TextRecognition
{
    /// <summary>
    /// Расширение, в котором выполняется:
    /// <para>
    /// - установка тэга OCR на файле в каждом файловом контроле карточки,
    /// </para>
    /// <para>
    /// - отображение окна предупреждения при удалении файла, с которым связана карточка операции OCR.
    /// </para>
    /// </summary>
    public sealed class OcrFileTagUIExtension : CardUIExtension
    {
        #region Base Overrides

        /// <inheritdoc/>
        public override Task Initialized(ICardUIExtensionContext context)
        {
            context.FileContainer.ContainerFileAdded += this.OnFileAdded;
            context.FileContainer.ContainerFileRemoving += this.OnFileRemoving;

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public override Task ContextInitialized(ICardUIExtensionContext context)
        {
            var iconContainer = context.Icons;

            foreach (var fileListViewModel in context.Model.ControlBag.OfType<FileListViewModel>())
            {
                foreach (var fileViewModel in fileListViewModel.FileControl.Items)
                {
                    var file = fileViewModel.Model;
                    if (file.Options?.TryGet<Dictionary<string, object?>?>(OcrCommon.OcrKey) is { } ocrOptions)
                    {
                        fileViewModel.Tag = ocrOptions.TryGetValue("CardID", out var cardID) && cardID is not null
                            ? new FileTagViewModel("Int74", iconContainer, Color.FromArgb(50, 0, 255, 0))
                            : new FileTagViewModel("Int74", iconContainer, Color.FromArgb(50, 255, 0, 0));
                    }
                }
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public override Task Finalized(ICardUIExtensionContext context)
        {
            context.FileContainer.ContainerFileAdded -= this.OnFileAdded;
            context.FileContainer.ContainerFileRemoving -= this.OnFileRemoving;

            return Task.CompletedTask;
        }

        #endregion

        #region Private

        private async void OnFileAdded(object? sender, FileControlEventArgs e)
        {
            var file = e.File;
            var options = file.Options;

            // Если в добавленном файле есть ключ OCR (такое может произойти, например, при создании копии файла)
            if (options is not null && options.ContainsKey(OcrCommon.OcrKey))
            {
                var deferral = e.Defer();
                try
                {
                    options?.Remove(OcrCommon.OcrKey);
                    await file.NotifyAsync(FileNotificationType.OptionsModified);
                }
                catch (Exception ex)
                {
                    deferral.SetException(ex);
                }
                finally
                {
                    deferral.Dispose();
                }
            }
        }

        private async void OnFileRemoving(object? sender, FileControlCancelEventArgs e)
        {
            var file = e.File;
            var options = file.Options;

            // Если в удаляемом файле есть ключ OCR с существующей карточкой операции OCR
            if (!e.Cancel
                && options is not null
                && options.TryGet<Dictionary<string, object?>?>(OcrCommon.OcrKey) is { } ocrOptions
                && ocrOptions.TryGetValue("CardID", out var cardID)
                && cardID is not null)
            {
                var deferral = e.Defer();
                try
                {
                    var message = await LocalizeFormatAsync("$UI_Controls_FilesControl_RemoveOcrFileMessage", file.Name);
                    if (!await TessaDialog.ConfirmAsync(message))
                    {
                        e.Cancel = true;
                    }
                }
                catch (Exception ex)
                {
                    deferral.SetException(ex);
                }
                finally
                {
                    deferral.Dispose();
                }
            }
        }

        #endregion
    }
}
