#nullable enable

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Tessa.Files;
using Tessa.Platform;
using Tessa.UI.Cards;
using Tessa.UI.Cards.Controls;
using Tessa.UI.Files.Controls;

namespace Tessa.Extensions.Default.Client.UI.CardFiles
{
    /// <summary>
    /// Объект, производящий дополнительную обработку UI событий перетаскивания файлов через <see cref="ViewCardControlDropBehavior"/>.
    /// </summary>
    internal sealed class FilesDragDrop : DefaultDragDrop
    {
        #region Fields

        private readonly ICardModel cardModel;
        private readonly string fileControlName;

        #endregion

        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="FilesDragDrop"/>.
        /// </summary>
        /// <param name="cardModel">Модель карточки.</param>
        /// <param name="fileControlName">Имя файлового контрола.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public FilesDragDrop(
            ICardModel cardModel,
            string fileControlName)
        {
            this.cardModel = NotNullOrThrow(cardModel);
            this.fileControlName = fileControlName;
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc />
        public override bool CanDrag => true;

        /// <inheritdoc />
        public override async ValueTask<object?> StartDragAsync(object sender, MouseEventArgs e, CancellationToken cancellationToken = default)
        {
            var fileControl = CardFilesHelper.TryGetFileControl(this.cardModel.Info, this.fileControlName);

            // когда drop реально начался, но предпросмотр занят загрузкой файла, то мы не запускаем drag&drop.
            if (fileControl is null
                || fileControl.Manager.IsPreviewInProgress(childrenOnly: true))
            {
                return null;
            }

            fileControl.StopTimer();
            var operationFiles = fileControl.SelectedFiles.ToArray<IFileObject>();
            if (!await FileControlHelper.CheckCanDownloadFilesAndShowMessagesAsync(operationFiles))
            {
                return null;
            }

            // При перетаскивании файлов записываем результаты валидации загрузки файлов в лог, но не выводим их пользователю.
            var (pathList, result) = await FileControlHelper.DownloadContentAsync(operationFiles, fileControl, cancellationToken);
            TessaLoggers.Validation.LogResultItems(result);
            if (pathList is not { Count: > 0 })
            {
                return null;
            }

            // мы получили контент файлов и не упали - выполняем drag&drop.
            var dataObj = new DataObject(DataFormats.FileDrop, pathList.ToArray());
            dataObj.SetData("DragSource", fileControl);

            return dataObj;
        }

        /// <inheritdoc />
        public override void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left
                && e.ClickCount == 2)
            {
                var fileControl = CardFilesHelper.TryGetFileControl(this.cardModel.Info, this.fileControlName);
                fileControl?.StopTimer();
            }
        }

        /// <inheritdoc />
        public override async void OnDrop(object sender, DragEventArgs e)
        {
            var fileControl = CardFilesHelper.TryGetFileControl(this.cardModel.Info, this.fileControlName);
            fileControl?.DropAction(sender, e);
        }

        /// <inheritdoc />
        public override async void DragOver(object sender, DragEventArgs e)
        {
            var fileControl = CardFilesHelper.TryGetFileControl(this.cardModel.Info, this.fileControlName);
            fileControl?.DragOverAction(sender, e);
        }

        #endregion
    }

}
