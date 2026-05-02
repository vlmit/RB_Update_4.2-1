import { IStorage, StorageHelper } from '@tessa/core';
import { extension } from '@tessa/application';
import { IConditionTypesProvider, IConditionTypesProvider$ } from '@tessa/platform';
import { ApplicationExtension } from 'tessa/applicationExtension';
import { IApplicationExtensionMetadataContext } from 'tessa/applicationExtensionContext';

@extension({ name: 'ConditionTypesClientInitializationExtension' })
export class ConditionTypesClientInitializationExtension extends ApplicationExtension {
  constructor(
    @IConditionTypesProvider$() private readonly _conditionTypesProvider: IConditionTypesProvider
  ) {
    super();
  }

  override async afterMetadataReceived(
    context: IApplicationExtensionMetadataContext
  ): Promise<void> {
    const conditionTypes = StorageHelper.tryGet<IStorage[]>(
      context.response?.info,
      '.ConditionTypes'
    );
    if (!conditionTypes) {
      return;
    }

    await this._conditionTypesProvider.initialize(conditionTypes);
  }
}
