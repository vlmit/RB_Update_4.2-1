import { Flags } from '@tessa/core';
import { extension, inject } from '@tessa/application';
import { IKrTypesCache, IKrTypesCache$, KrComponents, KrComponentsHelper } from '@tessa/platform';
import { CardUIExtension, ICardUIExtensionContext } from 'tessa/ui/cards';
import { DefaultFormMainViewModel } from 'tessa/ui/cards/forms';
import { tryGetKrType } from '../../workflow/krProcess/krUIHelper';

@extension({ name: 'HideForumTabUIExtension' })
export class HideForumTabUIExtension extends CardUIExtension {
  //#region ctor

  constructor(@inject(IKrTypesCache$) private readonly _krTypesCache: IKrTypesCache) {
    super();
  }

  //#endregion

  //#region CardUIExtension

  async initialized(context: ICardUIExtensionContext): Promise<void> {
    const model = context.model;
    const mainForm = model.mainForm;
    if (!(mainForm instanceof DefaultFormMainViewModel)) {
      return;
    }

    const usedComponents = await KrComponentsHelper.getKrComponentsByCard(
      model.card,
      this._krTypesCache
    );
    const krType = await tryGetKrType(this._krTypesCache, model.card, model.card.typeId);

    if (
      Flags.hasNotFlag(usedComponents, KrComponents.UseForum) ||
      (!!krType && !krType.useForum) ||
      (!!krType && krType.useForum && !krType.useDefaultDiscussionTab)
    ) {
      const forumTab = model.forms.find(x => x.name === 'Forum');
      if (forumTab) {
        forumTab.isCollapsed = true;
        mainForm.restoreSelectedTab();
      }
    }
  }

  //#endregion
}
