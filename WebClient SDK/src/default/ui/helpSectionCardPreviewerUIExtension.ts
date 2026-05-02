import { CardUIExtension, ICardUIExtensionContext } from 'tessa/ui/cards';
import { extension } from '@tessa/application';

@extension({ name: 'HelpSectionCardPreviewerUIExtension' })
export class HelpSectionCardPreviewerUIExtension extends CardUIExtension {
  public initialized(context: ICardUIExtensionContext): void {
    context.model.previewManager.showInDialogByDefault = true;
  }
}
