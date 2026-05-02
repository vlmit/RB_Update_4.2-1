import { extension } from '@tessa/application';
import {
  CardTypeCustomControl,
  CardTypeTabControl,
  CardMetadataRepositoryExtension,
  ICardMetadataRepositoryExtensionCardTypeContext
} from '@tessa/platform';
import { OcrOperationTypeId } from '../misc/ocrConstants';
import { OcrGridControlType } from '../components/grid/ocrGridTypes';

/**
 * Расширение, добавляющее контрол обозревателя свойств в тип карточки операции OCR.
 * @remarks Детальное описание расширения:
 * 1. В расширении выполняется поиск контейнера с контролами по его псевдониму.
 * 2. В контейнер с контролами добавляется новый контрол обозревателя свойств.
 */
@extension({ name: 'OcrMetadataExtension' })
export class OcrMetadataExtension extends CardMetadataRepositoryExtension {
  //#region base overrides

  override async cardTypeLoaded(
    context: ICardMetadataRepositoryExtensionCardTypeContext
  ): Promise<void> {
    const cardType = context.cardTypes.getCardTypeById(OcrOperationTypeId);
    if (!cardType) {
      return;
    }

    const controls = cardType.forms[0]?.blocks[0]?.controls;
    const container = controls?.find(c => c.name === 'ControlsContainer') as CardTypeTabControl;
    if (!container) {
      return;
    }

    const containerControls = container.forms[0]?.blocks[0]?.controls;
    if (!containerControls) {
      return;
    }

    if (containerControls.some(x => x.name === 'OcrGrid')) {
      return;
    }

    const propertyGridType = new CardTypeCustomControl();
    propertyGridType.type = OcrGridControlType;
    propertyGridType.name = 'OcrGrid';
    propertyGridType.controlSettings['Properties'] = [];
    propertyGridType.blockSettings = containerControls[0]?.blockSettings;

    containerControls.push(propertyGridType);
  }

  //#endregion
}
