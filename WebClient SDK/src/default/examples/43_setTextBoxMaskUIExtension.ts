import { extension } from '@tessa/application';
import { Guid } from '@tessa/core';
import { CardUIExtension, ICardUIExtensionContext } from 'tessa/ui/cards';
import { GridViewModel, TextBoxViewModel } from 'tessa/ui/cards/controls';

const AbCarCardTypeID = 'd0006e40-a342-4797-8d77-6501c4b7c4ac';

/** Пример установки маски для строкового контрола. */
@extension({ name: 'SetTextBoxMaskUIExtension' })
export class SetTextBoxMaskUIExtension extends CardUIExtension {
  public getExtensionName(): string {
    return 'SetTextBoxMaskUIExtension';
  }

  shouldExecute(context: ICardUIExtensionContext): boolean {
    return Guid.equals(context.card.typeId, AbCarCardTypeID);
  }

  public initialized(context: ICardUIExtensionContext): void {
    // Маска на строковый контрол "Телефон покупателя".
    const grid = context.model.controls.get('ShareList') as GridViewModel;
    if (grid) {
      grid.rowInitializing.addWithDispose(e => {
        const buyersControl = e.rowModel?.controls.get('Buyers') as GridViewModel;
        if (buyersControl) {
          buyersControl.rowInitializing.addWithDispose(event => {
            const phoneControl = event.rowModel?.controls.get('BuyerPhone') as TextBoxViewModel;
            if (phoneControl) {
              phoneControl.maskOptions = {
                mask: '+7 (000) 000-00-00',
                placeholder: '+7 (___) ___-__-__',
                lazy: false,
                // Блокируем вставку, если там нет цифр
                onPaste: e => {
                  const pasted = e.clipboardData?.getData('Text') ?? '';
                  if (/\D/.test(pasted) && !/\d/.test(pasted)) {
                    e.preventDefault(); // отменяем вставку, так как отсутствуют цифры
                    return;
                  }

                  // если есть цифры — оставляем только их
                  const numbersOnly = pasted.replace(/\D/g, '');
                  e.preventDefault();
                  document.execCommand('insertText', false, numbersOnly);
                }
              };
            }
          });
        }
      });
    }
  }
}
