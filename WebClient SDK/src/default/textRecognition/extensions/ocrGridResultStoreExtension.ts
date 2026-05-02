import { FieldType, Guid } from '@tessa/core';
import { extension } from '@tessa/application';
import { CardStoreExtension, ICardStoreExtensionContext } from '@tessa/platform';
import { OcrGridDataStorage, OcrGridDataConverter } from '../components/grid/ocrGridTypes';
import { OcrGridViewModel } from '../components/grid/ocrGridViewModel';
import { OcrOperationTypeId } from '../misc/ocrConstants';
import { OcrHelper } from '../misc/ocrHelper';

/**
 * Расширение, в котором выполняется сохранение результата верификации
 * из обозревателя свойств в поле для маппинга карточки операции OCR.
 */
@extension({ name: 'OcrGridResultStoreExtension' })
export class OcrGridResultStoreExtension extends CardStoreExtension {
  //#region base overrides

  shouldExecute(context: ICardStoreExtensionContext): boolean {
    return Guid.equals(context.cardType?.id, OcrOperationTypeId);
  }

  async beforeRequest(context: ICardStoreExtensionContext): Promise<void> {
    if (!context.validationResult.isSuccessful) {
      return;
    }

    const ocrFields = context.request.card.sections.tryGet('OcrOperations')?.fields;
    if (!ocrFields) {
      return;
    }

    // поиск контрола обозревателя свойств
    const controls = OcrHelper.getContextEditorModel().model?.controlsBag;
    const ocrGrid = controls?.find(c => c.name === 'OcrGrid') as OcrGridViewModel;
    if (!ocrGrid) {
      return;
    }

    // получение результирующих данных от обозревателя свойств
    const ocrDataStorage = ocrGrid.control.dataProvider.data as OcrGridDataStorage;
    // фильтрация и получение только тех данных, которые были изменены в карточке операции OCR
    const ocrDataEntries = Object.entries(ocrDataStorage).filter(([_, data]) => data.modified);
    // сериализация данных в JSON-формат и их запись в поле карточки
    const ocrDataObject = Object.fromEntries(ocrDataEntries);
    const ocrMappingJson = OcrGridDataConverter.serialize(ocrDataObject);
    ocrFields.set('Mapping', ocrMappingJson, FieldType.String);
  }

  //#endregion
}
