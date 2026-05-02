import { FieldType } from '@tessa/core';
import { extension } from '@tessa/application';
import { CardRowsListener } from '@tessa/platform';
import { CardUIExtension, ICardUIExtensionContext } from 'tessa/ui/cards';

/**
 * Позволяет отслеживать добавление\удаление строк в коллекционной секции для определенного типа карточки.
 *
 * Результат работы расширения:
 * В тестовой карточке "Автомобиль" при добавлении или удалении строки из контрола "Список Акций"
 * секции "AbCarSales" в поле "Цвет" добавлется текст с соответсвующим идентификатором строки и
 * наименованием действия (добавления или удаления соответстенно).
 */
@extension()
export class TableSectionChangedUIExtension extends CardUIExtension {
  private _listener: CardRowsListener | null;

  override async initialized(context: ICardUIExtensionContext): Promise<void> {
    // пытаемся достать секции, в которых находятся соответствующие
    // контролы "Список Акций" и "Цвет"
    const carSales = context.card.sections.tryGet('AbCarSales');
    const additionalInfo = context.card.sections.tryGet('AbCarAdditionalInfo');

    if (!carSales || !additionalInfo) {
      return;
    }

    // если есть активные листенеры, то останавливаем их
    if (this._listener) {
      this._listener.stop();
    }

    // можно подписаться на изменения в коллекции через carSales.rows.collectionChanged.
    // В этом случае handler будет вызываться только когда элемент добавляется или удаляется из коллекции.
    // Чтобы правильно обрабатывать изменения состояния строк, лучше использовать
    // хелперный класс CardRowsListener.
    // В этом случае handler будет вызываться, когда состояния строк меняются.

    this._listener = new CardRowsListener();

    // подписываемся на изменение поля "Цвет" при добавлении строки
    this._listener.rowInserted.add(({ row }) => {
      let text: string = additionalInfo.fields.tryGet('Color') || '';
      text += `\nRow with id = "${row.rowId}" has been added.`;
      additionalInfo.fields.set('Color', text, FieldType.String);
    });

    // подписываемся на изменение поля "Цвет" при удалении строки
    this._listener.rowDeleted.add(({ row }) => {
      let text: string = additionalInfo.fields.tryGet('Color') || '';
      text += `\nRow with id = "${row.rowId}" has been deleted.`;
      additionalInfo.fields.set('Color', text, FieldType.String);
    });

    // запускаем листенер на добавление/удаление строк
    this._listener.start(carSales.rows);
  }

  override async finalized(): Promise<void> {
    // Выключаем листенеры и очищаем все, что было с ними связано
    if (this._listener) {
      this._listener.dispose();
      this._listener = null;
    }
  }
}
