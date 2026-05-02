import { implementation, IStorage, StorageHelper } from '@tessa/core';
import { DbEnumerationItemBase } from '@tessa/platform';

@implementation('KrWeActionCompletionOptions')
export class DbKrWeActionCompletionOption extends DbEnumerationItemBase<string> {
  //#region ctor

  constructor(storage: IStorage) {
    super(storage);
    this.id = StorageHelper.tryGet<string>(storage, 'ID')!;
    this.name = StorageHelper.tryGet<string>(storage, 'Name')!;
    this.caption = StorageHelper.tryGet<string>(storage, 'Caption')!;
  }

  //#endregion

  //#region EnumerationItemBase overrides

  readonly id: string;

  //#endregion

  //#region fields

  readonly name: string;
  readonly caption: string;

  //#endregion
}
