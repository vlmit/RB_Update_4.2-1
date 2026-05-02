import { observable, runInAction } from 'mobx';
import { IStorage, StorageSerializableContext } from '@tessa/core';
import { ButtonWidgetSettings } from '../button/buttonWidgetSettings';

/** Настройки виджета {@link NavigatorWidget}. */
export class NavigatorWidgetSettings extends ButtonWidgetSettings {
  //#region keys

  /** @category Static Keys */
  static readonly linkKey = 'Link';

  /** @category Static Keys */
  static readonly openViewInSeparateTabKey = 'OpenViewInSeparateTab';

  //#endregion

  //#region fields

  @observable.ref
  private _link: string | null = null;

  @observable.ref
  private _openViewInSeparateTab = false;

  //#endregion

  //#region properties

  /** Ссылка. */
  get link(): string | null {
    return this._link;
  }
  set link(value: string | null) {
    runInAction(() => (this._link = value));
  }

  /** Открыть представление в отдельной вкладке. */
  get openViewInSeparateTab(): boolean {
    return this._openViewInSeparateTab;
  }
  set openViewInSeparateTab(value: boolean) {
    runInAction(() => (this._openViewInSeparateTab = value));
  }

  //#endregion

  //#region IStorageSerializable

  protected override serializeToStorageCore(
    storage: IStorage,
    context?: StorageSerializableContext
  ): void {
    super.serializeToStorageCore(storage, context);

    const sa = this.getStorageAccessor(storage, context);
    sa.setString(NavigatorWidgetSettings.linkKey, this.link);
    sa.setBoolean(NavigatorWidgetSettings.openViewInSeparateTabKey, this.openViewInSeparateTab);

    sa.removeEmptyFields();
  }

  protected override deserializeFromStorageCore(
    storage: IStorage,
    context?: StorageSerializableContext
  ): void {
    super.deserializeFromStorageCore(storage, context);

    const sa = this.getStorageAccessor(storage, context);
    this.link = sa.tryGetString(NavigatorWidgetSettings.linkKey);
    this.openViewInSeparateTab = sa.tryGetBooleanOrDefault(
      NavigatorWidgetSettings.openViewInSeparateTabKey
    );
  }

  //#endregion
}
