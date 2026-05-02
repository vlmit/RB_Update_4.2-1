import { Guid } from '@tessa/core';
import { extension, inject } from '@tessa/application';
import {
  FilePreviewExtension,
  IFilePreviewLoadExtensionContext,
  IFilePreviewResolveExtensionContext,
  IPreviewerViewModelFactory,
  IPreviewPrinterFactory,
  IPreviewPrinterFactory$
} from 'tessa/ui/preview';
import { OnlyOfficeApi } from './onlyOfficeApi';
import { OnlyOfficeApiSingleton } from './onlyOfficeApiSingleton';
import { OnlyOfficeEditorPreviewViewModel } from './onlyOfficeEditorPreviewViewModel';
import { OcrOperationTypeId } from '../textRecognition/misc/ocrConstants';

@extension({ name: 'OnlyOfficePreviewerExtension' })
export class OnlyOfficePreviewerExtension extends FilePreviewExtension {
  constructor(
    @inject(IPreviewPrinterFactory$) private readonly _printerFactory: IPreviewPrinterFactory
  ) {
    super();
  }
  //#region fields

  private _factory: IPreviewerViewModelFactory = ({ previewManager, fileVersion }) => {
    const previewer = new OnlyOfficeEditorPreviewViewModel(
      previewManager,
      fileVersion,
      OnlyOfficeApiSingleton.instance,
      this._printerFactory()
    );

    previewer.header.showTitle = false;

    return previewer;
  };

  //#endregion

  //#region FilePreviewExtension

  override async resolve(context: IFilePreviewResolveExtensionContext): Promise<void> {
    const { cardModel, fileExtension } = context;

    if (!OnlyOfficeApiSingleton.isAvailable) {
      return;
    }

    // для карточки операции OCR необходимо оставить предпросмотр по умолчанию
    if (Guid.equals(cardModel?.card.typeId, OcrOperationTypeId)) {
      return;
    }

    const canUseOnlyOffice =
      OnlyOfficeApiSingleton.instance.settings.previewEnabled &&
      OnlyOfficeApi.isSupportedFormat(fileExtension) &&
      !OnlyOfficeApiSingleton.instance.settings.excludedPreviewFormats.includes(fileExtension);
    if (!canUseOnlyOffice) {
      return;
    }

    context.previewerFactory = this._factory;
  }

  override async contentLoading(context: IFilePreviewLoadExtensionContext): Promise<void> {
    // OpenOffice сам загрузит контент файла
    // ставим пустой blob, для того чтобы не вызывать дефолтный загрузчик контента
    if (context.previewerFactory === this._factory) {
      context.fileContent = new File([], '');
    }
  }

  //#endregion
}
