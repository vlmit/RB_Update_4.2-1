import { extension } from '@tessa/application';
import { CardStoreMode, IAvatarManager, IAvatarManager$ } from '@tessa/platform';
import { CardUIExtension, ICardUIExtensionContext } from 'tessa/ui/cards';
import { WorkspaceStorage } from 'tessa/workspaceStorage';
import { AvatarViewModel } from 'ui/avatar/avatarViewModel';
import { UserAvatarDataSource } from 'ui/avatar/userAvatarDataSource';

@extension({ name: 'PersonalRoleAvatarUIExtension' })
export class PersonalRoleAvatarUIExtension extends CardUIExtension {
  constructor(@IAvatarManager$() readonly _avatarManager: IAvatarManager) {
    super();
  }

  async contextInitialized(context: ICardUIExtensionContext): Promise<void> {
    const cardEditor = context.uiContext.cardEditor;
    if (!cardEditor) {
      return;
    }

    const card = context.card;
    const cardIsNew = card.storeMode === CardStoreMode.Insert;
    if (cardIsNew) {
      return;
    }

    const cardWorkspace = Array.from(WorkspaceStorage.instance.cards.values()).find(
      x => x.editor === cardEditor
    );
    if (!cardWorkspace) {
      return;
    }

    const mainSection = card.sections.tryGet('PersonalRoles');

    const avatarDataSource = new UserAvatarDataSource(
      {
        id: card.id,
        firstName: mainSection?.fields.tryGetString('FirstName'),
        lastName: mainSection?.fields.tryGetString('LastName')
      },
      this._avatarManager
    );
    avatarDataSource.trackLinkChanges();
    // дизпозится при закрытии вкладки
    const avatar = new AvatarViewModel(avatarDataSource, 'semi-md');
    await avatar.initialize();
    cardWorkspace.setIconProvider(avatar);
  }
}
