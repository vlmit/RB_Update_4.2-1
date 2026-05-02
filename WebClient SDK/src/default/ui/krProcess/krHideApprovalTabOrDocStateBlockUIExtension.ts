import { Flags } from '@tessa/core';
import { extension, inject } from '@tessa/application';
import {
  Card,
  IKrTypesCache,
  IKrTypesCache$,
  KrComponents,
  KrComponentsHelper
} from '@tessa/platform';
import { tryGetKrType, designTimeCard } from '../../workflow/krProcess/krUIHelper';
import { CardUIExtension, ICardModel, ICardUIExtensionContext } from 'tessa/ui/cards';
import { DefaultFormMainViewModel } from 'tessa/ui/cards/forms';
import { Visibility } from 'tessa/platform';
import { IRuntimeBlockViewModel } from 'tessa/ui/formEditor/types';
import { TabRuntimeViewModel } from 'tessa/ui/formEditor/blocks/tab/tabRuntimeViewModel';
import { TabContainerRuntimeViewModel } from 'tessa/ui/formEditor/blocks/tab/tabContainerRuntimeViewModel';

@extension({ name: 'KrHideApprovalTabOrDocStateBlockUIExtension' })
export class KrHideApprovalTabOrDocStateBlockUIExtension extends CardUIExtension {
  //#region ctor

  constructor(@inject(IKrTypesCache$) private readonly _krTypesCache: IKrTypesCache) {
    super();
  }

  //#endregion

  //#region CardUIExtension

  async initialized(context: ICardUIExtensionContext): Promise<void> {
    // В карточке шаблона этапов и вторичного процесса ничего не трогать
    if (designTimeCard(context.card.typeId)) {
      return;
    }

    const model = context.model;
    if (!(model.mainForm instanceof DefaultFormMainViewModel) && !context.model.wysiwygForm) {
      return;
    }

    const mainForm = model.mainForm as DefaultFormMainViewModel;
    const wysiwygForm = context.model.wysiwygForm;
    const usedComponents = await KrComponentsHelper.getKrComponentsByCard(
      model.card,
      this._krTypesCache
    );
    if (!(await this.checkRoutesVisibility(usedComponents, model.card))) {
      // удаляем вкладку согласования
      !wysiwygForm?.getRootBlock()
        ? KrHideApprovalTabOrDocStateBlockUIExtension.hideApprovalTab(mainForm, model)
        : await KrHideApprovalTabOrDocStateBlockUIExtension.hideWysiwygApprovalTab(
            wysiwygForm!.getRootBlock()!
          );

      // скрываем специальный блок с состоянием документа, если нет согласования и регистрации
      if (!KrHideApprovalTabOrDocStateBlockUIExtension.hasRoutesAndRegistration(usedComponents)) {
        wysiwygForm?.getRootBlock()
          ? KrHideApprovalTabOrDocStateBlockUIExtension.hideWysiwygKrStatusBlock(
              wysiwygForm.getRootBlock()!
            )
          : KrHideApprovalTabOrDocStateBlockUIExtension.hideKrStatusBlock(model);
      }
    }
  }

  //#endregion

  //#region private

  private async checkRoutesVisibility(usedComponents: KrComponents, card: Card) {
    const krType = await tryGetKrType(this._krTypesCache, card, card.typeId);

    return Flags.hasFlag(usedComponents, KrComponents.Routes) && !krType?.hideRouteTab;
  }

  private static async hideWysiwygApprovalTab(rootBlock: IRuntimeBlockViewModel) {
    const approvalTab = rootBlock.getItem<TabRuntimeViewModel>('ApprovalProcess');
    if (!approvalTab) {
      return;
    }

    const tabContainer = approvalTab.parent as TabContainerRuntimeViewModel;

    approvalTab.isCollapsed = true;
    tabContainer.selectedTab = tabContainer.children[0] as TabRuntimeViewModel;
  }

  private static hideApprovalTab(mainForm: DefaultFormMainViewModel, model: ICardModel) {
    const approvalProcessTab = model.forms.find(x => x.name === 'ApprovalProcess');
    if (approvalProcessTab) {
      approvalProcessTab.isCollapsed = true;
      mainForm.restoreSelectedTab();
    }
  }

  private static hasRoutesAndRegistration(usedComponents: KrComponents) {
    return (
      Flags.hasFlag(usedComponents, KrComponents.Routes) &&
      Flags.hasFlag(usedComponents, KrComponents.Registration)
    );
  }

  private static hideKrStatusBlock(model: ICardModel) {
    for (const form of model.forms) {
      for (const block of form.blocks) {
        if (block.name === 'KrBlockForDocStatus') {
          block.blockVisibility = Visibility.Collapsed;
          break;
        }
      }
    }
  }

  private static hideWysiwygKrStatusBlock(rootBlock: IRuntimeBlockViewModel) {
    const krBlock = rootBlock.getItem<IRuntimeBlockViewModel>(
      item => item.alias === 'KrBlockForDocStatus'
    );
    if (krBlock) {
      krBlock.isCollapsed = true;
    }
  }

  //#endregion
}
