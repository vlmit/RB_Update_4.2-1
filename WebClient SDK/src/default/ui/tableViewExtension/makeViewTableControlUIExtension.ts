import { CardUIExtension, ICardUIExtensionContext } from 'tessa/ui/cards';
import { extension } from '@tessa/application';
import { CardHelper, ICardTypeExtensionContext, CardTypeExtensionTypes } from '@tessa/platform';
import { initializeViewTable } from './makeViewTableHelper';

@extension({ name: 'MakeViewTableControlUIExtension' })
export class MakeViewTableControlUIExtension extends CardUIExtension {
  public async initializing(context: ICardUIExtensionContext): Promise<void> {
    const result = await CardHelper.executeTypeExtensions(
      CardTypeExtensionTypes.MakeViewTableControl,
      context.card,
      context.model.generalMetadata,
      this.executeInitializingAction,
      context
    );

    context.validationResult.add(result);
  }

  executeInitializingAction = async (typeContext: ICardTypeExtensionContext): Promise<void> =>
    initializeViewTable(
      typeContext.settings,
      (typeContext.externalContext as ICardUIExtensionContext).model,
      typeContext.cardTask
    );
}
