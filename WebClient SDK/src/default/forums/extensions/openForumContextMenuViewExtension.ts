import { WorkplaceViewComponentExtension } from 'tessa/ui/views/extensions';
import { IWorkplaceViewComponent, IViewContextMenuContext, IViewContext } from 'tessa/ui/views';
import { MenuAction } from 'tessa/ui';
import {
  ForumChangeParticipants,
  ForumRemoveParticipants,
  getForumOperationContextFromViewContext
} from 'tessa/ui/cards';
import { extension } from '@tessa/application';

@extension()
export class OpenForumContextMenuViewExtension extends WorkplaceViewComponentExtension {
  //#region CardUIExtension

  getExtensionName(): string {
    return 'Tessa.Extensions.Default.Client.Forums.OpenForumContextMenuViewExtension';
  }

  initialize(model: IWorkplaceViewComponent): void {
    model.contextMenuGenerators.push(this.getParticipantsMenuAction());
  }

  //#endregion

  //#region private methods

  private getParticipantsMenuAction(): (ctx: IViewContextMenuContext) => void {
    return c => {
      const { viewContext, rowsInAction } = c;
      if (!rowsInAction?.length) {
        return;
      }

      c.menuActions.push(
        new MenuAction('ChangeParticipant', '$Forum_MenuAction_ChangeParticipants', null, () =>
          c.uiContextExecutor(() =>
            OpenForumContextMenuViewExtension.changeParticipantsCommand(viewContext)
          )
        ),
        new MenuAction('RemoveParticipants', '$Forum_MenuAction_RemoveParticipants', null, () =>
          c.uiContextExecutor(() =>
            OpenForumContextMenuViewExtension.removeParticipantsCommand(viewContext)
          )
        )
      );
    };
  }

  private static changeParticipantsCommand(context: IViewContext): Promise<void> {
    const operationContext = getForumOperationContextFromViewContext(context);
    const operation = new ForumChangeParticipants(operationContext);
    return operation.startAsync(context);
  }

  private static removeParticipantsCommand(context: IViewContext): Promise<void> {
    const operationContext = getForumOperationContextFromViewContext(context);
    const operation = new ForumRemoveParticipants(operationContext);
    return operation.startAsync(context);
  }

  //#endregion
}
