import { StorageHelper } from '@tessa/core';
import { extension } from '@tessa/application';
import { CardUIExtension, ICardUIExtensionContext } from 'tessa/ui/cards';
import { FileListViewModel } from 'tessa/ui/cards/controls';
import { showMessage } from 'tessa/ui/tessaDialog';

/**
 * Пример данного расширения позволяет для определденного типа карточки:
 * - Фильтровать содержимое файлового контрола по определенной категории.
 * - Добавлять файлы только с определенным расширением.
 *
 * Результат работы расширения:
 * В карточке типа "Автомобиль" выполняет фильтрацию файлового контрола "Без изображений (всё, кроме категории "Image")"
 * по названию категории: отображает файлы только с категорией "Text".
 * Запрещает добавление файлов с разрешением, отличным от ".txt", для всех файлововых контролов выбранной карточки.
 */
@extension()
export class FileControlUIExtension extends CardUIExtension {
  override async initialized(context: ICardUIExtensionContext): Promise<void> {
    // пытаемся получить контрол "Без изображений (всё, кроме категории "Image")"
    const allFilesControl = StorageHelper.tryGet<FileListViewModel | null>(
      context.model.info,
      'AllFilesControl'
    );

    if (!allFilesControl) {
      return;
    }

    // показываем файлы только с категорией Text
    allFilesControl.removeFiles(
      file => !(file.model.category && file.model.category.caption === 'Text')
    );
    // если добавляется файл с расширением отличным от 'txt', то прерываем добавление
    this.disposeList.add(
      context.fileContainer.containerFileChanging.addWithDispose(async e => {
        const file = e.added;
        if (file && file.getExtension() !== 'txt') {
          await showMessage(`Вы должны приложить файл только с расширением 'txt'.`);
          e.cancel = true;
        }
      })!
    );
  }

  override async finalized(): Promise<void> {
    this.disposeList.dispose();
  }
}
