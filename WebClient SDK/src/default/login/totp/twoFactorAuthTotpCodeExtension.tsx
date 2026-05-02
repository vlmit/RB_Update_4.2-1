import { observer } from 'mobx-react-lite';
import { QRCodeSVG } from 'qrcode.react';
import { extension, injectable } from '@tessa/application';
import {
  CardHelper,
  CardMetadataRepositoryExtension,
  ICardMetadataRepositoryExtensionCardTypeContext
} from '@tessa/platform';
import { Visibility } from 'tessa/platform/visibility';
import { CardControlType, CardControlTypeFlags, CardControlTypeUsageMode } from 'tessa/cards';
import {
  CardTypeBlock,
  CardTypeControl,
  CardTypeEntryControl,
  CardTypeForm
} from 'tessa/cards/types';
import { ICardModel, IControlViewModel } from 'tessa/ui/cards';
import { ControlTypeBase, TextBlockViewModel } from 'tessa/ui/cards/controls';
import { ControlProps } from 'tessa/ui/cards/components/controls';
import ControlContainer from 'ui/controlContainer/controlContainer';

/** Контрол для TOTP-кода. */
export const TwoFactorAuthTotpCode = observer<ControlProps<TextBlockViewModel>>(
  function TwoFactorAuthTotpCodeControl({ viewModel }) {
    const { tooltip, text, controlContainer } = viewModel;

    const controlVisibility = viewModel.controlVisibility;
    if (!text || controlVisibility === Visibility.Collapsed) {
      return null;
    }

    return (
      <ControlContainer viewModel={controlContainer}>
        <div className={viewModel.className.result} title={tooltip}>
          <QRCodeSVG value={text} />
        </div>
      </ControlContainer>
    );
  }
);

/** Тип контрола для TOPT-кода. */
@injectable()
export class TwoFactorAuthTotpCodeType extends ControlTypeBase {
  protected async createControlCore(
    control: CardTypeControl,
    _block: CardTypeBlock,
    _form: CardTypeForm,
    _parentControl: CardTypeControl | null,
    model: ICardModel
  ): Promise<IControlViewModel> {
    return new TextBlockViewModel(control as CardTypeEntryControl, model);
  }
}

/** Экземпляр типа контрола для TOTP-кода. */
export const TwoFactorAuthTotpCodeControlType = new CardControlType(
  'b8affe52-65ea-4282-96e1-fd81d8a6b186',
  'TwoFactorAuthTotpCode',
  CardControlTypeUsageMode.Entry,
  CardControlTypeFlags.UseEverywhere
);

/**
 * Расширение на метаданные приложения, в котором происходит добавление
 * контролов в диалог настроек для двухфакторной аутентификации
 * с использованием одноразового пароля на основе времени.
 */
@extension({ name: 'TwoFactorAuthTotpMetadataExtension' })
export class TwoFactorAuthTotpMetadataExtension extends CardMetadataRepositoryExtension {
  override async cardTypeLoaded(
    context: ICardMetadataRepositoryExtensionCardTypeContext
  ): Promise<void> {
    const cardType = context.cardTypes.getCardTypeById(CardHelper.TwoFactorAuthTotpTypeID);
    if (!cardType) {
      return;
    }

    const block = cardType.forms[0]?.blocks[0];
    if (!block) {
      return;
    }

    const control = block.controls.find(c => c.name === 'Uri');
    if (!control) {
      return;
    }

    control.type = TwoFactorAuthTotpCodeControlType;
  }
}
