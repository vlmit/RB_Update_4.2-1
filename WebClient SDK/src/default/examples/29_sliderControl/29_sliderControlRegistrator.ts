import { CardControlTypeRegistry } from '@tessa/platform';
import { ComponentsRegistry } from '@tessa/ui';
import { CardControlRegistry } from 'tessa/ui/cards/cardControlRegistry';
import { SliderControlType } from './29_sliderControlType';
import { SliderType } from './29_sliderType';
import { SliderControl } from './29_sliderControl';

export function registerSliderControlTypes(): void {
  CardControlTypeRegistry.instance.register(SliderControlType);
  CardControlRegistry.instance.register(SliderControlType.id, SliderType);
  ComponentsRegistry.instance.register(SliderControlType.id, SliderControl);
}
