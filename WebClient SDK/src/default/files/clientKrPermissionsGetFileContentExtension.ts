import {
  CardGetFileContentExtension,
  ICardGetFileContentExtensionContext
} from 'tessa/cards/extensions';
import { UIContext } from 'tessa/ui';
import { ICardModel } from 'tessa/ui/cards';
import { KrToken } from 'tessa/workflow';
import { CardTemplateHelper } from 'tessa/cards';
import { extension } from '@tessa/application';

@extension()
export class ClientKrPermissionsGetFileContentExtension extends CardGetFileContentExtension {
  async beforeRequest(context: ICardGetFileContentExtensionContext): Promise<void> {
    const editor = UIContext.current.cardEditor;
    let model: ICardModel;
    let token: KrToken;

    if (
      editor &&
      (model = editor.cardModel!) &&
      (token = KrToken.tryGet(model.card.info)!) &&
      context.request.versionRowId !== CardTemplateHelper.ReplacePlaceholdersVersionRowID &&
      model.card.id === context.request.cardId
    ) {
      token.setInfo(context.request.info);
    }
  }
}
