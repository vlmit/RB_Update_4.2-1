import {
  ICloneable,
  IStorage,
  IStorageSerializable,
  StorageSerializableContext,
  StorageSerializableObject,
  TypedField
} from '@tessa/core';
import { TagsPosition } from '@tessa/platform';
import { SortingColumnStorage } from './sortingColumnStorage';

export class ColumnSettingsStorage
  extends StorageSerializableObject
  implements IStorageSerializable, ICloneable<ColumnSettingsStorage>
{
  //#region props

  groupingColumn: string | null | undefined;

  tagsPosition: TagsPosition | null;

  sortingColumns: SortingColumnStorage[] | undefined;

  ordering: string[] | undefined;

  visibilities: Map<string, boolean> | undefined;

  widths: Map<string, number> | undefined;

  //#endregion

  //#region IStorageSerializable

  protected override serializeToStorageCore(
    storage: IStorage,
    context?: StorageSerializableContext
  ): void {
    super.serializeToStorageCore(storage, context);
    const sa = this.getStorageAccessor(storage, context);

    sa.setEnumAsString('TagsPosition', TagsPosition, this.tagsPosition)
      .setStringArray('Ordering', this.ordering)
      .setMap('Visibilities', this.visibilities)
      .setMap('Widths', this.widths)
      .set(
        'SortingColumns',
        this.sortingColumns?.map(s => s.serializeToStorage())
      );

    sa.removeEmptyFields();

    if (this.groupingColumn !== undefined) {
      sa.setString('GroupingColumn', this.groupingColumn);
    }
  }

  protected override deserializeFromStorageCore(
    storage: IStorage,
    context?: StorageSerializableContext
  ): void {
    super.deserializeFromStorageCore(storage, context);
    const sa = this.getStorageAccessor(storage, context);

    this.groupingColumn =
      storage['GroupingColumn'] !== undefined ? sa.tryGetString('GroupingColumn') : undefined;

    this.tagsPosition = sa.tryGetEnumFromString('TagsPosition', TagsPosition);
    this.ordering = sa.tryGetStringArray('Ordering') ?? undefined;
    this.visibilities =
      sa.tryGetMap('Visibilities', (_, tf) => TypedField.getBoolean(tf as TypedField)) ?? undefined;
    this.widths =
      sa.tryGetMap('Widths', (_, tf) => TypedField.getNumber(tf as TypedField)) ?? undefined;
    this.sortingColumns =
      sa.tryGetArray('SortingColumns', x =>
        new SortingColumnStorage().deserializeFromStorage(x, context)
      ) ?? undefined;
  }

  //#endregion

  //#region ICloneable implementation

  clone(): ColumnSettingsStorage {
    const context: StorageSerializableContext = { clone: true };
    const storage = this.serializeToStorage({}, context);
    return new ColumnSettingsStorage().deserializeFromStorage(storage);
  }

  //#endregion
}
