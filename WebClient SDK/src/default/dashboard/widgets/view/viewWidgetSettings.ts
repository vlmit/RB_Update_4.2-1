import { IStorage, StorageSerializableContext } from '@tessa/core';
import { ViewRequestParameter } from '@tessa/platform';
import { ViewWidgetSettingsBase } from './viewWidgetSettingsBase';

export class ViewWidgetSettings extends ViewWidgetSettingsBase {
  //#region keys

  /** @category Static Keys */
  static readonly viewAliasKey = 'viewAlias';

  /** @category Static Keys */
  static readonly parametersKey = 'parameters';

  //#endregion

  //#region properties

  viewAlias = '';

  parameters: ViewRequestParameter[] | null;

  //#endregion

  //#region IStorageSerializable implementation

  protected override serializeToStorageCore(
    storage: IStorage,
    context?: StorageSerializableContext
  ): void {
    super.serializeToStorageCore(storage, context);

    const sa = this.getStorageAccessor(storage, context);
    sa.setString(ViewWidgetSettings.viewAliasKey, this.viewAlias).set(
      ViewWidgetSettings.parametersKey,
      this.parameters?.map(x => x.serializeToStorage({}, context))
    );

    sa.removeEmptyFields();
  }

  protected override deserializeFromStorageCore(
    storage: IStorage,
    context?: StorageSerializableContext
  ): void {
    super.deserializeFromStorageCore(storage, context);

    const sa = this.getStorageAccessor(storage, context);
    this.viewAlias = sa.tryGetStringOrDefault(ViewWidgetSettings.viewAliasKey);
    this.parameters =
      sa.tryGetList(ViewWidgetSettings.parametersKey, x =>
        new ViewRequestParameter().deserializeFromStorage(x, context)
      ) ?? [];
  }

  //#endregion
}
