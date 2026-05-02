import { extension } from '@tessa/application';
import {
  CardMetadataRepositoryExtension,
  ICardMetadataRepositoryExtensionCardTypeContext,
  CardTypeEntryControl
} from '@tessa/platform';
import { OcrSliderControlType } from '../components/slider/ocrSliderType';
import { OcrRequestDialogTypeID } from '../misc/ocrConstants';

/**
 * Расширение, добавляющее контрол ползунка в тип диалога с параметрами запроса для операции OCR.
 * @remarks Детальное описание расширения:
 * 1. В расширении выполняется поиск уже существующего скрытого контрола по его псевдониму.
 * 2. Найденный контрол заменяется новым контролом ползунка.
 */
@extension({ name: 'OcrRequestMetadataExtension' })
export class OcrRequestMetadataExtension extends CardMetadataRepositoryExtension {
  //#region base overrides

  override async cardTypeLoaded(
    context: ICardMetadataRepositoryExtensionCardTypeContext
  ): Promise<void> {
    const cardType = context.cardTypes.getCardTypeById(OcrRequestDialogTypeID);
    if (!cardType) {
      return;
    }

    const block = cardType.forms[0]?.blocks[0];
    if (!block) {
      return;
    }

    const control = block.controls.find(c => c.name === 'Confidence');
    if (!control) {
      return;
    }

    const sources = control.getSourceInfo();

    const sliderType = new CardTypeEntryControl();
    sliderType.type = OcrSliderControlType;
    sliderType.name = control.name;
    sliderType.caption = control.caption;
    sliderType.toolTip = control.toolTip;
    sliderType.blockSettings = control.blockSettings;
    sliderType.controlSettings = control.controlSettings;
    sliderType.sectionId = sources.sectionId;
    sliderType.physicalColumnIdList = sources.columnIds;

    block.controls.splice(1, 0, sliderType);
  }

  //#endregion
}
