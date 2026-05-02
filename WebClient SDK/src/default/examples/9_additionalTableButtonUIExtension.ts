import { autorun } from 'mobx';
import { Guid } from '@tessa/core';
import { CardRowState } from '@tessa/platform';
import { extension } from '@tessa/application';
import { CardUIExtension, ICardUIExtensionContext } from 'tessa/ui/cards';
import {
  GridViewModel,
  AutoCompleteEntryViewModel,
  TextBoxViewModel
} from 'tessa/ui/cards/controls';
import { UIButton } from 'tessa/ui';

/**
 * Пример данного расширения определяет следующую логику для выбранного типа карточки:
 * - Логика при нажатии на кнопку на форме с карточкой.
 * - Логика при нажатии произвольной клавиши на активном контроле.
 *
 * Результат работы расширения:
 * Для карточки типа "Автомобиль" добавляет дополнительную кнопку справа под контролом таблицы.
 * При нажатии на кнопку происходит получение данных из выделенной строки и, после,
 * добавляется новая строка в секцию с полученными данными (создается копия выделенной строки).
 * При клике на контрол "Марка автомобиля" и последующем нажатии на клавишу Enter фокус перемещается
 * на контрол "Имя водителя".
 */
@extension()
export class AdditionalTableButtonUIExtension extends CardUIExtension {
  override async initialized(context: ICardUIExtensionContext): Promise<void> {
    // пытемся получить, соответственно, контролы "Марка автомобиля" и "Имя водителя"
    const carNameControl = context.model.controls.get('CarName') as TextBoxViewModel;
    const driverNameControl = context.model.controls.get(
      'DriverName'
    ) as AutoCompleteEntryViewModel;
    if (carNameControl && driverNameControl) {
      this.disposeList.add(
        carNameControl.keyDown.addWithDispose(e => {
          if (e.event.key === 'Enter') {
            // enter
            console.log('Enter key was pressed');

            if (driverNameControl) {
              // при нажатии на клавишу Enter и наличии рассматриваемого контрола в условии
              // фокусируемся на контроле "Имя водителя"
              driverNameControl.focus();
            }
          }
        })!
      );
    }

    // пытаемся найти грид по алиасу (контрол "Список Акций")
    const table = context.model.controls.get('ShareList') as GridViewModel;
    if (!table) {
      return;
    }

    // пытаемся найти секцию "Список Акций"
    const section = context.card.sections.tryGet('AbCarSales');
    if (!section) {
      return;
    }

    // создаем кнопку копирования строки
    const copyButton = UIButton.create({
      name: 'CopyButton',
      caption: 'CopyButton',
      buttonAction: () => {
        // получаем текущую выбранную строку
        const selectedRow = table.selectedRow;
        if (!selectedRow) {
          return;
        }

        // копируем всем данные и добавляем в секцию
        const clonedRow = selectedRow.row.clone();
        clonedRow.rowId = Guid.newGuid();
        clonedRow.state = CardRowState.Inserted;
        section.rows.push(clonedRow);
      },
      type: 'small',
      theme: 'transparent'
    });

    // добавляем к кнопкам справа
    table.rightButtons.push(copyButton);

    // следим за selectedRow; когда значение меняется, то мы устанавливаем доступность кнопки
    this.disposeList.add(autorun(() => copyButton.setIsEnabled(!!table.selectedRow)));
  }

  override async finalized(): Promise<void> {
    this.disposeList.dispose();
  }
}
