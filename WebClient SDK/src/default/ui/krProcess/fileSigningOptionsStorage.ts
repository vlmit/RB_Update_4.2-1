import { FieldType, IStorage, StorageObject, TypedStorageArray } from '@tessa/core';

export class FileSigningOptionsStorage extends StorageObject {
  //#region ctor

  constructor(storage: IStorage = {}) {
    super(storage);

    this.init('.NoSignFileDialog', false);
    this.init('.DoNotSignFileCopies', false);
    this.init('.NoCommentDialog', false);
    this.init('SelectedFiles', null);
    this.init('HiddenFiles', null);
    this.init('.FileCategories', null);
    this.init('.HiddenFileCategories', null);
  }

  //#endregion

  //#region props

  get noSignFileDialog(): boolean {
    return this.getValue('.NoSignFileDialog', false);
  }

  get doNotSignFileCopies(): boolean {
    return this.getValue('.DoNotSignFileCopies', false);
  }

  get noCommentDialog(): boolean {
    return this.getValue('.NoCommentDialog', false);
  }

  get selectedFiles(): TypedStorageArray<FieldType.Guid> {
    return this.getArray('SelectedFiles', storage =>
      TypedStorageArray.from(storage, FieldType.Guid, { observable: false })
    );
  }

  get hiddenFiles(): TypedStorageArray<FieldType.Guid> {
    return this.getArray('HiddenFiles', storage =>
      TypedStorageArray.from(storage, FieldType.Guid, { observable: false })
    );
  }

  get fileCategories(): TypedStorageArray<FieldType.Guid> {
    return this.getArray('.FileCategories', storage =>
      TypedStorageArray.from(storage, FieldType.Guid, { observable: false })
    );
  }

  get hiddenFileCategories(): TypedStorageArray<FieldType.Guid> {
    return this.getArray('.HiddenFileCategories', storage =>
      TypedStorageArray.from(storage, FieldType.Guid, { observable: false })
    );
  }
}
