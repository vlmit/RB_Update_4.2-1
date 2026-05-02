import { extension } from '@tessa/application';
import {
  CardTypeEntryControl,
  CardMetadataRepositoryExtension,
  ICardMetadataRepositoryExtensionCardTypeContext
} from '@tessa/platform';
import { TestCardTypeID } from '../common';
import { SliderControlType } from './29_sliderControlType';

/**
 * Пример данного расширения позволяет создавать собственные контролы и добавлять
 * их в выбранную карточку.
 *
 * Результат работы расширения:
 * Создаем кастомный слайдер-контрол и добавляем его в блок "Общая информация" тестовой
 * карточки "Автомобиль".
 */
@extension()
export class SliderMetadataExtension extends CardMetadataRepositoryExtension {
  override async cardTypeLoaded(
    context: ICardMetadataRepositoryExtensionCardTypeContext
  ): Promise<void> {
    const cardType = context.cardTypes.getCardTypeById(TestCardTypeID);
    if (!cardType) {
      return;
    }

    // пытаемся получить вкладку "Карточка"
    const cardTab = cardType.forms.find(tab => tab.name === 'Main');
    if (!cardTab) {
      return;
    }

    // пытаемся получить блок "Общая информация"
    const mainBlock = cardTab.blocks.find(block => block.name === 'MainInfo');
    if (!mainBlock) {
      return;
    }

    // если контрол с таким алиасом уже есть - выходим
    if (mainBlock.controls.some(x => x.name === 'OurSuperMegaCoolSlider')) {
      return;
    }

    // создаем новый контрол и задаем ему основные параметры
    const sliderType = new CardTypeEntryControl();
    sliderType.type = SliderControlType;
    sliderType.name = 'OurSuperMegaCoolSlider';
    sliderType.caption = 'Slider';
    sliderType.controlSettings = {
      MinValue: 20,
      MaxValue: 150,
      Step: 1
    };
    sliderType.blockSettings = {
      StartAtNewLine: true
    };
    sliderType.sectionId = '509d961f-00cf-4403-a78f-6736841de448';
    sliderType.physicalColumnIdList = ['ef4db447-b0b5-4474-a6b7-6c5c75465355'];

    // добавляем созданный контрол в блок "Общая информация"
    mainBlock.controls.push(sliderType);
  }
}
