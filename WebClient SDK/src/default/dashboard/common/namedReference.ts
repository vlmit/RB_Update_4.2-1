import {
  DefaultValues,
  IStorage,
  StorageSerializableContext,
  StorageSerializableObject
} from '@tessa/core';

export class NamedReference extends StorageSerializableObject {
  //#region constructors

  constructor(
    public id = DefaultValues.guid,
    public name = DefaultValues.str
  ) {
    super();
  }

  //#endregion

  //#region keys

  /** @category Static Keys */
  static readonly idKey = 'ID';

  /** @category Static Keys */
  static readonly nameKey = 'Name';

  //#endregion

  //#region IStorageSerializable implementation

  protected override serializeToStorageCore(
    storage: IStorage,
    context?: StorageSerializableContext
  ): void {
    super.serializeToStorageCore(storage, context);

    const sa = this.getStorageAccessor(storage, context);
    sa.setGuid(NamedReference.idKey, this.id);
    sa.setString(NamedReference.nameKey, this.name);

    sa.removeEmptyFields();
  }

  protected override deserializeFromStorageCore(
    storage: IStorage,
    context?: StorageSerializableContext
  ): void {
    super.deserializeFromStorageCore(storage, context);

    const sa = this.getStorageAccessor(storage, context);
    this.id = sa.tryGetGuidOrDefault(NamedReference.idKey);
    this.name = sa.tryGetStringOrDefault(NamedReference.nameKey);
  }

  //#endregion
}
