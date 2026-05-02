import { TypedField } from '@tessa/core';
import { showMessage, UIButton } from 'tessa/ui';
import { CardUIExtension, ICardUIExtensionContext } from 'tessa/ui/cards';
import { DecimalBoxViewModel } from 'tessa/ui/cards/controls';
import { extension } from '@tessa/application';

@extension()
export class ControlsCustomButtonUIExtension extends CardUIExtension {
  public initialized(context: ICardUIExtensionContext): void {
    const card = context.card;
    const discount = context.model.controls.get('Discount') as DecimalBoxViewModel;
    if (discount) {
      discount.buttonsContainer.addButton(
        UIButton.create({
          name: 'PayTips',
          icon: 'm-plus',
          theme: 'control',
          type: 'small',
          buttonAction: () => {
            const sec = card.sections.get('AbCarMainInfo');
            sec.fields.set(
              'DiscountCost',
              TypedField.createDecimal(
                (parseFloat(sec.fields.get<string>('DiscountCost') ?? '0') + 500).toString()
              )
            );
            showMessage(`You have decided to pay us 500 more as tips!!!`);
          }
        })
      );
    }
  }
}
