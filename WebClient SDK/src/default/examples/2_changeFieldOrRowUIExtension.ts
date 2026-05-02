import { Guid, FieldType } from '@tessa/core';
import { extension } from '@tessa/application';
import { CardRowState, CardRow } from '@tessa/platform';
import { CardUIExtension, ICardUIExtensionContext } from 'tessa/ui/cards';

/**
 * В зависимости от значения флага на форме выбранного типа карточки менять другие поля этой же карточки:
 * - текстовое поле.
 * - добавлять строчку в коллекционную секцию.
 *
 * Когда нажимают галку "Базовый цвет", то:
 * - если галка установлена, то значение поля "Цвет" меняется на "Это базовый цвет".
 * - если галка установлена, то в секцию "Список Акций" добавляется новая строка.
 * - если галка не установлена, то значение поля "Цвет" меняется на "Это другой, не базовый цвет".
 */
@extension()
export class ChangeFieldOrRowUIExtension extends CardUIExtension {
  override async initialized(context: ICardUIExtensionContext): Promise<void> {
    // пытаемся найти секции "Дополнительная информация" и "Список Акций"
    const additionalInfo = context.card.sections.tryGet('AbCarAdditionalInfo');
    const carSales = context.card.sections.tryGet('AbCarSales');
    if (!additionalInfo || !carSales) {
      return;
    }

    // подписываемся на изменения полей в секции
    this.disposeList.add(
      additionalInfo.fields.fieldChanged.add(e => {
        // если было изменено другое поле, то никак не реагируем
        if (e.fieldName !== 'IsBaseColor') {
          return;
        }

        const text = e.fieldValue ? 'Это базовый цвет' : 'Это другой, не базовый цвет';
        // ставим значения поля в секции
        additionalInfo.fields.set('Color', text, FieldType.String);

        if (e.fieldValue) {
          // добавляем новую строчку в табличную секцию
          const newRow = new CardRow();
          newRow.rowId = Guid.newGuid();
          newRow.set('ID', '3db19fa0-228a-497f-873a-0250bf0a4ccb', FieldType.Guid);
          newRow.set('Name', 'Test Row', FieldType.String);
          newRow.set('ManagerID', '3db19fa0-228a-497f-873a-0250bf0a4ccb', FieldType.Guid);
          newRow.set('ManagerName', 'Admin', FieldType.String);
          newRow.state = CardRowState.Inserted;
          carSales.rows.push(newRow);
        }
      })
    );
  }

  override async finalized(): Promise<void> {
    this.disposeList.dispose();
  }
}
