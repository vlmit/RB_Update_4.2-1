import { Guid, IStorage, StorageSerializableContext, StorageSerializableObject } from '@tessa/core';

export class CardTemplate extends StorageSerializableObject {
  //#region keys

  /** @category Static Keys */
  static readonly idKey = 'id';

  /** @category Static Keys */
  static readonly captionKey = 'caption';

  //#endregion

  //#region props

  id = Guid.empty;

  caption = '';

  //#endregion

  //#region IStorageSerializable implementation

  protected override serializeToStorageCore(
    storage: IStorage,
    context?: StorageSerializableContext
  ): void {
    super.serializeToStorageCore(storage, context);

    const sa = this.getStorageAccessor(storage, context);
    sa.setGuid(CardTemplate.idKey, this.id).setString(CardTemplate.captionKey, this.caption);

    sa.removeEmptyFields();
  }

  protected override deserializeFromStorageCore(
    storage: IStorage,
    context?: StorageSerializableContext
  ): void {
    super.deserializeFromStorageCore(storage, context);

    const sa = this.getStorageAccessor(storage, context);
    this.id = sa.tryGetGuidOrDefault(CardTemplate.idKey);
    this.caption = sa.tryGetStringOrDefault(CardTemplate.captionKey);
  }

  //#endregion
}
