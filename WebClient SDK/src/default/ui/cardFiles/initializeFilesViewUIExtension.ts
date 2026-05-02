import { CardUIExtension, ICardUIExtensionContext } from 'tessa/ui/cards';
import { extension } from '@tessa/application';
import { CardHelper, CardTypeExtensionTypes, ICardTypeExtensionContext } from '@tessa/platform';
import {
  processInitializedFilesView,
  processInitializingFilesView
} from './filesViewGeneratorHelper';

@extension()
export class InitializeFilesViewUIExtension extends CardUIExtension {
  private _disposes: Array<(() => void) | null> = [];

  async initializing(context: ICardUIExtensionContext): Promise<void> {
    const result = await CardHelper.executeTypeExtensions(
      CardTypeExtensionTypes.InitializeFilesView,
      context.card,
      context.model.generalMetadata,
      this.executeInitializingAction,
      context
    );

    context.validationResult.add(result);
  }

  async initialized(context: ICardUIExtensionContext): Promise<void> {
    const result = await CardHelper.executeTypeExtensions(
      CardTypeExtensionTypes.InitializeFilesView,
      context.card,
      context.model.generalMetadata,
      this.executeInitializedActionAsync,
      context
    );

    context.validationResult.add(result);
  }

  finalized(): void {
    for (const dispose of this._disposes) {
      if (dispose) {
        dispose();
      }
    }
    this._disposes.length = 0;
  }

  private executeInitializingAction = async (context: ICardTypeExtensionContext) => {
    const uiContext = context.externalContext as ICardUIExtensionContext;
    await processInitializingFilesView(uiContext.model, context);
  };
  private executeInitializedActionAsync = async (context: ICardTypeExtensionContext) => {
    const uiContext = context.externalContext as ICardUIExtensionContext;
    await processInitializedFilesView(uiContext.model, context, this._disposes);
  };
}
