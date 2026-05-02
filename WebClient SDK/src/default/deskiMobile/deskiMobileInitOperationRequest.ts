import { IStorage, StorageSerializableContext, StorageSerializableObject } from '@tessa/core';
import { DeskiMobileFile } from './deskiMobileFile';

export class DeskiMobileInitOperationRequest extends StorageSerializableObject {
  //#region ctor

  constructor(files: DeskiMobileFile[] | null = null) {
    super();

    this._files = files ?? [];
  }

  //#endregion

  //#region fields

  private _files: DeskiMobileFile[];

  //#endregion

  //#region public methods

  public addFile(...files: readonly DeskiMobileFile[]): void {
    this._files.push(...files);
  }

  //#endregion

  //#region IStorageSerializable

  protected serializeToStorageCore(
    storage: IStorage,
    context?: StorageSerializableContext | undefined
  ): void {
    super.serializeToStorageCore(storage, context);
    const sa = this.getStorageAccessor(storage, context);

    if (!context) {
      context = {};
    }

    sa.set(
      'Files',
      this._files.map(x => x.serializeToStorage({}, context))
    );
  }

  protected deserializeFromStorageCore(
    storage: IStorage,
    context?: StorageSerializableContext | undefined
  ): void {
    super.deserializeFromStorageCore(storage, context);
    const sa = this.getStorageAccessor(storage, context);

    this._files = sa.tryGetArray('Files', x => new DeskiMobileFile(x)) ?? [];
  }

  //#endregion
}
