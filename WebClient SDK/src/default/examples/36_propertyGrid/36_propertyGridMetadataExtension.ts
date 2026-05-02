import { extension } from '@tessa/application';
import {
  CardControlTypes,
  CardTypeEntryControl,
  CardMetadataRepositoryExtension,
  ICardMetadataRepositoryExtensionCardTypeContext
} from '@tessa/platform';
import { TestCardTypeID } from '../common';

/**
 * Расширение для добавления кнопки открытия обозревателя свойств
 * в блок "Основная информация" тестовой карточки "Автомобиль".
 */
@extension({ name: 'PropertyGridMetadataExtension' })
export class PropertyGridMetadataExtension extends CardMetadataRepositoryExtension {
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

    // пытаемся получить блок "Дополнительная информация"
    const mainBlock = cardTab.blocks.find(block => block.name === 'MainInfo');
    if (!mainBlock) {
      return;
    }

    // если контрол с таким алиасом уже есть - выходим
    if (mainBlock.controls.some(x => x.name === 'OpenPropertyGrid')) {
      return;
    }

    // создаем новый контрол кнопки и задаем ему основные параметры
    const buttonType = new CardTypeEntryControl();
    buttonType.type = CardControlTypes.ButtonControlType;
    buttonType.name = 'OpenPropertyGrid';
    buttonType.caption = 'Open property grid';
    buttonType.controlSettings = { UseAllSpace: true };
    buttonType.blockSettings = { StartAtNewLine: true, ControlSpan: true };

    // добавляем созданный контрол в блок ""Дополнительная информация"
    mainBlock.controls.push(buttonType);
  }
}
