import { implementation, IStorage, StorageHelper } from '@tessa/core';
import { DbEnumerationItemBase } from '@tessa/platform';

@implementation('KrPermissionsFileCheckRules')
export class DbKrPermissionsFileCheckRule extends DbEnumerationItemBase<number> {
  //#region ctor

  constructor(storage: IStorage) {
    super(storage);
    this.id = StorageHelper.tryGet<number>(storage, 'ID')!;
    this.name = StorageHelper.tryGet<string>(storage, 'Name')!;
  }

  //#endregion

  //#region EnumerationItemBase overrides

  readonly id: number;

  //#endregion

  //#region fields

  readonly name: string;

  //#endregion
}
