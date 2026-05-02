import { AbExternalFile } from './abExternalFile';
import { FileExtension, IFileExtensionContext } from 'tessa/ui/files';
import { UIContext } from 'tessa/ui';
import { ICardModel } from 'tessa/ui/cards';
import { extension } from '@tessa/application';

@extension()
export class AbExternalFileExtension extends FileExtension {
  public openingMenu(context: IFileExtensionContext): void {
    // Проверяем тип карточки
    const editor = UIContext.current.cardEditor;
    let model: ICardModel;
    if (editor == null || !(model = editor.cardModel!) || model.cardType.name !== 'AbCar') {
      return;
    }

    // Отключаем пункт "Копировать ссылку"
    const copyLinkAction = context.actions.find(p => p.name === 'CopyLink');
    if (context.file.model instanceof AbExternalFile && copyLinkAction) {
      copyLinkAction.isCollapsed = true;
    }
  }
}
