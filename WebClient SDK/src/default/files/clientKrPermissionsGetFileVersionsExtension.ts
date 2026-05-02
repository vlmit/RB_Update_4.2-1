import { extension } from '@tessa/application';
import {
  CardGetFileVersionsExtension,
  ICardGetFileVersionsExtensionContext
} from 'tessa/cards/extensions';
import { UIContext } from 'tessa/ui';
import { ICardModel } from 'tessa/ui/cards';
import { KrToken } from 'tessa/workflow';

@extension()
export class ClientKrPermissionsGetFileVersionsExtension extends CardGetFileVersionsExtension {
  async beforeRequest(context: ICardGetFileVersionsExtensionContext): Promise<void> {
    const editor = UIContext.current.cardEditor;
    let model: ICardModel;
    let token: KrToken;

    if (editor && (model = editor.cardModel!) && (token = KrToken.tryGet(model.card.info)!)) {
      token.setInfo(context.request.info);
    }
  }
}
