import { Flags, IStorage, TypedField } from '@tessa/core';
import { extension, inject } from '@tessa/application';
import {
  IKrTypesCache,
  IKrTypesCache$,
  KrComponents,
  KrComponentsHelper,
  KrPermissionFlagDescriptors,
  KrToken
} from '@tessa/platform';
import { CardUIExtension, ICardUIExtensionContext, CardModelFlags } from 'tessa/ui/cards';
import { GridViewModel } from 'tessa/ui/cards/controls';
import { UIButton } from 'tessa/ui';

@extension({ name: 'KrRecalcStagesUIExtension' })
export class KrRecalcStagesUIExtension extends CardUIExtension {
  //#region ctor

  constructor(@inject(IKrTypesCache$) private readonly _krTypesCache: IKrTypesCache) {
    super();
  }

  //#endregion

  //#region CardUIExtension

  async initialized(context: ICardUIExtensionContext): Promise<void> {
    const cardModel = context.model;

    const usedComponents = await KrComponentsHelper.getKrComponentsByCard(
      cardModel.card,
      this._krTypesCache
    );
    // Выходим если нет согласования
    if (Flags.hasNotFlag(usedComponents, KrComponents.Routes)) {
      return;
    }

    const approvalStagesTable = cardModel.controls.get('ApprovalStagesTable') as GridViewModel;
    let krToken: KrToken | null = null;
    if (
      approvalStagesTable &&
      Flags.hasNotFlag(cardModel.flags, CardModelFlags.Disabled) &&
      (krToken = KrToken.tryGet(context.card.info)) &&
      krToken.hasPermission(KrPermissionFlagDescriptors.CanFullRecalcRoute)
    ) {
      const uiContext = context.uiContext;

      approvalStagesTable.leftButtons.push(
        UIButton.create({
          name: 'Recalc',
          caption: '$CardTypes_Buttons_RecalcApprovalStages',
          buttonAction: () => {
            const editor = uiContext.cardEditor;
            if (editor && !editor.operationInProgress) {
              const info: IStorage = {
                '.Recalc': TypedField.trueBoolean
              };
              editor.saveCard(uiContext, info);
            }
          },
          type: 'small',
          theme: 'transparent'
        })
      );
    }
  }

  //#endregion
}
