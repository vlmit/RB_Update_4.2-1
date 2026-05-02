import { CardStoreExtension, ICardStoreExtensionContext } from 'tessa/cards/extensions';
import { KrToken } from 'tessa/workflow';
import { UIContext } from 'tessa/ui';
import { ICardEditorModel, ICardModel } from 'tessa/ui/cards';
import { extension } from '@tessa/application';

@extension()
export class AcquaintanceClientStoreExtension extends CardStoreExtension {
  async afterRequest(context: ICardStoreExtensionContext): Promise<void> {
    if (!context.requestIsSuccessful) {
      return;
    }

    const token = KrToken.tryGet(context.response!.info);
    const uiContext = UIContext.current;

    let editor: ICardEditorModel;
    let model: ICardModel;
    if (
      token &&
      (editor = uiContext.cardEditor!) &&
      (model = editor.cardModel!) &&
      '.SaveWithPermissionsCalc' in context.request.info &&
      context.response!.cardVersion !== model.card.version
    ) {
      token.setInfo(editor.info);
    }
  }
}
