import { IStorage, StorageHelper } from '@tessa/core';

export type DeskiMobileSignatureValidationInfo = {
  /**
   * Идентификатор подписи файла
   */
  id: string;
  html: string;
  validationInfo: IStorage[];
};

export type DeskiMobileFileVerifyResponse = {
  signatureValidationInfo: DeskiMobileSignatureValidationInfo[];
  signatureValidationData: {
    /**
     * Требуется ли отображать диалог с результатами проверки подписи.
     */
    showValidationDialog: boolean;
  };
};

/**
 * Коллекция значений с результатами проверки подписи, где
 * key - CacheID файла,
 * value - результат проверки подписи.
 */
export type IDeskiMobileVerifyOperationResponse = {
  info: Map<string, DeskiMobileFileVerifyResponse>;
};

export class DeskiMobileVerifyOperationResponse implements IDeskiMobileVerifyOperationResponse {
  //#region ctor

  constructor(storage: IStorage) {
    const data = StorageHelper.tryGetMap(storage, 'Items', {
      itemFactory: (_, v) => {
        const signatureValidation = StorageHelper.tryGet<
          { ID: string; Html: string; ValidationInfo: IStorage[] }[]
        >(v, 'SignatureValidationInfo');
        if (!signatureValidation) {
          throw new Error('SignatureValidationInfo not found in operation response.');
        }

        const validationInfo: DeskiMobileSignatureValidationInfo[] = signatureValidation.map(
          item => ({
            id: item.ID,
            html: item.Html,
            validationInfo: item.ValidationInfo
          })
        );

        const signatureValidationDataStorage = StorageHelper.tryGet<IStorage>(
          v,
          'SignatureValidationData'
        );
        if (!signatureValidationDataStorage) {
          throw new Error('SignatureValidationData not found in operation response.');
        }

        const showValidationDialogFlag =
          StorageHelper.tryGet<boolean>(signatureValidationDataStorage, 'ShowValidationDialog') ??
          true;

        return {
          signatureValidationInfo: validationInfo,
          signatureValidationData: {
            showValidationDialog: showValidationDialogFlag
          }
        } as DeskiMobileFileVerifyResponse;
      }
    });

    if (data) {
      this.info = data;
    }
  }

  //#endregion

  //#region props

  info: Map<string, DeskiMobileFileVerifyResponse> = new Map();

  //#endregion
}
