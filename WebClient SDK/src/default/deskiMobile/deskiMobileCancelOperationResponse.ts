import { IStorage, StorageHelper } from '@tessa/core';

export type IDeskiMobileCancelOperationResponse = {
  canceled: boolean;
};

export class DeskiMobileCancelOperationResponse implements IDeskiMobileCancelOperationResponse {
  //#region ctor

  constructor(storage: IStorage | boolean) {
    if (typeof storage === 'boolean') {
      this.canceled = storage;
    } else {
      const statusData = StorageHelper.tryGet<IStorage>(storage, 'StatusData');
      if (!statusData) {
        throw new Error("Can't get 'StatusData' from OperationResponse.");
      }

      const canceled = StorageHelper.tryGet<boolean>(statusData, 'Canceled') ?? false;
      this.canceled = canceled;
    }
  }

  //#endregion

  //#region props

  readonly canceled: boolean = false;

  //#endregion
}
