import { extension } from '@tessa/application';
import { CardUIExtension, ICardUIExtensionContext } from 'tessa/ui/cards';
import { GridViewModel } from 'tessa/ui/cards/controls';

/**
 * Подписываемся на двойной клик в контроле таблицы выбранной карточки.
 *
 * Результат работы расширения:
 * В карточке "Автомобиль" подписываемся на двойной клик в контроле таблицы "Список Акций".
 */
@extension()
export class TableControlDoubleClickUIExtension extends CardUIExtension {
  override async initialized(context: ICardUIExtensionContext): Promise<void> {
    // пытаемся получить контрол "Список Акций"
    const tableControl = context.model.controls.get('ShareList') as GridViewModel;
    if (!tableControl) {
      return;
    }

    // подписываемся на открытие строки
    this.disposeList.add(tableControl.rowInitializing.addWithDispose(e => console.log(e))!);
  }

  override async finalized(): Promise<void> {
    this.disposeList.dispose();
  }
}
