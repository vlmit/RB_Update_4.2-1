import { CardDeleteExtension, CardDeleteExtensionContext } from 'tessa/cards/extensions';
import { UIContext, tryGetFromInfo } from 'tessa/ui';
import { ICardModel } from 'tessa/ui/cards';
import { createTypedField, DotNetType } from 'tessa/platform';
import { extension } from '@tessa/application';

/**
 * Расширение на удаление виртуальной карточки состояния документа со стороны клиента.
 */
@extension()
export class KrDocStateClientDeleteExtension extends CardDeleteExtension {
  public shouldExecute(context: CardDeleteExtensionContext): boolean {
    return context.request.cardTypeId === 'e83a230a-f5fc-445e-9b44-7d0140ee69f6'; // KrDocStateTypeID
  }

  async beforeRequest(context: CardDeleteExtensionContext): Promise<void> {
    const cardId = context.request.cardId;
    if (!cardId) {
      return;
    }

    const editor = UIContext.current.cardEditor;
    let model: ICardModel;

    if (
      editor &&
      (model = editor.cardModel!) &&
      model.card.id === cardId &&
      model.cardType.id === 'e83a230a-f5fc-445e-9b44-7d0140ee69f6' // KrDocStateTypeID
    ) {
      const stateId = tryGetFromInfo<number | null>(model.card.tryGetInfo() || {}, 'StateID');
      if (stateId != null) {
        context.request.info['StateID'] = createTypedField(stateId, DotNetType.Int);
      }
    }
  }
}
