import { observable, runInAction } from 'mobx';
import { StorageSerializableContext, IStorage } from '@tessa/core';
import { ContainerDashboardWidgetSettingsBase } from 'tessa/ui/dashboard';

/** Объект с настройками виджета {@link NotesWidget}. */
export class NotesWidgetSettings extends ContainerDashboardWidgetSettingsBase {
  //#region keys

  /** @category Static Keys */
  static readonly editorBackgroundKey = 'EditorBackground';

  /** @category Static Keys */
  static readonly editorBorderKey = 'EditorBorder';

  /** @category Static Keys */
  static readonly noPaddingKey = 'NoPadding';

  //#endregion

  //#region fields

  @observable.ref
  private _editorBackground: number | null = null;

  @observable.ref
  private _editorBorder: number | null = null;

  @observable.ref
  private _noPadding = false;

  //#endregion

  //#region properties

  /** Editor background color. */
  get editorBackground(): number | null {
    return this._editorBackground;
  }
  set editorBackground(value: number | null) {
    runInAction(() => {
      this._editorBackground = value;
    });
  }

  /** Editor border color. */
  get editorBorder(): number | null {
    return this._editorBorder;
  }
  set editorBorder(value: number | null) {
    runInAction(() => {
      this._editorBorder = value;
    });
  }

  /** Without padding. */
  get noPadding(): boolean {
    return this._noPadding;
  }
  set noPadding(value: boolean) {
    runInAction(() => {
      this._noPadding = value;
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
    sa.setInt(NotesWidgetSettings.editorBackgroundKey, this.editorBackground);
    sa.setInt(NotesWidgetSettings.editorBorderKey, this.editorBorder);
    sa.setBoolean(NotesWidgetSettings.noPaddingKey, this.noPadding);

    sa.removeEmptyFields();
  }

  protected override deserializeFromStorageCore(
    storage: IStorage,
    context?: StorageSerializableContext
  ): void {
    super.deserializeFromStorageCore(storage, context);

    const sa = this.getStorageAccessor(storage, context);
    this.editorBackground = sa.tryGetInt(NotesWidgetSettings.editorBackgroundKey);
    this.editorBorder = sa.tryGetInt(NotesWidgetSettings.editorBorderKey);
    this.noPadding = sa.tryGetBooleanOrDefault(NotesWidgetSettings.noPaddingKey);
  }

  //#endregion
}
