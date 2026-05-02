import {
  CardUIExtension,
  ICardUIExtensionContext,
  CardEditorOperationType,
  ICardEditorModel
} from 'tessa/ui/cards';
import { tryGetFromInfo } from 'tessa/ui';
import { DotNetType, TypedField, createTypedField } from 'tessa/platform';
import { extension } from '@tessa/application';
import { OpenCardArg } from 'tessa/ui/uiHost/common';

/**
 * Расширение на обновление виртуальной карточки состояния документа со стороны клиента.
 */
@extension()
export class KrDocStateUIExtension extends CardUIExtension {
  public shouldExecute(context: ICardUIExtensionContext): boolean {
    return context.card.typeId === 'e83a230a-f5fc-445e-9b44-7d0140ee69f6'; // KrDocStateTypeID
  }

  public initialized(context: ICardUIExtensionContext): void {
    context.model.info['.moveToTabModifierFunc'] = async (
      editor: ICardEditorModel,
      options: OpenCardArg
    ): Promise<boolean> => {
      if (!options.info) {
        options.info = {};
      }

      const fields = editor.cardModel!.card.sections.get('KrDocStateVirtual').fields;
      options.info['StateID'] = TypedField.createInt(fields.getNumber('StateID')!);
      return true;
    };
  }

  public reopening(context: ICardUIExtensionContext): void {
    const editor = context.uiContext.cardEditor;
    if (editor == null || context.model == null || context.getRequest == null) {
      return;
    }

    let stateId: number | null = null;
    if (editor.currentOperationType === CardEditorOperationType.SaveAndRefresh) {
      // идентификатор этой карточки можно менять, поэтому актуальный идентификатор при рефреше после сохранения будет в той карточке,
      // которая сохранялась (и была успешно сохранена, раз запущен рефреш)
      stateId = context.model.card.sections.get('KrDocStateVirtual').fields.get('StateID');
    }

    if (stateId == null) {
      // либо это обновление без сохранения, либо по какой-то причине поле было пустым
      stateId = tryGetFromInfo<number | null>(context.model.card.tryGetInfo() || {}, 'StateID');
    }

    if (stateId == null) {
      return;
    }

    context.getRequest.info['StateID'] = createTypedField(stateId, DotNetType.Int);
  }
}
