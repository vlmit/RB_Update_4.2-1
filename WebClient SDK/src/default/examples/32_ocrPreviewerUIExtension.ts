import { Guid, FieldStorageMap, FieldType } from '@tessa/core';
import { extension } from '@tessa/application';
import { IControlViewModel } from 'tessa/ui/cards';
import { getDecimalFromRbga } from 'tessa/ui/uiHelper';
import { IOcrRecognizedBox } from 'tessa/platform/textRecognition/entities/ocrRecognizedBox';
import { OcrRecognizedLayout } from 'tessa/platform/textRecognition/entities/ocrRecognizedLayout';
import {
  FilePreviewExtension,
  IFilePreviewExtensionContext,
  IFilePreviewLoadExtensionContext
} from 'tessa/ui/preview';
import { PdfPreviewerViewModel } from 'tessa/ui/preview/pdf/pdfPreviewerViewModel';
import { TestCardTypeID } from './common';

/**
 * Расширение для модификации инструмента предпросмотра PDF в режиме распознавания.
 * - Выполняется переопределение фабрики для создания инструмента предпросмотра.
 * - Устанавливается режим распознавания в модели представления инструмента.
 * - Устанавливается заголовок в средстве предпросмотра.
 * - Добавляется обработчик события выбора распознанного элемента.
 */
@extension({ name: 'ExamplePreviewerExtension' })
export class OcrPreviewerUIExtension extends FilePreviewExtension {
  //#region fields

  private _recognizedBoxSelectedDisposer: VoidFunction | null | undefined = null;

  //#endregion

  //#region FilePreviewExtension

  override async initializing(context: IFilePreviewExtensionContext): Promise<void> {
    const { previewerViewModel, cardModel, fileVersion } = context;
    if (
      !Guid.equals(cardModel?.card.typeId, TestCardTypeID) ||
      !(previewerViewModel instanceof PdfPreviewerViewModel)
    ) {
      return;
    }

    // ищем для файла связанный с ним оригинал файла в формате JSON
    const metadataFile = fileVersion.file.origin?.lastVersion;
    const recognitionMode = !!metadataFile && metadataFile.getExtension() === 'json';

    if (recognitionMode) {
      previewerViewModel.recognitionMode = recognitionMode;
      previewerViewModel.header.showTitle = true;
      // переопределим метод (если он был задан) для модификации заголовка
      previewerViewModel.header.title = name => {
        return fileVersion!.number > 1 ? `Modified file name - "${name}"` : name;
      };
    }
  }

  override async loaded(context: IFilePreviewLoadExtensionContext): Promise<void> {
    const { previewerViewModel, cardModel } = context;
    if (
      !cardModel ||
      !Guid.equals(cardModel.card.typeId, TestCardTypeID) ||
      !(previewerViewModel instanceof PdfPreviewerViewModel)
    ) {
      return;
    }

    const fieldName = 'Color';
    const control = cardModel.controls.get(fieldName)!;
    const mainFields = cardModel.card.sections.get('AbCarMainInfo').fields;
    const additionalFields = cardModel.card.sections.get('AbCarAdditionalInfo').fields;

    // подписываемся на выбор распознанного элемента в предпросмотре и добавляем обработчик
    // выполняем это здесь, так как только после загрузки файла будет задана recognizedCollection
    const boxSelectedHandler = previewerViewModel.recognizedCollection?.recognizedBoxSelected;
    this._recognizedBoxSelectedDisposer = boxSelectedHandler?.add(box =>
      this.onRecognizedBoxSelect(box, fieldName, mainFields, additionalFields, control)
    );
    // установим режим отображения распознанных элементов
    previewerViewModel.recognizedLayout = boxSelectedHandler
      ? OcrRecognizedLayout.Word
      : OcrRecognizedLayout.Default;
  }

  override finalized(): void {
    this._recognizedBoxSelectedDisposer?.();
    this._recognizedBoxSelectedDisposer = null;
  }

  //#endregion

  //#region methods

  private onRecognizedBoxSelect(
    box: IOcrRecognizedBox | null,
    fieldName: string,
    mainStorage: FieldStorageMap,
    additionalStorage: FieldStorageMap,
    control: IControlViewModel
  ): void {
    // Настраиваем цвет фона контрола
    const background = box?.color?.main || '';
    const corners = background ? 'var(--control-corners)' : '';
    control.controlTheme.setValue('background', background);
    control.controlTheme.setValue('borderRadius', corners);
    control.isReadOnly = !!background;
    // Устанавливаем значение полей
    additionalStorage.systemSet(fieldName, box?.text ?? '', FieldType.String);
    mainStorage.systemSet(fieldName, getDecimalFromRbga(background) ?? null, FieldType.Int);
  }

  //#endregion
}
