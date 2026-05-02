import {
  CardUIExtension,
  ICardUIExtensionContext,
  CardToolbarAction,
  tileIsVisible
} from 'tessa/ui/cards';
import { showConfirmWithCancel } from 'tessa/ui';
import { openMarkedCard } from 'tessa/ui/tiles';
import { extension } from '@tessa/application';

@extension()
export class KrEditModeToolbarUIExtension extends CardUIExtension {
  public initialized(context: ICardUIExtensionContext): void {
    if (tileIsVisible(context.card, 'KrEditMode')) {
      const isInDialog = !!context.dialogName;

      context.toolbar.addItemIfNotExists(
        new CardToolbarAction({
          name: 'KrEditMode',
          caption: '$KrTiles_EditMode',
          captionIsVisible: isInDialog,
          icon: 'ta icon-thin-002',
          order: 11,
          toolTip: '$KrTiles_EditModeTooltip',
          command: this.openForEditing
        }),
        isInDialog
          ? {
              name: 'Alt+E',
              key: 'KeyE',
              modifiers: { alt: true }
            }
          : undefined
      );
    } else {
      context.toolbar.removeItemIfExists('KrEditMode');
    }
  }

  private openForEditing = async () => {
    await openMarkedCard(
      'kr_calculate_permissions',
      null, // Не требуем подтверждения действия, если не было изменений
      () => showConfirmWithCancel('$KrTiles_EditModeConfirmation')
    );
  };
}
