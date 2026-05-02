import { IStorage, StorageSerializableContext } from '@tessa/core';
import { ViewWidgetSettingsBase } from '../view/viewWidgetSettingsBase';

export class SearchQueryWidgetSettings extends ViewWidgetSettingsBase {
  //#region keys

  /** @category Static Keys */
  static readonly searchQueryIdKey = 'SearchQueryId';

  //#endregion

  //#region properties

  searchQueryId = '';

  //#endregion

  //#region IStorageSerializable implementation

  protected override serializeToStorageCore(
    storage: IStorage,
    context?: StorageSerializableContext
  ): void {
    super.serializeToStorageCore(storage, context);

    const sa = this.getStorageAccessor(storage, context);
    sa.setGuid(SearchQueryWidgetSettings.searchQueryIdKey, this.searchQueryId);

    sa.removeEmptyFields();
  }

  protected override deserializeFromStorageCore(
    storage: IStorage,
    context?: StorageSerializableContext
  ): void {
    super.deserializeFromStorageCore(storage, context);

    const sa = this.getStorageAccessor(storage, context);
    this.searchQueryId = sa.tryGetGuidOrDefault(SearchQueryWidgetSettings.searchQueryIdKey);
  }

  //#endregion
}
