import { extension, localize } from '@tessa/application';
import { getTessaIcon } from 'common/utility/uiHelpers';
import { showConfirm } from 'tessa/ui/tessaDialog/show';
import { IFileControlExtensionContext } from 'tessa/ui/files/interfaces';
import { FileControlExtension } from 'tessa/ui/files/fileControlExtension';
import { FileTagViewModel } from 'tessa/ui/cards/controls/fileList/fileTagViewModel';
import { FileListViewModel } from 'tessa/ui/cards/controls/fileList/fileListViewModel';
import { OcrKey } from '../misc/ocrConstants';

/**
 * Расширение, реализующее функционал индикации наличия OCR для файла в исходной карточке.
 * @remarks Детальное описание расширения:
 * 1. На файле в каждом файловом контроле исходной карточки устанавливается тэг OCR,
 *    если для него существует соответствующая операция на распознавание файла.
 * 2. Отображение окна предупреждения при удалении файла в исходной карточке,
 *    с которым связана карточка операции OCR.
 */
@extension({ name: 'OcrSourceFileTagUIExtension' })
export class OcrSourceFileTagUIExtension extends FileControlExtension {
  //#region private fields

  private static readonly fileTagSuccess = new FileTagViewModel(getTessaIcon('Int74'), '#00b00030');
  private static readonly fileTagError = new FileTagViewModel(getTessaIcon('Int74'), '#ff000030');

  //#endregion

  //#region base overrides

  override initializing(context: IFileControlExtensionContext): void {
    const fileContainer = context.control.fileContainer;
    const disposer = fileContainer.containerFileChanging.addWithDispose(async args => {
      if (!args.cancel) {
        // Если в добавленном файле есть ключ OCR (такое может произойти, например, при создании копии файла)
        if (args.added?.options?.[OcrKey]) {
          delete args.added?.options[OcrKey];
          args.added.source.notifyOptionsModified(args.added);
          // Если в удаляемом файле есть ключ OCR с существующей карточкой операции OCR
        } else if (args.removed?.options?.[OcrKey]?.['CardID']) {
          args.cancel = !(await showConfirm(
            localize('$UI_Controls_FilesControl_RemoveOcrFileMessage', args.removed.name)
          ));
        }
      }
    });

    disposer && this.disposeList.add(disposer);
  }

  override async initialized(context: IFileControlExtensionContext): Promise<void> {
    if (context.control instanceof FileListViewModel) {
      for (const fileViewModel of context.control.files) {
        const ocrOptions = fileViewModel.model.options?.[OcrKey];
        if (ocrOptions) {
          fileViewModel.tag = ocrOptions['CardID']
            ? OcrSourceFileTagUIExtension.fileTagSuccess
            : OcrSourceFileTagUIExtension.fileTagError;
        }
      }
    }
  }

  //#endregion
}
