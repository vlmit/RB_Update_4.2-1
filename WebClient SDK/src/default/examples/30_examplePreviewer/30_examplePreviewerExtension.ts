import { Guid } from '@tessa/core';
import { extension, inject } from '@tessa/application';
import {
  FilePreviewExtension,
  IFilePreviewResolveExtensionContext,
  IPreviewerViewModelFactory,
  IPreviewPrinterFactory,
  IPreviewPrinterFactory$
} from 'tessa/ui/preview';
import { TestCardTypeID } from '../common';
import { ExamplePreviewerViewModel } from './30_examplePreviewerViewModel';

/**
 * Позволяет создавать кастомный превьюер и использовать его для определенного
 * типа данных в выбранной карточке.
 *
 * Результат работы расширения:
 * Для тестовой карточки "Автомобиль" создает кастомный превьюер и отображает его
 * для типа данных с расширением ".txt".
 */
@extension({ name: 'ExamplePreviewerExtension' })
export class ExamplePreviewerExtension extends FilePreviewExtension {
  //#region ctor

  constructor(
    @inject(IPreviewPrinterFactory$) private readonly _printerFactory: IPreviewPrinterFactory
  ) {
    super();
  }

  //#endregion

  //#region fields

  private _factory: IPreviewerViewModelFactory = ({ previewManager, fileVersion }) =>
    new ExamplePreviewerViewModel(previewManager, fileVersion, this._printerFactory());

  //#endregion

  //#region FilePreviewExtension

  override async resolve(context: IFilePreviewResolveExtensionContext): Promise<void> {
    const { fileExtension, cardModel } = context;
    if (!Guid.equals(cardModel?.card.typeId, TestCardTypeID) || fileExtension !== 'txt') {
      return;
    }

    context.previewerFactory = this._factory;
  }

  //#endregion
}
