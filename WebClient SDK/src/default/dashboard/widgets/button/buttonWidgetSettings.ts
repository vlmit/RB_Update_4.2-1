import { observable, runInAction } from 'mobx';
import { IStorage, StorageSerializableContext } from '@tessa/core';
import { DashboardWidgetSettingsBase } from 'tessa/ui/dashboard';

export class ButtonWidgetSettings extends DashboardWidgetSettingsBase {
  //#region keys

  /** @category Static Keys */
  static readonly colorKey = 'Color';

  /** @category Static Keys */
  static readonly captionKey = 'Caption';

  /** @category Static Keys */
  static readonly captionHiddenKey = 'CaptionHidden';

  /** @category Static Keys */
  static readonly captionColorKey = 'CaptionColor';

  /** @category Static Keys */
  static readonly iconKey = 'Icon';

  //#endregion

  //#region fields

  @observable.ref
  private _color: number | null = null;

  @observable.ref
  private _caption: string | null = null;

  @observable.ref
  private _captionColor: number | null = null;

  @observable.ref
  private _captionHidden = false;

  @observable.ref
  private _icon: string | null = null;

  //#endregion

  //#region properties

  get color(): number | null {
    return this._color;
  }
  set color(value: number | null) {
    runInAction(() => {
      this._color = value;
    });
  }

  get caption(): string | null {
    return this._caption;
  }
  set caption(value: string | null) {
    runInAction(() => {
      this._caption = value;
    });
  }

  get captionHidden(): boolean {
    return this._captionHidden;
  }
  set captionHidden(value: boolean) {
    runInAction(() => {
      this._captionHidden = value;
    });
  }

  get captionColor(): number | null {
    return this._captionColor;
  }
  set captionColor(value: number | null) {
    runInAction(() => {
      this._captionColor = value;
    });
  }

  get icon(): string | null {
    return this._icon;
  }
  set icon(value: string | null) {
    runInAction(() => {
      this._icon = value;
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
    sa.setBoolean(ButtonWidgetSettings.captionHiddenKey, this.captionHidden)
      .setString(ButtonWidgetSettings.captionKey, this.caption)
      .setInt(ButtonWidgetSettings.captionColorKey, this.captionColor)
      .setInt(ButtonWidgetSettings.colorKey, this.color)
      .setString(ButtonWidgetSettings.iconKey, this.icon);

    sa.removeEmptyFields();
  }

  protected override deserializeFromStorageCore(
    storage: IStorage,
    context?: StorageSerializableContext
  ): void {
    super.deserializeFromStorageCore(storage, context);

    const sa = this.getStorageAccessor(storage, context);
    this.captionHidden = sa.tryGetBooleanOrDefault(ButtonWidgetSettings.captionHiddenKey);
    this.caption = sa.tryGetString(ButtonWidgetSettings.captionKey);
    this.captionColor = sa.tryGetInt(ButtonWidgetSettings.captionColorKey);
    this.color = sa.tryGetInt(ButtonWidgetSettings.colorKey);
    this.icon = sa.tryGetString(ButtonWidgetSettings.iconKey);
  }

  //#endregion
}
