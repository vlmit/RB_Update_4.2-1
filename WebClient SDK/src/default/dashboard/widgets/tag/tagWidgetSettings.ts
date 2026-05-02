import { observable, runInAction } from 'mobx';
import { IStorage, StorageSerializableContext } from '@tessa/core';
import { DashboardWidgetSettingsBase } from 'tessa/ui/dashboard';
import { NamedReference } from '../../common/namedReference';

/** Настройки виджета {@link TagWidget}. */
export class TagWidgetSettings extends DashboardWidgetSettingsBase {
  //#region keys

  /** @category Static Keys */
  static readonly tagKey = 'Tag';

  /** @category Static Keys */
  static readonly captionKey = 'Caption';

  /** @category Static Keys */
  static readonly displayTypesKey = 'DisplayTypes';

  //#endregion

  //#region fields

  @observable.ref
  private _tag: NamedReference | null = null;

  @observable.ref
  private _caption: string | null = null;

  private _displayTypes: NamedReference[] = observable.array([], { deep: false });

  //#endregion

  //#region properties

  /** Ссылка на тег. */
  get tag(): NamedReference | null {
    return this._tag;
  }
  set tag(value: NamedReference | null) {
    runInAction(() => (this._tag = value));
  }

  /**
   * Заголовок тега.
   * @remarks Может быть не равен {@link tag.name}.
   */
  get caption(): string | null {
    return this._caption;
  }
  set caption(value: string | null) {
    runInAction(() => (this._caption = value));
  }

  /**
   * Отображаемые типы.
   * @remarks Список типов карточек/документов, отображаемых в отдельной вкладке при клике по тегу.
   */
  get displayTypes(): NamedReference[] {
    return this._displayTypes;
  }
  set displayTypes(value: NamedReference[]) {
    this._displayTypes = observable.array(value, { deep: false });
  }

  //#endregion

  //#region IStorageSerializable

  protected override serializeToStorageCore(
    storage: IStorage,
    context?: StorageSerializableContext
  ): void {
    super.serializeToStorageCore(storage, context);

    const sa = this.getStorageAccessor(storage, context);
    sa.set(TagWidgetSettings.tagKey, this.tag?.serializeToStorage({}, context));
    sa.setString(TagWidgetSettings.captionKey, this.caption);
    sa.set(
      TagWidgetSettings.displayTypesKey,
      this.displayTypes?.map(x => x.serializeToStorage({}, context))
    );

    sa.removeEmptyFields();
  }

  protected override deserializeFromStorageCore(
    storage: IStorage,
    context?: StorageSerializableContext
  ): void {
    super.deserializeFromStorageCore(storage, context);

    const sa = this.getStorageAccessor(storage, context);
    const tag = sa.tryGet<IStorage>(TagWidgetSettings.tagKey);
    this.tag = !!tag ? new NamedReference().deserializeFromStorage(tag, context) : null;
    this.caption = sa.tryGetStringOrDefault(TagWidgetSettings.captionKey);
    this.displayTypes =
      sa.tryGetList(TagWidgetSettings.displayTypesKey, x =>
        new NamedReference().deserializeFromStorage(x, context)
      ) ?? [];
  }

  //#endregion
}
