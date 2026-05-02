import { extension } from '@tessa/application';
import { UIContext } from 'tessa/ui/uiContext';
import { DoubleClickInfo } from 'tessa/ui/views/doubleClickInfo';
import { IWorkplaceViewComponent } from 'tessa/ui/views/workplaceViewComponent';
import { AdvancedCardDialogManager } from 'tessa/ui/cards/advancedCardDialogManager';
import { OpenFromActionHistoryOnDoubleClickExtension } from 'tessa/defaultExtensions/platform/cards/openFromActionHistoryOnDoubleClickExtension';
import { ApiAccessTokensHelper } from './apiAccessTokensHelper';

@extension({ name: 'ApiAccessTokensHistoryViewExtension' })
export class ApiAccessTokensHistoryViewExtension extends OpenFromActionHistoryOnDoubleClickExtension {
  //#region base overrides

  override initialize(model: IWorkplaceViewComponent): void {
    if (
      model.inSelectionMode() ||
      !ApiAccessTokensHelper.getInDialogScope(UIContext.current.info)
    ) {
      return;
    }

    const doubleClickAction = model.doubleClickAction;
    if (!doubleClickAction) {
      return;
    }

    model.doubleClickAction = async (info: DoubleClickInfo) => {
      const dialogManager = AdvancedCardDialogManager.instance;
      info.context.actionOverridings = dialogManager.createUIContextActionOverridings();
      await doubleClickAction(info);
    };
  }

  //#endregion
}
