import { reaction } from 'mobx';
import { CardRow } from '@tessa/platform';
import { Guid, TypedField } from '@tessa/core';
import { extension, localize } from '@tessa/application';
import { getTessaIcon } from 'common/utility/uiHelpers';
import { IFile } from 'tessa/files/file';
import { IUIContext } from 'tessa/ui/uiContext';
import { MenuAction } from 'tessa/ui/menuAction';
import { IPreviewerViewModel } from 'tessa/ui/preview';
import { ICardModel } from 'tessa/ui/cards';
import { CardUIExtension } from 'tessa/ui/cards/cardUIExtension';
import { PdfPreviewerViewModel } from 'tessa/ui/preview/pdf/pdfPreviewerViewModel';
import { ICardUIExtensionContext } from 'tessa/ui/cards/cardUIExtensionContext';
import { FilePreviewViewModel } from 'tessa/ui/cards/controls/filePreviewViewModel';
import { ArrayStorage } from 'tessa/platform/storage/arrayStorage';
import { OcrRecognizedLayout } from 'tessa/platform/textRecognition/entities/ocrRecognizedLayout';
import { CardTableViewControlViewModel } from '../../ui/tableViewExtension/cardTableViewControlViewModel';
import { CardTableViewRowData } from '../../ui/tableViewExtension/cardTableViewRowData';
import { OcrOperationTypeId } from '../misc/ocrConstants';
import { OcrRequestStates } from '../misc/ocrTypes';
import { OcrHelper } from '../misc/ocrHelper';
import { Visibility } from 'ui/uiEnums';

/**
 * Расширение, реализующее функционал постраничного сравнения файлов.
 * @remarks Детальное описание расширения:
 * 1. Расширение распространяется на вкладку "Сравнение файлов".
 * 2. В расширении выполняется модификация инструментов предпросмотра
 *    по аналогии с тем, как это делается на вкладке "Верификация".
 * 3. При выборе строки в таблице с запросами OCR выполняется:
 *   - Отображение файла в области предпросмотра, проассоциированного с этим запросом.
 *   - Синхронизация номера текущей страницы, выбранной в одном из инструментов предпросмотра.
 * 4. В таблице со списком запросов OCR выполняется:
 *   - Подсветка строк в зависимости от статуса запроса.
 *   - Добавление пункта контекстного меню "Сравнить с исходным файлом".
 *   - Добавление обработчика для возможности выбора только одного файла в качестве основного.
 */
@extension({ name: 'OcrCompareFilesUIExtension' })
export class OcrCompareFilesUIExtension extends CardUIExtension {
  //#region fields

  private _commonDisposers: Array<VoidFunction | null> = [];
  private _syncPageEventDisposers: Array<VoidFunction> = [];

  //#endregion

  //#region base overrides

  shouldExecute(context: ICardUIExtensionContext): boolean {
    return (
      Guid.equals(context.card.typeId, OcrOperationTypeId) &&
      !OcrHelper.checkRequestsStates(
        context.card,
        OcrRequestStates.Active,
        OcrRequestStates.Created
      )
    );
  }

  async initialized(context: ICardUIExtensionContext): Promise<void> {
    const controls = context.model.controls;
    const files1 = controls.get('FirstFiles') as CardTableViewControlViewModel;
    const files2 = controls.get('SecondFiles') as CardTableViewControlViewModel;
    const preview1 = controls.get('FirstPreview') as FilePreviewViewModel;
    const preview2 = controls.get('SecondPreview') as FilePreviewViewModel;
    if (!files1 || !files2 || !preview1 || !preview2) {
      return;
    }

    const expertMode = OcrHelper.getExpertMode(context.uiContext.cardEditor?.info);
    const { sourceFileId, sourceFile } = Static.getSourceFileInfo(context.model, context.uiContext);
    if (sourceFile) {
      // Добавление контекстного меню для сравнения распознанного файла с исходным
      Static.generateContextMenu(files1, files2, preview2, sourceFile);
      Static.generateContextMenu(files2, files1, preview1, sourceFile);
    }

    // Установка настроек для контролов предпросмотра в режиме распознавания
    this.modifyPreviewer(expertMode, preview1, sourceFileId);
    this.modifyPreviewer(expertMode, preview2, sourceFileId);

    // Обработчики для проверки наличия записи с установленным флагом "Основной"
    const requests = context.card.sections.get('OcrRequests').rows;
    for (const request of requests) {
      this._commonDisposers.push(Static.onRowCheckHandler(request, requests));
    }

    // Вспомогательная функция для отображения файла в предпросмотре
    const showInPreview = async (
      preview: FilePreviewViewModel,
      selectedRow: ReadonlyMap<string, unknown> | null
    ) => {
      const cardRow = (selectedRow as CardTableViewRowData)?.cardRow;
      if (cardRow) {
        const fileId = cardRow.get<string>('ContentFileID');
        if (fileId) {
          const file = context.fileContainer.files.find(file => Guid.equals(file.id, fileId));
          if (file) {
            await preview.fileControlManager.showPreview(file.lastVersion);
          }
        }
      }
    };

    this._commonDisposers.push(
      // При выборе строки в таблице с результатами выполняется предпросмотр файла
      reaction(
        () => files1.selectedRow,
        async row => await showInPreview(preview1, row)
      ),
      reaction(
        () => files2.selectedRow,
        async row => await showInPreview(preview2, row)
      ),
      // Если в каком-либо превью меняется вью-модель средства, необходимо заново их связать.
      reaction(
        () => preview1.fileControlManager.previewerViewModel,
        vm1 => this.setPageSyncEvents(vm1, preview2.fileControlManager.previewerViewModel)
      ),
      reaction(
        () => preview2.fileControlManager.previewerViewModel,
        vm2 => this.setPageSyncEvents(preview1.fileControlManager.previewerViewModel, vm2)
      ),
      // Подсветка строк таблицы с результатами распознавания в зависимости от состояния запроса
      Static.colorizeRowsFunc(files1),
      Static.colorizeRowsFunc(files2)
    );

    this.setPageSyncEvents(
      preview1.fileControlManager.previewerViewModel,
      preview2.fileControlManager.previewerViewModel
    );
  }

  finalized(): void {
    Static.dispose(this._commonDisposers);
    Static.dispose(this._syncPageEventDisposers);
  }

  //#endregion

  //#region private methods

  private modifyPreviewer(
    expertMode: boolean,
    previewer: FilePreviewViewModel,
    sourceFileId?: string | null
  ): void {
    const manager = previewer.fileControlManager;

    this._commonDisposers.push(
      manager.onPreviewerInitializing.add(({ fileVersion, previewerViewModel }) => {
        if (previewerViewModel instanceof PdfPreviewerViewModel) {
          // выполним проверку, что выполняется предпросмотр исходного файла
          const isSourceFile = Guid.equals(fileVersion.file.id, sourceFileId);
          previewerViewModel.recognitionMode = true;
          previewerViewModel.header.showTitle = isSourceFile;
          previewerViewModel.header.title = name =>
            isSourceFile ? localize('$UI_Controls_Preview_Headers_SourceFile') : name;
        }
      }),
      // переопределим обратный вызов после загрузки файла в инструмент предпросмотра
      manager.onPreviewerLoaded.add(({ previewerViewModel }) => {
        if (previewerViewModel instanceof PdfPreviewerViewModel) {
          // установим способ отображения распознанных элементов
          previewerViewModel.recognizedLayout = OcrRecognizedLayout.Text;
          // скрытие способов отображения с учётом результата распознавания и экспертного режима
          for (const action of previewerViewModel.ocrRecognitionList) {
            if (Static.canCollapseAction(action.name, expertMode, previewerViewModel)) {
              action.setVisibility(Visibility.Collapsed);
            }
          }
        }
      })
    );
  }

  private static onRowCheckHandler(
    request: CardRow,
    requests: ArrayStorage<CardRow>
  ): VoidFunction | null {
    return request.fieldChanged.add(args => {
      if (args.fieldName === 'IsMain' && args.fieldValue) {
        const requestRow =
          request.get('StateID') === OcrRequestStates.Completed
            ? requests.find(r => r.get('IsMain') && !Guid.equals(r.rowId, request.rowId))
            : request;
        requestRow?.set('IsMain', TypedField.falseBoolean);
      }
    });
  }

  private static colorizeRowsFunc(files: CardTableViewControlViewModel): VoidFunction | null {
    return files.table!.modifyRowActions.addWithDispose(row => {
      const stateId = (row.data as CardTableViewRowData)?.cardRow.get('StateID');
      if (stateId === OcrRequestStates.Completed) {
        row.style.backgroundColor = '#52f2704d'; // green
      } else if (stateId === OcrRequestStates.Active) {
        row.style.backgroundColor = '#d6e8654d'; // yellow green
      } else if (stateId === OcrRequestStates.Interrupted) {
        row.style.backgroundColor = '#f7a9344d'; // orange
      }
    });
  }

  private setPageSyncEvents(
    previewArea1: IPreviewerViewModel | null,
    previewArea2: IPreviewerViewModel | null
  ): void {
    Static.dispose(this._syncPageEventDisposers);

    if (
      previewArea1 &&
      previewArea2 &&
      previewArea1 instanceof PdfPreviewerViewModel &&
      previewArea2 instanceof PdfPreviewerViewModel
    ) {
      // на изменение страницы в одном превью, меняем на такую же страницу в другом превью
      this._syncPageEventDisposers.push(
        reaction(
          () => previewArea1.pageIndex,
          index => (previewArea2.pageIndex = index)
        ),
        reaction(
          () => previewArea2.pageIndex,
          index => (previewArea1.pageIndex = index)
        )
      );
    }
  }

  private static generateContextMenu(
    filesView: CardTableViewControlViewModel,
    otherView: CardTableViewControlViewModel,
    preview: FilePreviewViewModel,
    sourceFile: IFile
  ): void {
    filesView.table?.rowContextMenuGenerators?.push(ctx => {
      const stateId = (ctx.row.data as CardTableViewRowData)?.cardRow.get('StateID');
      ctx.menuActions.push(
        MenuAction.create({
          type: 'normal',
          name: 'CompareWithSourceFile',
          icon: getTessaIcon('Thin64'),
          caption: localize('$UI_Cards_ContextMenu_CompareWithSourceFile'),
          tooltip: localize('$UI_Cards_ContextMenu_CompareWithSourceFile_Tooltip'),
          isCollapsed: stateId !== OcrRequestStates.Completed,
          action: async () => {
            // сброс выделения для всех строк в таблицах
            filesView.table?.rows.forEach(r => r.selectRow(false));
            otherView.table?.rows.forEach(r => r.selectRow(false));
            // установка выделения текущей строки
            ctx.row.selectRow(true);
            // отображение файла в предпросмотре
            await preview.fileControlManager.showPreview(sourceFile.lastVersion);
          }
        })
      );
    });
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
      ((layout !== OcrRecognizedLayout.Word && !expertMode) ||
        !previewer.recognizedCollection?.pages?.some(p =>
          p.items.some(i => i.layout === layout && i.boxes.length > 0)
        ))
    );
  }

  private static getSourceFileInfo(
    model: ICardModel,
    uiContext: IUIContext
  ): { sourceFileId?: string | null; sourceFile?: IFile | null } {
    const ocrOperations = model.card.sections.get('OcrOperations').fields;
    const sourceCardId = ocrOperations.getString('CardID');
    const sourceFileId = ocrOperations.getString('FileID');
    const sourceEditor = OcrHelper.getSourceEditor(uiContext.info);
    const sourceFile = !!sourceCardId
      ? sourceEditor?.cardModel?.fileContainer.files?.find(f => Guid.equals(f.id, sourceFileId))
      : model.fileContainer.files?.find(f => Guid.equals(f.id, sourceFileId));
    return { sourceFileId, sourceFile };
  }

  private static dispose(disposers: Array<(() => void) | null>): void {
    for (const disposer of disposers) {
      disposer?.();
    }
    disposers.length = 0;
  }

  //#endregion
}

const Static: typeof OcrCompareFilesUIExtension = OcrCompareFilesUIExtension;
