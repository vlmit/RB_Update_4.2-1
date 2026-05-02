import { Flags } from '@tessa/core';
import { extension, inject } from '@tessa/application';
import { IKrTypesCache, IKrTypesCache$, KrComponents, KrComponentsHelper } from '@tessa/platform';
import {
  CardUIExtension,
  ICardUIExtensionContext,
  CardModelFlags,
  CardModelInitializingEventArgs
} from 'tessa/ui/cards';

@extension({ name: 'KrDocumentWorkspaceInfoUIExtension' })
export class KrDocumentWorkspaceInfoUIExtension extends CardUIExtension {
  //#region ctor

  constructor(@inject(IKrTypesCache$) private readonly _krTypesCache: IKrTypesCache) {
    super();
  }

  //#endregion

  //#region CardUIExtension

  async initialized(context: ICardUIExtensionContext): Promise<void> {
    const editor = context.uiContext.cardEditor;

    if (
      !editor ||
      (context.model.inSpecialMode &&
        Flags.hasNotFlag(context.model.flags, CardModelFlags.EditTemplate) &&
        Flags.hasNotFlag(context.model.flags, CardModelFlags.ViewExported))
    ) {
      return;
    }

    editor.cardModelInitialized.remove(KrDocumentWorkspaceInfoUIExtension.onCardModelInitialized);

    const usedComponents = await KrComponentsHelper.getKrComponents(
      context.model.cardType.id!,
      null,
      this._krTypesCache
    );
    if (Flags.hasFlag(usedComponents, KrComponents.DocTypes)) {
      editor.cardModelInitialized.add(KrDocumentWorkspaceInfoUIExtension.onCardModelInitialized);
    }
  }

  //#endregion

  //#region methods

  private static async onCardModelInitialized(e: CardModelInitializingEventArgs) {
    const sections = e.cardModel.card.tryGetSections();
    if (!sections) {
      return;
    }

    const section = sections.tryGet('DocumentCommonInfo');
    if (!section) {
      return;
    }

    const docTypeTitle = section.fields.tryGetString('DocTypeTitle');
    if (docTypeTitle) {
      e.workspaceInfo = docTypeTitle;
    }
  }

  //#endregion
}
