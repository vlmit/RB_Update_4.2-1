import {
  CardTypeForm,
  CardTypeBlock,
  CardTypeControl,
  CardTypeEntryControl
} from '@tessa/platform';
import { injectable } from '@tessa/application';
import { ControlTypeBase } from 'tessa/ui/cards/controls';
import { ICardModel, IControlViewModel } from 'tessa/ui/cards';
import { SliderViewModel } from './29_sliderViewModel';

@injectable()
export class SliderType extends ControlTypeBase {
  protected async createControlCore(
    control: CardTypeControl,
    _block: CardTypeBlock,
    _form: CardTypeForm,
    _parentControl: CardTypeControl | null,
    model: ICardModel
  ): Promise<IControlViewModel> {
    return new SliderViewModel(control as CardTypeEntryControl, model);
  }
}
