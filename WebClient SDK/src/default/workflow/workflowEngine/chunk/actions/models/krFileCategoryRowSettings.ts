import { IStorage } from '@tessa/core';
import { undoredo } from '@tessa/platform';
import { WorkflowActionSettingsRowStorageBase } from 'tessa/ui/workflow/chunk';
import { IReadOnlyKeyValuePair } from 'tessa/ui/workflow';

/** Строка списка "Категории файлов". */
export class KrFileCategoryRowSettings extends WorkflowActionSettingsRowStorageBase<KrFileCategoryRowSettings> {
  //#region keys

  /** @category Static Keys */
  static readonly fileCategoryKey = 'FileCategory';

  /** @category Static Keys */
  static readonly fileCategoryIdKey = 'ID';

  /** @category Static Keys */
  static readonly fileCategoryNameKey = 'Name';

  //#endregion

  //#region props

  get fileCategory(): IReadOnlyKeyValuePair<string, string | null> | null {
    return this.tryGetKeyPair(
      KrFileCategoryRowSettings.fileCategoryKey,
      KrFileCategoryRowSettings.fileCategoryIdKey,
      KrFileCategoryRowSettings.fileCategoryNameKey
    );
  }
  @undoredo()
  set fileCategory(value: IReadOnlyKeyValuePair<string, string | null> | null) {
    this.setKeyPair(
      KrFileCategoryRowSettings.fileCategoryKey,
      KrFileCategoryRowSettings.fileCategoryIdKey,
      KrFileCategoryRowSettings.fileCategoryNameKey,
      value
    );
  }

  //#endregion

  //#region base overrides

  protected override currentObjectFactory(storage: IStorage): KrFileCategoryRowSettings {
    return new KrFileCategoryRowSettings(this.action, this.actionState, storage);
  }

  //#endregion
}
