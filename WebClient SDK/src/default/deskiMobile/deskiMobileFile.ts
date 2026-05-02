import { StorageSerializableContext, StorageSerializableObject } from '@tessa/core';
import { IStorage } from 'tessa/platform/storage';

export class DeskiMobileFile extends StorageSerializableObject {
  //#region ctor

  constructor(storage: IStorage) {
    super();
    this.deserializeFromStorage(storage);
  }

  //#endregion

  //#region fields

  private _cacheID = '';

  private _cardID: string | null = null;

  private _fileID: string | null = null;

  private _versionRowID: string | null = null;

  private _fileName: string | null = null;

  //#endregion

  //#region storage props

  public get cacheID(): string {
    return this._cacheID;
  }

  public get cardID(): string | null {
    return this._cardID;
  }

  public get fileID(): string | null {
    return this._fileID;
  }

  public get versionRowID(): string | null {
    return this._versionRowID;
  }

  public get fileName(): string | null {
    return this._fileName;
  }

  //#endregion

  //#region IStorageSerializable

  protected override serializeToStorageCore(
    storage: IStorage,
    context?: StorageSerializableContext
  ): void {
    super.serializeToStorageCore(storage, context);
    const sa = this.getStorageAccessor(storage, context);

    sa.setString('CacheID', this.cacheID)
      .setGuid('CardID', this.cardID)
      .setGuid('FileID', this.fileID)
      .setGuid('VersionRowID', this.versionRowID)
      .setString('FileName', this.fileName);
  }

  protected override deserializeFromStorageCore(
    storage: IStorage,
    context?: StorageSerializableContext
  ): void {
    super.deserializeFromStorageCore(storage, context);
    const sa = this.getStorageAccessor(storage, context);

    this._cardID = sa.tryGetGuid('CardID');
    this._fileID = sa.tryGetGuid('FileID');
    this._versionRowID = sa.tryGetGuid('VersionRowID');
    this._fileName = sa.tryGetString('FileName') ?? '';
    this._cacheID =
      sa.tryGetString('CacheID') ?? `${this._cardID}_${this._fileID}_${this._versionRowID}`;
  }

  //#endregion
}
