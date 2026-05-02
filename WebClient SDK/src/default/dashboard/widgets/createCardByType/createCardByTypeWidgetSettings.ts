import { observable, runInAction } from 'mobx';
import { StorageSerializableContext, IStorage } from '@tessa/core';
import { ButtonWidgetSettings } from '../button/buttonWidgetSettings';
import { CardTypeReference } from './cardTypeReference';

export class CreateCardByTypeWidgetSettings extends ButtonWidgetSettings {
  //#region keys

  /** @category Static Keys */
  static readonly cardTypeKey = 'CardType';

  //#endregion

  //#region fields

  @observable.ref
  private _cardType: CardTypeReference | null = null;

  //#endregion

  //#region properties

  get cardType(): CardTypeReference | null {
    return this._cardType;
  }
  set cardType(value: CardTypeReference | null) {
    runInAction(() => {
      this._cardType = value;
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
      CreateCardByTypeWidgetSettings.cardTypeKey,
      this.cardType?.serializeToStorage({}, context)
    );

    sa.removeEmptyFields();
  }

  protected override deserializeFromStorageCore(
    storage: IStorage,
    context?: StorageSerializableContext
  ): void {
    super.deserializeFromStorageCore(storage, context);

    const sa = this.getStorageAccessor(storage, context);
    this.cardType = sa.tryGetObject(CreateCardByTypeWidgetSettings.cardTypeKey, storage =>
      new CardTypeReference().deserializeFromStorage(storage)
    );
  }

  //#endregion
}
