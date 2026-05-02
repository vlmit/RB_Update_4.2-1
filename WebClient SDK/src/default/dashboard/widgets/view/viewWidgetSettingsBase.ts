import { observable, runInAction } from 'mobx';
import { IStorage, StorageSerializableContext } from '@tessa/core';
import { ContainerDashboardWidgetSettingsBase } from 'tessa/ui/dashboard';

export abstract class ViewWidgetSettingsBase extends ContainerDashboardWidgetSettingsBase {
  //#region keys

  /** @category Static Keys */
  static readonly disableHorizontalScrollKey = 'DisableHorizontalScroll';

  //#endregion

  //#region fields

  @observable.ref
  private _disableHorizontalScrollKey = false;

  //#endregion

  //#region properties

  get disableHorizontalScroll(): boolean {
    return this._disableHorizontalScrollKey;
  }
  set disableHorizontalScroll(value: boolean) {
    runInAction(() => {
      this._disableHorizontalScrollKey = value;
    });
  }

  //#endregion

  //#region IStorageSerializable implementation

  protected override serializeToStorageCore(
    storage: IStorage,
    context?: StorageSerializableContext
  ): void {
    super.serializeToStorageCore(storage, context);

    const sa = this.getStorageAccessor(storage, context);
    sa.setBoolean(ViewWidgetSettingsBase.disableHorizontalScrollKey, this.disableHorizontalScroll);

    sa.removeEmptyFields();
  }

  protected override deserializeFromStorageCore(
    storage: IStorage,
    context?: StorageSerializableContext
  ): void {
    super.deserializeFromStorageCore(storage, context);

    const sa = this.getStorageAccessor(storage, context);
    this.disableHorizontalScroll = sa.tryGetBooleanOrDefault(
      ViewWidgetSettingsBase.disableHorizontalScrollKey
    );
  }

  //#endregion
}
