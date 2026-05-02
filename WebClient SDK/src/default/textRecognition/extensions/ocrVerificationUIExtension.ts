import { observable, reaction, runInAction } from 'mobx';
import { CardRow } from '@tessa/platform';
import { FieldType, Guid } from '@tessa/core';
import { extension } from '@tessa/application';
import { ArrayStorage } from 'tessa/platform/storage';
import { Visibility } from 'tessa/platform/visibility';
import { FileContainer } from 'tessa/files/fileContainer';
import { FieldMapStorage } from 'tessa/cards/fieldMapStorage';
import { IControlViewModel, CardUIExtension, ICardUIExtensionContext } from 'tessa/ui/cards';
import { FilePreviewViewModel } from 'tessa/ui/cards/controls/filePreviewViewModel';
import { DefaultFormMainViewModel } from 'tessa/ui/cards/forms/defaultFormMainViewModel';
import { PdfPreviewerViewModel } from 'tessa/ui/preview/pdf/pdfPreviewerViewModel';
import {
  OcrRecognizedBox,
  IOcrRecognizedBox
} from 'tessa/platform/textRecognition/entities/ocrRecognizedBox';
import { OcrRecognizedLayout } from 'tessa/platform/textRecognition/entities/ocrRecognizedLayout';
import { OcrProperty } from '../components/grid/properties/ocrProperty';
import { OcrGridViewModel } from '../components/grid/ocrGridViewModel';
import { OcrOperationTypeId } from '../misc/ocrConstants';
import { OcrRequestStates } from '../misc/ocrTypes';
import { OcrHelper } from '../misc/ocrHelper';

/**
 * Расширение  для реализации общей UI логики взаимодействия с карточкой OCR.
 * @remarks Детальное описание расширения:
 * 1. Расширение распространяется на вкладку "Верификация".
 * 2. В расширении выполняется скрытие всех вкладок карточки OCR.
 * 3. Выполняется скрытие подсказки о наличии текстового слоя в распознаваемом файле, если текст не был обнаружен.
 * 4. Модификация инструмента предпросмотра для работы с распознанным файлом:
 *   - Скрытие неиспользуемых способов отображения распознанных элементов, а также
 *     тех способов отображения, которые доступны только в экспертном режиме.
 *   - Добавление обработчика для выбора файла в качестве основного.
 *   - Добавление обработчика для выбора распознанного элемента в области предпросмотра и
 *     отображение его значения и цвета в контроле "Распознанный текст".
 *   - Выделение распознанного элемента, связанного с выделенным свойством обозревателя.
 */
@extension({ name: 'OcrVerificationUIExtension' })
export class OcrVerificationUIExtension extends CardUIExtension {
  //#region fields

  private _recognizedBoxSelectedDisposer: VoidFunction | null | undefined = null;
  private readonly _disposers: (VoidFunction | null)[] = [];

  //#endregion

  //#region base overrides

  shouldExecute(context: ICardUIExtensionContext): boolean {
    return Guid.equals(context.card.typeId, OcrOperationTypeId);
  }

  async initialized(context: ICardUIExtensionContext): Promise<void> {
    const card = context.card;
    const sections = card.sections;
    const requests = sections.get('OcrRequests').rows;
    const operations = sections.get('OcrOperations').fields;
    const resultsVirtual = sections.get('OcrResultsVirtual').fields;

    // Скрытие подсказки о наличии текстового слоя в файле
    const controls = context.model.controlsBag;
    Static.collapseTextLayerHint(operations, controls);

    // Если есть созданные или активные запросы, то дальнейшая логика расширения не выполняется
    if (OcrHelper.checkRequestsStates(card, OcrRequestStates.Active, OcrRequestStates.Created)) {
      return;
    }

    const recognizedText = controls.find(c => c.name === 'Text')!;
    const ocrGrid = controls.find(c => c.name === 'OcrGrid') as OcrGridViewModel;
    const previewer = controls.find(c => c.name === 'Preview') as FilePreviewViewModel;
    const expertMode = OcrHelper.getExpertMode(context.uiContext.cardEditor?.info);
    this.modifyPreviewer(expertMode, previewer, resultsVirtual, ocrGrid, recognizedText);

    // Теперь для каждой строки запроса выполним подписку на изменение поля
    for (const request of requests) {
      this._disposers.push(
        request.fieldChanged.add(async args => {
          // Если было установлено поле "Основной", то отображаем этот файл в предпросмотре
          if (args.fieldName === 'IsMain' && args.fieldValue) {
            await Static.setFilePreview(
              previewer,
              context.fileContainer,
              request.get('ContentFileID')
            );
          }
        })
      );
    }
  }

  async contextInitialized(context: ICardUIExtensionContext): Promise<void> {
    const card = context.card;
    const model = context.model;

    // Инициализация видимости всех вкладок в карточке
    const expertMode = OcrHelper.getExpertMode(context.uiContext.cardEditor?.info);
    const mainForm = context.model.mainFormWithTabs as DefaultFormMainViewModel;
    if (mainForm) {
      mainForm.tabsAreCollapsed = !expertMode;
      for (let index = 0; index < mainForm.tabs.length; index++) {
        index > 0 && (mainForm.tabs[index].isCollapsed = !expertMode);
      }
    }

    // Если отсутствуют созданные или активные запросы, то выполняется дальнейшая логика расширения
    if (!OcrHelper.checkRequestsStates(card, OcrRequestStates.Active, OcrRequestStates.Created)) {
      const requests = card.sections.get('OcrRequests')!.rows;
      const previewer = model.controlsBag.find(c => c.name === 'Preview') as FilePreviewViewModel;
      // Отображение файла по умолчанию в предпросмотре на вкладке "Верификация"
      await Static.setFilePreview(
        previewer,
        context.fileContainer,
        Static.getRecognizedFileId(requests)
      );
    }
  }

  finalized(): void {
    this._recognizedBoxSelectedDisposer?.();
    this._recognizedBoxSelectedDisposer = null;
    for (const disposer of this._disposers) {
      disposer?.();
    }
    this._disposers.length = 0;
  }

  //#endregion

  //#region private

  private modifyPreviewer(
    expertMode: boolean,
    previewer: FilePreviewViewModel,
    storage: FieldMapStorage,
    ocrGrid: OcrGridViewModel,
    ...controls: IControlViewModel[]
  ): void {
    const manager = previewer.fileControlManager;
    const selectedBox = observable.box<IOcrRecognizedBox | null>(null);

    // Настраиваем цвет фона контролов
    for (const control of controls) {
      control.controlStyle.add(css => {
        const background = selectedBox.get()?.color?.main || '';
        return !!background ? css({ background: `${background} !important` }) : null;
      });
    }

    this._disposers.push(
      manager.onPreviewerInitializing.add(({ previewerViewModel }) => {
        if (previewerViewModel instanceof PdfPreviewerViewModel) {
          // вызовем предыдущие методы обратного вызова для освобождения ресурсов, если они были
          this._recognizedBoxSelectedDisposer?.();
          previewerViewModel.recognitionMode = true;
          previewerViewModel.header.showTitle = false;
        }
      }),
      // переопределим обратный вызов после загрузки файла в инструмент предпросмотра
      manager.onPreviewerLoaded.add(({ previewerViewModel }) => {
        let disposer = () => {};

        if (previewerViewModel instanceof PdfPreviewerViewModel) {
          // подписываемся на выбор распознанного элемента в предпросмотре и добавляем обработчик
          // выполняем это здесь, так как только после загрузки файла будет задана recognizedCollection
          const recognizedCollection = previewerViewModel.recognizedCollection;
          const boxes = recognizedCollection?.pages.flatMap(p => p.items.flatMap(i => i.boxes));
          const boxSelectedHandler = recognizedCollection?.recognizedBoxSelected;
          this._recognizedBoxSelectedDisposer = boxSelectedHandler?.add(box => {
            runInAction(() => selectedBox.set(box));
            // Устанавливаем значение для полей
            for (const control of controls) {
              storage.systemSet(control.name!, box?.text ?? '', FieldType.String);
            }
          });
          // установим способ отображения распознанных элементов
          previewerViewModel.recognizedLayout = boxSelectedHandler
            ? OcrRecognizedLayout.Word
            : OcrRecognizedLayout.Default;
          // скрытие способов отображения с учётом результата распознавания и экспертного режима
          for (const action of previewerViewModel.ocrRecognitionList) {
            if (Static.canCollapseAction(action.name, expertMode, previewerViewModel)) {
              action.setVisibility(Visibility.Collapsed);
            }
          }
          // выделение распознанного элемента, связанного с выделенным свойством
          if (!!boxes?.length) {
            disposer = reaction(
              () => ocrGrid.control.selectedProperty,
              (property: OcrProperty) => Static.boxReferenceChecker(property.reference, boxes)
            );
          }
        }

        return disposer;
      })
    );
  }

  private static async setFilePreview(
    previewer: FilePreviewViewModel,
    fileContainer: FileContainer,
    fileId: string | null | undefined
  ): Promise<void> {
    const file = fileContainer.files.find(file => Guid.equals(file.id, fileId));
    if (file) {
      previewer.fileControlManager.reset();
      await previewer.fileControlManager.showPreview(file.lastVersion);
    }
  }

  private static getRecognizedFileId(storage: ArrayStorage<CardRow>): string | null | undefined {
    return (
      // поиск файла, отмеченного, как основной
      storage.find(r => r.get('IsMain'))?.get('ContentFileID') ??
      // поиск последнего успешно распознанного файла (сортировка выполняется в расширении на тип карточки)
      storage.find(r => r.get('StateID') === OcrRequestStates.Completed)?.get('ContentFileID')
    );
  }

  private static canCollapseAction(
    actionName: string,
    expertMode: boolean,
    previewer: PdfPreviewerViewModel
  ): boolean {
    const layout = OcrRecognizedLayout[actionName];
    return (
      layout !== OcrRecognizedLayout.Default &&
      layout !== OcrRecognizedLayout.Text &&
      layout !== OcrRecognizedLayout.Fields &&
      ((layout !== OcrRecognizedLayout.Word && !expertMode) ||
        !previewer.recognizedCollection?.pages?.some(p =>
          p.items.some(i => i.layout === layout && i.boxes.length > 0)
        ))
    );
  }

  private static collapseTextLayerHint(
    operations: FieldMapStorage,
    controls: ReadonlyArray<IControlViewModel>
  ): void {
    const textLayerHint = controls.find(c => c.name === 'TextLayerHint');
    if (textLayerHint && operations.get('FileHasText')) {
      textLayerHint.controlVisibility = Visibility.Visible;
    }
  }

  private static boxReferenceChecker(
    reference: number[] | number | null,
    boxes: OcrRecognizedBox[]
  ): void {
    if (typeof reference === 'number') {
      for (const box of boxes) {
        box.isFocused = box.id === reference;
      }
    } else if (!!reference?.length) {
      for (const box of boxes) {
        box.isFocused = reference.includes(box.id);
      }
    } else {
      for (const box of boxes) {
        box.isFocused = false;
      }
    }
  }

  //#endregion
}

const Static: typeof OcrVerificationUIExtension = OcrVerificationUIExtension;
