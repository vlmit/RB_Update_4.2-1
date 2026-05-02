import {
  CardUIExtension,
  ICardUIExtensionContext,
  CardModelFlags,
  CardModelInitializingEventArgs
} from 'tessa/ui/cards';
import { hasNotFlag } from 'tessa/platform';
import { extension } from '@tessa/application';
import { CardHelper } from '@tessa/platform';

@extension({ name: 'WfTaskSatelliteUIExtension' })
export class WfTaskSatelliteUIExtension extends CardUIExtension {
  public async initialized(context: ICardUIExtensionContext): Promise<void> {
    if (
      (context.model.inSpecialMode &&
        hasNotFlag(context.model.flags, CardModelFlags.EditTemplate) &&
        hasNotFlag(context.model.flags, CardModelFlags.ViewExported)) ||
      !context.uiContext.cardEditor
    ) {
      return;
    }

    const editor = context.uiContext.cardEditor;
    editor.cardModelInitialized.remove(WfTaskSatelliteUIExtension.onCardModelInitialized);
    if (context.model.cardType.id === CardHelper.WfTaskCardTypeID) {
      editor.cardModelInitialized.add(WfTaskSatelliteUIExtension.onCardModelInitialized);
    }
  }

  public static async onCardModelInitialized(e: CardModelInitializingEventArgs): Promise<void> {
    const sections = e.cardModel.card.tryGetSections();
    if (!sections) {
      return;
    }

    const section = sections.tryGet('WfTaskCardsVirtual');
    const docTypeTitle = !!section ? section.fields.tryGet<string>('DocTypeTitle')! : null;

    if (docTypeTitle) {
      e.workspaceInfo = docTypeTitle;
    }
  }
}
