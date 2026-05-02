import { observable, runInAction } from 'mobx';
import { IStorage, StorageSerializableContext } from '@tessa/core';
import { ButtonWidgetSettings } from '../button/buttonWidgetSettings';
import { CardTemplate } from './cardTemplate';

export class CreateCardByTemplateWidgetSettings extends ButtonWidgetSettings {
  //#region keys

  /** @category Static Keys */
  static readonly templateKey = 'Template';

  //#endregion

  //#region fields

  @observable.ref
  private _template: CardTemplate | null;

  //#endregion

  //#region properties

  get template(): CardTemplate | null {
    return this._template;
  }
  set template(value: CardTemplate | null) {
    runInAction(() => {
      this._template = value;
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
    sa.set(
      CreateCardByTemplateWidgetSettings.templateKey,
      this.template?.serializeToStorage({}, context)
    );

    sa.removeEmptyFields();
  }

  protected override deserializeFromStorageCore(
    storage: IStorage,
    context?: StorageSerializableContext
  ): void {
    super.deserializeFromStorageCore(storage, context);

    const sa = this.getStorageAccessor(storage, context);
    this.template = sa.tryGetObject(CreateCardByTemplateWidgetSettings.templateKey, storage =>
      new CardTemplate().deserializeFromStorage(storage)
    );
  }

  //#endregion
}
