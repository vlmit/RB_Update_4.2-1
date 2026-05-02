import { Card } from '@tessa/platform';
import { ICardEditorModel, ICardModel } from 'tessa/ui/cards/interfaces';
import { IUIContext, UIContext } from 'tessa/ui/uiContext';
import { OcrRequestStates } from './ocrTypes';
import { IStorage, StorageHelper, TypedField } from '@tessa/core';
import { OcrExpertModeKey, OcrSourceEditorKey } from './ocrConstants';

/**
 * @helper
 */
export namespace OcrHelper {
  /**
   * Выполняет проверку наличия запросов в состояниях {@link states}.
   * @param card Карточка, в которой выполняется проверка.
   * @param states Список состояний, которые необходимо проверить.
   * @returns Возвращает `true`, если найден хотя бы один запрос для хотя бы одного состояния, иначе - `false`.
   * */
  export function checkRequestsStates(card: Card, ...states: OcrRequestStates[]): boolean {
    return (
      card.sections.tryGet('OcrRequests')?.rows?.some(request => {
        const stateId = request.tryGet<number>('StateID') ?? -1;
        return states.includes(stateId);
      }) ?? false
    );
  }

  /**
   * Вспомогательный метод для получения контекста, редактора и модели карточки.
   * @param context Контекст, содержащий информацию о UI-контексте.
   * @returns Возвращает текущий UI-контекст, редактор карточки и модель карточки.
   */
  export function getContextEditorModel(context?: { uiContext: IUIContext }): {
    uiContext: IUIContext;
    editor: ICardEditorModel | null;
    model: ICardModel | null | undefined;
  } {
    const uiContext = context?.uiContext ?? UIContext.current;
    const editor = uiContext.cardEditor;
    const model = editor?.cardModel;
    return { uiContext, editor, model };
  }

  export function setExpertMode(info: IStorage, value: boolean): void {
    info[OcrExpertModeKey] = TypedField.createBoolean(value);
  }

  export function getExpertMode(info?: IStorage): boolean {
    return StorageHelper.tryGet<boolean>(info, OcrExpertModeKey) ?? false;
  }

  export function setSourceEditor(info: IStorage, value: ICardEditorModel): void {
    info[OcrSourceEditorKey] = value;
  }

  export function getSourceEditor(info: IStorage): ICardEditorModel | null {
    return StorageHelper.tryGet<ICardEditorModel>(info, OcrSourceEditorKey) ?? null;
  }
}
