import {
  ICloneable,
  IStorage,
  IStorageSerializable,
  StorageSerializableContext,
  StorageSerializableObject
} from '@tessa/core';
import { SortDirection } from '@tessa/platform';

export class SortingColumnStorage
  extends StorageSerializableObject
  implements IStorageSerializable, ICloneable<SortingColumnStorage>
{
  //#region props

  alias: string;

  sortDirection: SortDirection;

  //#endregion

  //#region IStorageSerializable implementation

  protected serializeToStorageCore(
    storage: IStorage,
    context?: StorageSerializableContext | undefined
  ): void {
    super.serializeToStorageCore(storage, context);
    const sa = this.getStorageAccessor(storage, context);

    sa.setEnumAsString('SortDirection', SortDirection, this.sortDirection).setString(
      'Alias',
      this.alias
    );

    sa.removeEmptyFields();
  }

  protected deserializeFromStorageCore(
    storage: IStorage,
    context?: StorageSerializableContext | undefined
  ): void {
    super.deserializeFromStorageCore(storage, context);
    const sa = this.getStorageAccessor(storage, context);

    this.sortDirection = sa.tryGetEnumFromStringOrDefault('SortDirection', SortDirection);
    this.alias = sa.tryGetStringOrDefault('Alias');
  }

  //#endregion

  //#region ICloneable implementation

  clone(): SortingColumnStorage {
    const context: StorageSerializableContext = { clone: true };
    const storage = this.serializeToStorage({}, context);
    return new SortingColumnStorage().deserializeFromStorage(storage);
  }

  //#endregion
}
