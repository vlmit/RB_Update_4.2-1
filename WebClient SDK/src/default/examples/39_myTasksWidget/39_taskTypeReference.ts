import { Guid, IStorage, StorageSerializableContext, StorageSerializableObject } from '@tessa/core';

export class TaskTypeReference extends StorageSerializableObject {
  //#region keys

  /** @category Static Keys */
  static readonly idKey = 'id';

  /** @category Static Keys */
  static readonly captionKey = 'caption';

  //#endregion

  //#region properties

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
    sa.setGuid(TaskTypeReference.idKey, this.id).setString(
      TaskTypeReference.captionKey,
      this.caption
    );
  }

  protected override deserializeFromStorageCore(
    storage: IStorage,
    context?: StorageSerializableContext
  ): void {
    super.deserializeFromStorageCore(storage, context);

    const sa = this.getStorageAccessor(storage, context);
    this.id = sa.tryGetGuidOrDefault(TaskTypeReference.idKey);
    this.caption = sa.tryGetStringOrDefault(TaskTypeReference.captionKey);
  }

  //#endregion
}
