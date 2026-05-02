import { Guid, IStorage, StorageSerializableContext, StorageSerializableObject } from '@tessa/core';

export class CardTypeReference extends StorageSerializableObject {
  //#region keys

  /** @category Static Keys */
  static readonly cardTypeIdKey = 'cardTypeId';

  /** @category Static Keys */
  static readonly cardTypeNameKey = 'cardTypeName';

  /** @category Static Keys */
  static readonly cardTypeCaptionKey = 'cardTypeCaption';

  /** @category Static Keys */
  static readonly documentTypeIdKey = 'documentTypeId';

  /** @category Static Keys */
  static readonly documentTypeTitleKey = 'documentTypeTitle';

  //#endregion

  //#region properties

  cardTypeId = Guid.empty;

  cardTypeName = '';

  cardTypeCaption = '';

  documentTypeId: string | null;

  documentTypeTitle: string | null;

  //#endregion

  //#region IStorageSerializable implementation

  protected override serializeToStorageCore(
    storage: IStorage,
    context?: StorageSerializableContext
  ): void {
    super.serializeToStorageCore(storage, context);

    const sa = this.getStorageAccessor(storage, context);
    sa.setGuid(CardTypeReference.cardTypeIdKey, this.cardTypeId)
      .setString(CardTypeReference.cardTypeNameKey, this.cardTypeName)
      .setString(CardTypeReference.cardTypeCaptionKey, this.cardTypeCaption)
      .setGuid(CardTypeReference.documentTypeIdKey, this.documentTypeId)
      .setString(CardTypeReference.documentTypeTitleKey, this.documentTypeTitle);

    sa.removeEmptyFields();
  }

  protected override deserializeFromStorageCore(
    storage: IStorage,
    context?: StorageSerializableContext
  ): void {
    super.deserializeFromStorageCore(storage, context);

    const sa = this.getStorageAccessor(storage, context);
    this.cardTypeId = sa.tryGetGuidOrDefault(CardTypeReference.cardTypeIdKey);
    this.cardTypeName = sa.tryGetStringOrDefault(CardTypeReference.cardTypeNameKey);
    this.cardTypeCaption = sa.tryGetStringOrDefault(CardTypeReference.cardTypeCaptionKey);
    this.documentTypeId = sa.tryGetGuid(CardTypeReference.documentTypeIdKey);
    this.documentTypeTitle = sa.tryGetString(CardTypeReference.documentTypeTitleKey);
  }

  //#endregion
}
