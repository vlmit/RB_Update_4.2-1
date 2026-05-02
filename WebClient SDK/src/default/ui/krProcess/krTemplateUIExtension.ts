import { Flags } from '@tessa/core';
import { extension, inject } from '@tessa/application';
import { IKrTypesCache, IKrTypesCache$, KrComponents, KrComponentsHelper } from '@tessa/platform';
import { designTimeCard } from '../../workflow/krProcess/krUIHelper';
import {
  CardUIExtension,
  ICardUIExtensionContext,
  CardModelFlags,
  ICardModel,
  IFormWithBlocksViewModel
} from 'tessa/ui/cards';
import { Visibility } from 'tessa/platform';

@extension({ name: 'KrTemplateUIExtension' })
export class KrTemplateUIExtension extends CardUIExtension {
  //#region ctor

  constructor(@inject(IKrTypesCache$) private readonly _krTypesCache: IKrTypesCache) {
    super();
  }

  //#endregion

  //#region CardUIExtension

  async initialized(context: ICardUIExtensionContext): Promise<void> {
    let routesForm: IFormWithBlocksViewModel;
    if (
      !Flags.hasFlag(context.model.flags, CardModelFlags.EditTemplate) ||
      !this.cardIsAvailableForExtension(context.model) ||
      !(routesForm = context.model.forms.find(x => x.name === 'ApprovalProcess')!)
    ) {
      return;
    }

    for (const block of routesForm.blocks) {
      block.blockVisibility =
        block.name === 'DisclaimerBlock' ? Visibility.Visible : Visibility.Collapsed;
    }
  }

  //#endregion

  private async cardIsAvailableForExtension(model: ICardModel): Promise<boolean> {
    if (designTimeCard(model.card.typeId)) {
      return true;
    }

    const usedComponents = await KrComponentsHelper.getKrComponentsByCard(
      model.card,
      this._krTypesCache
    );
    return Flags.hasFlag(usedComponents, KrComponents.Routes);
  }
}
