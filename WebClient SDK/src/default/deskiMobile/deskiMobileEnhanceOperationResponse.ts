import { IStorage, StorageHelper } from '@tessa/core';
import { CertificateData } from 'tessa/files';
import { ISignedData } from 'tessa/cards/eds/edsTypes';
import { SignatureType, SignatureProfile } from 'tessa/cards/eds/edsEnums';

export type DeskiMobileMobileFileEnhanceResponse = {
  certData: CertificateData;
  signedData: ISignedData;
};

/**
 * Коллекция значений с результатами обогащения подписи, где
 * key - CacheID файла,
 * value - результат обогащения подписи.
 */
export type IDeskiMobileEnhanceOperationResponse = {
  info: Map<string, DeskiMobileMobileFileEnhanceResponse>;
};

export class DeskiMobileEnhanceOperationResponse implements IDeskiMobileEnhanceOperationResponse {
  //#region ctor

  constructor(storage: IStorage) {
    const data = StorageHelper.tryGetMap(storage, 'Items', {
      itemFactory: (_, v) => {
        const certDataStorage = StorageHelper.tryGet<IStorage>(v, 'CertData');
        if (!certDataStorage) {
          throw 'CertData not found in operation response.';
        }
        const certData = new CertificateData(certDataStorage);

        const signedDataStorage = StorageHelper.tryGet<IStorage>(v, 'SignedData');
        if (!signedDataStorage) {
          throw 'SignedData not found in operation response.';
        }
        const strType = StorageHelper.tryGet<string>(signedDataStorage, 'Type') ?? '';
        const type: SignatureType = SignatureType[strType] ?? SignatureType.None;

        const strProfile = StorageHelper.tryGet<string>(signedDataStorage, 'Profile') ?? '';
        const profile: SignatureProfile = SignatureProfile[strProfile] ?? SignatureProfile.None;

        const signedData: ISignedData = {
          signature: StorageHelper.tryGet<string>(signedDataStorage, 'Signature') ?? '',
          type,
          profile
        };

        return {
          certData,
          signedData
        } as DeskiMobileMobileFileEnhanceResponse;
      }
    });

    if (data) {
      this.info = data;
    }
  }

  //#endregion

  //#region props

  info: Map<string, DeskiMobileMobileFileEnhanceResponse> = new Map();

  //#endregion
}
