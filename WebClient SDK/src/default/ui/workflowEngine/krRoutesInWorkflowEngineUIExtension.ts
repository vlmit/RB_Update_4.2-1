import { Flags } from '@tessa/core';
import { extension, inject } from '@tessa/application';
import { IKrTypesCache, IKrTypesCache$, KrComponents, KrComponentsHelper } from '@tessa/platform';
import {
  CardModelFlags,
  CardUIExtension,
  IBlockViewModel,
  ICardUIExtensionContext,
  IFormWithBlocksViewModel
} from 'tessa/ui/cards';
import { Visibility } from 'tessa/platform';
import { designTimeCard, tryGetKrType } from '../../workflow/krProcess/krUIHelper';

@extension({ name: 'KrRoutesInWorkflowEngineUIExtension' })
export default class KrRoutesInWorkflowEngineUIExtension extends CardUIExtension {
  //#region ctor

  constructor(@inject(IKrTypesCache$) private readonly _krTypesCache: IKrTypesCache) {
    super();
  }

  //#endregion

  //#region CardUIExtension

  async initialized(context: ICardUIExtensionContext): Promise<void> {
    const model = context.model;
    const card = model.card;
    const cardTypeId = card.typeId;
    const routesForm: IFormWithBlocksViewModel | undefined = model.forms.find(
      x => x.name === 'ApprovalProcess'
    );
    const stageBlock: IBlockViewModel | undefined = model.blocks.get('ApprovalStagesBlock');
    const hasNotRoutes = Flags.hasNotFlag(
      await KrComponentsHelper.getKrComponentsByCard(card, this._krTypesCache),
      KrComponents.Routes
    );
    const krType = await tryGetKrType(
      this._krTypesCache,
      card,
      cardTypeId,
      context.validationResult
    );
    if (
      Flags.hasFlag(model.flags, CardModelFlags.EditTemplate) ||
      designTimeCard(cardTypeId) ||
      hasNotRoutes ||
      (krType && !krType.useRoutesInWorkflowEngine) ||
      !routesForm ||
      !stageBlock
    ) {
      return;
    }
    if (stageBlock.blockVisibility !== Visibility.Collapsed) {
      stageBlock.blockVisibility = Visibility.Collapsed;
    }
  }

  //#endregion
}
