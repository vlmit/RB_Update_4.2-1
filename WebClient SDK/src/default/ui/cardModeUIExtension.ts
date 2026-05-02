import { extension } from '@tessa/application';
import { CardUIExtension } from 'tessa/ui/cards/cardUIExtension';
import { ICardUIExtensionContext } from 'tessa/ui/cards/cardUIExtensionContext';

/*
 * Расширение отображает карточку в компактном режиме, если
 * в карточке одновременно нет блоков предпросмотра и заданий.
 */
@extension()
export class CardModeUIExtension extends CardUIExtension {
  initialized(context: ICardUIExtensionContext): void {
    const { cardEditor } = context.uiContext;
    if (!cardEditor) {
      return;
    }

    cardEditor.className.add('compact', () => {
      if (!cardEditor.cardModel) {
        return false;
      }

      return (
        cardEditor.cardMode === 'compact' &&
        !(
          // TODO проверять previewArea, а не previewManager
          (
            !cardEditor.cardModel.previewManager.showInDialogByDefault &&
            context.card.tasks.length > 0
          )
        )
      );
    });
  }
}
