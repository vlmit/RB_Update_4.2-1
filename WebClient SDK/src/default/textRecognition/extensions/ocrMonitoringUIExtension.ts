import { FieldType, Guid, StorageHelper, TypedField } from '@tessa/core';
import { extension, inject, ISession, ISession$ } from '@tessa/application';
import { IUIContext } from 'tessa/ui/uiContext';
import { ICardEditorModel } from 'tessa/ui/cards/interfaces';
import { CardUIExtension } from 'tessa/ui/cards/cardUIExtension';
import { ICardUIExtensionContext } from 'tessa/ui/cards/cardUIExtensionContext';
import { showViewModelDialog } from 'tessa/ui/tessaDialog/showViewModel';
import { showNotEmpty } from 'tessa/ui/tessaDialog/showNotEmpty';
import { OcrProgressDialog } from '../components/dialog/ocrProgressDialog';
import { OcrProgressDialogOptions } from '../components/dialog/ocrProgressDialogOptions';
import { OcrProgressDialogViewModel } from '../components/dialog/ocrProgressDialogViewModel';
import {
  OcrMonitoringKey,
  OperationCheckIntervalMilliseconds,
  OcrOperationTypeId
} from '../misc/ocrConstants';
import { OcrRequestStates } from '../misc/ocrTypes';
import { OcrHelper } from '../misc/ocrHelper';

/** Расширение, выполняющее отслеживание прогресса операции по распознаванию текста в файле. */
@extension({ name: 'OcrMonitoringUIExtension' })
export class OcrMonitoringUIExtension extends CardUIExtension {
  //#region constructors

  constructor(@inject(ISession$) private readonly _session: ISession) {
    super();
  }

  //#endregion

  //#region base overrides

  shouldExecute(context: ICardUIExtensionContext): boolean {
    return Guid.equals(context.card.typeId, OcrOperationTypeId);
  }

  contextInitialized(context: ICardUIExtensionContext): void {
    const { uiContext, editor, model } = OcrHelper.getContextEditorModel(context);
    if (!editor || !model) {
      return;
    }

    // проверяем, что сейчас редактор не занят обработкой активной операции
    if (editor.operationInProgress && StorageHelper.tryGet(editor.info, OcrMonitoringKey)) {
      return;
    }

    const requests = model.card.sections.tryGet('OcrRequests')?.rows;
    const executableRequest =
      // находим первый активный запрос на распознавание файла
      requests?.find(r => r.get('StateID') === OcrRequestStates.Active) ??
      // либо, если такого нет, то первый созданный
      requests?.find(r => r.get('StateID') === OcrRequestStates.Created);

    if (executableRequest) {
      // устанавливаем признак активной операции по отслеживанию прогресса
      editor.info[OcrMonitoringKey] = TypedField.trueBoolean;
      const creator = executableRequest.getString('CreatedByID');
      // создаем диалог и начинаем отслеживать прогресс
      this.monitorRequest(uiContext, editor, executableRequest.rowId, creator).finally(() => {
        // снимаем признак активной операции по отслеживанию прогресса
        StorageHelper.tryDeleteKey(editor.info, OcrMonitoringKey);
      });
    }
  }

  //#endregion

  //#region private

  private async monitorRequest(
    uiContext: IUIContext,
    editor: ICardEditorModel,
    executableRequestRowId: string,
    executableRequestCreator: string | null
  ): Promise<void> {
    // создаем вью-модель для отслеживания прогресса операции распознавания
    const progressViewModel = new OcrProgressDialogViewModel(
      executableRequestRowId,
      OperationCheckIntervalMilliseconds,
      undefined,
      Guid.equals(executableRequestCreator, this._session.user.id)
    );
    // запускаем отслеживание операции
    this.disposeList.add(progressViewModel.monitorStart());
    // отображаем диалог пользователю
    const dialogOptionResult = await showViewModelDialog<OcrProgressDialogOptions>(
      progressViewModel,
      OcrProgressDialog
    );
    // пользователь продолжил выполнение операции в фоне, поэтому закрываем карточку диалога
    if (dialogOptionResult === OcrProgressDialogOptions.ContinueInBackground) {
      await editor.close();
    }
    // пользователь выполнил запрос на отмену операции
    else if (dialogOptionResult === OcrProgressDialogOptions.Cancel) {
      // находим актуальную запись с операцией в карточке
      // и устанавливаем для неё состояние "Прерван"
      editor.cardModel?.card.sections
        .tryGet('OcrRequests')
        ?.rows.find(r => Guid.equals(r.rowId, executableRequestRowId))
        ?.set('StateID', OcrRequestStates.Interrupted, FieldType.Int);
      // сохраняем карточку с изменённой строкой
      await editor.saveCard(uiContext);
    }
    // операция была завершена системой
    else {
      // если была ошибка, то отображаем ее пользователю
      if (progressViewModel.validationResult) {
        await showNotEmpty(progressViewModel.validationResult);
      }
      // выполняем обновление карточки
      await editor.refreshCard(uiContext);
    }
  }

  //#endregion
}
