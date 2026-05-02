import { FormUIExtension, IFormUIExtensionContext } from 'tessa/ui';
import { extension } from '@tessa/application';
import { CardHelper, ICardTypeExtensionContext, CardTypeExtensionTypes } from '@tessa/platform';
import { initializeViewTable } from './makeViewTableHelper';

@extension({ name: 'MakeViewTableControlFormUIExtension' })
export class MakeViewTableControlFormUIExtension extends FormUIExtension {
  override async initializing(context: IFormUIExtensionContext): Promise<void> {
    const result = await CardHelper.executeTypeExtensions(
      CardTypeExtensionTypes.MakeViewTableControl,
      context.model.card,
      context.model.generalMetadata,
      this.executeInitializingAction,
      context
    );

    context.validationResult.add(result);
  }

  executeInitializingAction = async (typeContext: ICardTypeExtensionContext): Promise<void> =>
    initializeViewTable(
      typeContext.settings,
      (typeContext.externalContext as IFormUIExtensionContext).model,
      typeContext.cardTask
    );
}
