import { CardUIExtension, ICardUIExtensionContext } from 'tessa/ui/cards';
import { ForumViewModel } from 'tessa/ui/cards/controls';
import { extension } from '@tessa/application';
import { KrPermissionFlagDescriptors, KrToken } from '@tessa/platform';

@extension()
export class ForumControlUIExtension extends CardUIExtension {
  public initializing(_context: ICardUIExtensionContext): void {
    let token: KrToken | null;
    _context.model.controlInitializers.push(async control => {
      if (!(control instanceof ForumViewModel)) {
        return;
      }

      token = KrToken.tryGet(_context.card.info);
      if (!token) {
        return;
      }

      control.isAddTopicEnabled = token.hasPermission(KrPermissionFlagDescriptors.AddTopics);
      control.elevatedPermission = {
        isSuperModerator: token.hasPermission(KrPermissionFlagDescriptors.SuperModeratorMode),
        canEditAllMessages: token.hasPermission(KrPermissionFlagDescriptors.EditAllMessages)
      };
    });
  }
}
