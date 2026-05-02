import { CardStoreExtension, ICardStoreExtensionContext } from 'tessa/cards/extensions';
import { extension } from '@tessa/application';

@extension()
export class RemoveTagsCardStoreExtension extends CardStoreExtension {
  async beforeRequest(context: ICardStoreExtensionContext): Promise<void> {
    if (!context.validationResult.isSuccessful) {
      return;
    }

    const info = context.request.tryGetCard()?.tryGetInfo();
    if (!info) {
      return;
    }

    delete info['TagsForCard'];
  }
}
