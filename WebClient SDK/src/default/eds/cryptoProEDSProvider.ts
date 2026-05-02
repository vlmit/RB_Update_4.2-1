import { inject, injectable } from '@tessa/application';
import {
  ICardService,
  ICardService$,
  IDbEnumerationProvider,
  IDbEnumerationProvider$
} from '@tessa/platform';
import { arrayBufferToBase64, CertificateData } from 'tessa/files';
import { EDSProvider } from 'tessa/cards/eds/edsProvider';
import { EncryptAndDigest } from 'tessa/cards/eds/edsTypes';
import Platform from 'common/platform';
import { showDialogCryptoProUnavailable } from './dialogCryptoProUnavailable';
import { EdsHelper } from 'tessa/cards/eds/edsHelpers';

@injectable()
export class CryptoProEDSProvider extends EDSProvider {
  //#region ctor

  constructor(
    @inject(ICardService$) cardService: ICardService,
    @inject(IDbEnumerationProvider$) dbEnumerationProvider: IDbEnumerationProvider
  ) {
    super(cardService, dbEnumerationProvider);
  }

  //#endregion

  //#region constants

  private static readonly minPluginVersionForKeyCaching = 2015000; // 2.0.15000

  //#endregion

  //#region EDSProvider

  override async isAvailable(showDialogError?: boolean): Promise<boolean> {
    const cadesplugin = window['cadesplugin'];
    if (!cadesplugin || Platform.isMobile()) {
      return false;
    }

    try {
      await cadesplugin;
      return true;
    } catch {
      if (showDialogError) {
        await showDialogCryptoProUnavailable();
      }
      return false;
    }
  }

  override getCerts(): Promise<CertificateData[]> {
    const cadesplugin = window['cadesplugin'];
    const CreateObject = cadesplugin.CreateObjectAsync || cadesplugin.CreateObject;

    return new Promise((resolve, reject) => {
      cadesplugin.async_spawn(
        function* (args) {
          try {
            const oStore = yield CreateObject('CAdESCOM.Store');
            yield oStore.Open(
              cadesplugin.CAPICOM_CURRENT_USER_STORE,
              cadesplugin.CAPICOM_MY_STORE,
              cadesplugin.CAPICOM_STORE_OPEN_MAXIMUM_ALLOWED
            );

            const CertificatesObj = yield oStore.Certificates;
            const Count = yield CertificatesObj.Count;
            const certs: CertificateData[] = [];
            for (let i = 1; i <= Count; i++) {
              const cert = yield CertificatesObj.Item(i);
              const subjectName = yield cert.SubjectName;
              const issuerName = yield cert.IssuerName;
              const validFrom = yield cert.ValidFromDate;
              const validTo = yield cert.ValidToDate;
              const serialNumber = yield cert.SerialNumber;
              const thumbprint = yield cert.Thumbprint;
              const certificateStr = yield cert.Export(cadesplugin.CADESCOM_ENCODE_BASE64);

              const certData = new CertificateData();
              certData.subjectName = EdsHelper.getSubjectNameAdvanced(subjectName);
              certData.subjectNameFull = subjectName;
              certData.issuerName = EdsHelper.getSubjectNameAdvanced(issuerName);
              certData.validFrom = validFrom;
              certData.validTo = validTo;
              certData.serialNumber = serialNumber;
              certData.company = CryptoProEDSProvider.getOwner(subjectName);
              certData.certificateStr = certificateStr;
              certData.thumbprint = thumbprint;

              certs.push(certData);
            }

            yield oStore.Close();

            args[0](certs);
          } catch (err) {
            args[1](cadesplugin.getLastError(err));
          }
        },
        resolve,
        reject
      );
    });
  }

  override tryCacheKey(certificate: CertificateData): Promise<Function | undefined> {
    const cadesplugin = window['cadesplugin'];
    const CreateObject = cadesplugin.CreateObjectAsync || cadesplugin.CreateObject;

    return new Promise((resolve, reject) => {
      cadesplugin.async_spawn(
        function* (args) {
          try {
            const version = yield CryptoProEDSProvider.getPluginVersion();
            if (version < CryptoProEDSProvider.minPluginVersionForKeyCaching) {
              args[0](undefined);
              return;
            }

            // certificate
            const oStore = yield CreateObject('CAdESCOM.Store');
            yield oStore.Open(
              cadesplugin.CAPICOM_CURRENT_USER_STORE,
              cadesplugin.CAPICOM_MY_STORE,
              cadesplugin.CAPICOM_STORE_OPEN_MAXIMUM_ALLOWED
            );

            const CertificatesObj = yield oStore.Certificates;

            let oCertificates;

            if (certificate.thumbprint) {
              oCertificates = yield CertificatesObj.Find(
                cadesplugin.CAPICOM_CERTIFICATE_FIND_SHA1_HASH,
                certificate.thumbprint
              );
            }

            if ((!oCertificates || (yield oCertificates.Count) === 0) && certificate.subjectName) {
              oCertificates = yield CertificatesObj.Find(
                cadesplugin.CAPICOM_CERTIFICATE_FIND_SUBJECT_NAME,
                certificate.subjectName
              );
            }

            const Count = yield oCertificates.Count;
            if (Count === 0) {
              throw "Certificate isn't found: " + args[0];
            }

            let oCertificate;

            if (Count > 1 && certificate.subjectNameFull) {
              for (let i = 0; i < Count; i++) {
                const candidate = yield oCertificates.Item(i + 1);
                const candidateSN = yield candidate.SubjectName;
                const candidateSerial = yield candidate.SerialNumber;
                if (
                  candidateSN === certificate.subjectNameFull &&
                  candidateSerial === certificate.serialNumber
                ) {
                  oCertificate = candidate;
                  break;
                }
              }
            } else {
              oCertificate = yield oCertificates.Item(1);
            }

            if (!oCertificate) {
              throw "Certificate isn't found: " + args[0];
            }

            const oPrivateKey = yield oCertificate.PrivateKey;
            yield oPrivateKey.CacheKey(true);
            args[0](() => oPrivateKey.CacheKey(false));
          } catch (err) {
            args[1](cadesplugin.getLastError(err) || err);
          }
        },
        resolve,
        reject
      );
    });
  }

  override signFile(
    certificate: CertificateData,
    fileBase64Str: string,
    encryptAndDigestList: EncryptAndDigest[]
  ): Promise<string> {
    const cadesplugin = window['cadesplugin'];
    const CreateObject = cadesplugin.CreateObjectAsync || cadesplugin.CreateObject;

    return new Promise((resolve, reject) => {
      cadesplugin.async_spawn(
        function* (args) {
          try {
            // certificate
            const oStore = yield CreateObject('CAdESCOM.Store');
            yield oStore.Open(
              cadesplugin.CAPICOM_CURRENT_USER_STORE,
              cadesplugin.CAPICOM_MY_STORE,
              cadesplugin.CAPICOM_STORE_OPEN_MAXIMUM_ALLOWED
            );

            const CertificatesObj = yield oStore.Certificates;

            let oCertificates;

            if (certificate.thumbprint) {
              oCertificates = yield CertificatesObj.Find(
                cadesplugin.CAPICOM_CERTIFICATE_FIND_SHA1_HASH,
                certificate.thumbprint
              );
            }

            if ((!oCertificates || (yield oCertificates.Count) === 0) && certificate.subjectName) {
              oCertificates = yield CertificatesObj.Find(
                cadesplugin.CAPICOM_CERTIFICATE_FIND_SUBJECT_NAME,
                certificate.subjectName
              );
            }

            const Count = yield oCertificates.Count;
            if (Count === 0) {
              throw "Certificate isn't found: " + args[0];
            }

            let oCertificate;

            if (Count > 1 && certificate.subjectNameFull) {
              for (let i = 0; i < Count; i++) {
                const candidate = yield oCertificates.Item(i + 1);
                const candidateSN = yield candidate.SubjectName;
                const candidateSerial = yield candidate.SerialNumber;
                if (
                  candidateSN === certificate.subjectNameFull &&
                  candidateSerial === certificate.serialNumber
                ) {
                  oCertificate = candidate;
                  break;
                }
              }
            } else {
              oCertificate = yield oCertificates.Item(1);
            }

            if (!oCertificate) {
              throw "Certificate isn't found: " + args[0];
            }

            // hashAlgo
            const publicKey = yield oCertificate.PublicKey();
            const keyAlgo = yield publicKey.Algorithm;
            const publicOid = yield keyAlgo.Value;
            const hashAlgoOid = CryptoProEDSProvider.getHashByPublicOid(
              publicOid,
              encryptAndDigestList
            );
            const hashAlgo = CryptoProEDSProvider.getHashAlgoByOid(hashAlgoOid);

            // hashObject
            const oHashedData = yield CreateObject('CAdESCOM.HashedData');
            yield oHashedData.propset_Algorithm(hashAlgo);
            yield oHashedData.propset_DataEncoding(cadesplugin.CADESCOM_BASE64_TO_BINARY);
            yield oHashedData.Hash(fileBase64Str);

            // rawSignature
            const oRawSignature = yield CreateObject('CAdESCOM.RawSignature');
            const sRawSignature = yield oRawSignature.SignHash(oHashedData, oCertificate);

            const sSignedMessage = arrayBufferToBase64(
              CryptoProEDSProvider.toByteArray(sRawSignature).reverse()
            );

            yield oStore.Close();

            args[2](sSignedMessage);
          } catch (err) {
            args[3](cadesplugin.getLastError(err) || err);
          }
        },
        certificate.subjectName,
        fileBase64Str,
        resolve,
        reject
      );
    });
  }

  override getHashWithAlgo(fileContent: File, hashAlgoOid: string): Promise<string> {
    const cadesplugin = window['cadesplugin'];
    const CreateObject = cadesplugin.CreateObjectAsync || cadesplugin.CreateObject;

    return new Promise((resolve, reject) => {
      cadesplugin.async_spawn(
        function* (args) {
          try {
            const hashAlgo = CryptoProEDSProvider.getHashAlgoByOid(hashAlgoOid);

            // hashObject
            const oHashedData = yield CreateObject('CAdESCOM.HashedData');
            yield oHashedData.propset_Algorithm(hashAlgo);
            yield oHashedData.propset_DataEncoding(cadesplugin.CADESCOM_BASE64_TO_BINARY);

            const chunkSize = 10 * 1024 * 1024; // 10MB

            const chunks = Math.ceil(fileContent.size / chunkSize);
            let currentChunk = 0;

            const frOnload = function (e: ProgressEvent<FileReader>) {
              cadesplugin.async_spawn(function* () {
                if (!e.target?.result) {
                  args[3]('FileReader error');
                  return;
                }

                const header = ';base64,';
                const sFileData = e.target!.result as string;
                const sBase64Data = sFileData.substring(sFileData.indexOf(header) + header.length);

                yield oHashedData.Hash(sBase64Data);

                currentChunk++;

                if (currentChunk < chunks) {
                  loadNext();
                } else {
                  const hashValue = yield oHashedData.Value;
                  args[2](arrayBufferToBase64(CryptoProEDSProvider.toByteArray(hashValue)));
                }
              });
            };

            const frOnerror = function () {
              args[3]('File load error.');
            };

            function loadNext() {
              const fileReader = new FileReader();
              fileReader.onload = frOnload;
              fileReader.onerror = frOnerror;

              const start = currentChunk * chunkSize,
                end = start + chunkSize >= fileContent.size ? fileContent.size : start + chunkSize;

              fileReader.readAsDataURL(fileContent.slice(start, end));
            }

            loadNext();
          } catch (err) {
            args[3](cadesplugin.getLastError(err) || err);
          }
        },
        fileContent,
        hashAlgoOid,
        resolve,
        reject
      );
    });
  }

  override getHash(
    certificate: CertificateData,
    fileContent: File,
    encryptAndDigestList: EncryptAndDigest[]
  ): Promise<string> {
    const cadesplugin = window['cadesplugin'];
    const CreateObject = cadesplugin.CreateObjectAsync || cadesplugin.CreateObject;
    const getHashWithAlgoFunc = this.getHashWithAlgo.bind(this);

    return new Promise((resolve, reject) => {
      cadesplugin.async_spawn(
        function* (args) {
          try {
            // certificate
            const oStore = yield CreateObject('CAdESCOM.Store');
            yield oStore.Open(
              cadesplugin.CAPICOM_CURRENT_USER_STORE,
              cadesplugin.CAPICOM_MY_STORE,
              cadesplugin.CAPICOM_STORE_OPEN_MAXIMUM_ALLOWED
            );

            const CertificatesObj = yield oStore.Certificates;

            let oCertificates;

            if (certificate.thumbprint) {
              oCertificates = yield CertificatesObj.Find(
                cadesplugin.CAPICOM_CERTIFICATE_FIND_SHA1_HASH,
                certificate.thumbprint
              );
            }

            if ((!oCertificates || (yield oCertificates.Count) === 0) && certificate.subjectName) {
              oCertificates = yield CertificatesObj.Find(
                cadesplugin.CAPICOM_CERTIFICATE_FIND_SUBJECT_NAME,
                certificate.subjectName
              );
            }

            const Count = yield oCertificates.Count;
            if (Count === 0) {
              throw "Certificate isn't found: " + args[0];
            }

            let oCertificate;

            if (Count > 1 && certificate.subjectNameFull) {
              for (let i = 0; i < Count; i++) {
                const candidate = yield oCertificates.Item(i + 1);
                const candidateSN = yield candidate.SubjectName;
                const candidateSerial = yield candidate.SerialNumber;
                if (
                  candidateSN === certificate.subjectNameFull &&
                  candidateSerial === certificate.serialNumber
                ) {
                  oCertificate = candidate;
                  break;
                }
              }
            } else {
              oCertificate = yield oCertificates.Item(1);
            }

            if (!oCertificate) {
              throw "Certificate isn't found: " + args[0];
            }

            // hashAlgo
            const publicKey = yield oCertificate.PublicKey();
            const keyAlgo = yield publicKey.Algorithm;
            const publicOid = yield keyAlgo.Value;
            const hashAlgoOid = CryptoProEDSProvider.getHashByPublicOid(
              publicOid,
              encryptAndDigestList
            );
            const hashValue = yield getHashWithAlgoFunc(fileContent, hashAlgoOid);

            args[2](hashValue);
          } catch (err) {
            args[3](cadesplugin.getLastError(err) || err);
          }
        },
        certificate.subjectName,
        fileContent,
        resolve,
        reject
      );
    });
  }

  override async checkSign(
    signature: string,
    fileBase64Str?: string | undefined,
    file?: File,
    fileHash?: string
  ): Promise<{ valid: boolean; error: string; fileHash: string }> {
    const cadesplugin = window['cadesplugin'];
    const CreateObject = cadesplugin.CreateObjectAsync || cadesplugin.CreateObject;

    const signatureAttributes = await this.getAttributesFromSignature(signature);
    const hashAlgo = CryptoProEDSProvider.getHashAlgoByOid(signatureAttributes.hashOid);
    const sRawSignature = CryptoProEDSProvider.toHexString(
      atob(signatureAttributes.signature)
        .split('')
        .map(x => x.charCodeAt(0))
        .reverse()
    ).toUpperCase();
    const sRawFileHash = signatureAttributes.hash;
    let hashValue: string | undefined = fileHash;
    if (!hashValue && file) {
      hashValue = await this.getHashWithAlgo(file, signatureAttributes.hashOid);
    }

    return new Promise((resolve, reject) => {
      cadesplugin.async_spawn(
        function* (args) {
          try {
            // hash
            if (!hashValue) {
              const oHashedData = yield CreateObject('CAdESCOM.HashedData');
              yield oHashedData.propset_Algorithm(hashAlgo);
              yield oHashedData.propset_DataEncoding(cadesplugin.CADESCOM_BASE64_TO_BINARY);
              yield oHashedData.Hash(fileBase64Str);
              hashValue = arrayBufferToBase64(
                CryptoProEDSProvider.toByteArray(yield oHashedData.Value)
              );
            }
            if (hashValue !== sRawFileHash) {
              args[2]({
                valid: false,
                error: '$UI_Signature_MessageDigestInvalid',
                fileHash: hashValue
              });
              return;
            }

            // certificate
            const oCertificate = yield CreateObject('CAdESCOM.Certificate');
            yield oCertificate.Import(signatureAttributes.certificate);

            // hash
            const oSignAttrsHashedData = yield CreateObject('CAdESCOM.HashedData');
            yield oSignAttrsHashedData.propset_Algorithm(hashAlgo);
            yield oSignAttrsHashedData.propset_DataEncoding(cadesplugin.CADESCOM_BASE64_TO_BINARY);
            yield oSignAttrsHashedData.Hash(signatureAttributes.signedAttributes);

            // verify
            const oRawSignature = yield CreateObject('CAdESCOM.RawSignature');
            yield oRawSignature.VerifyHash(oSignAttrsHashedData, oCertificate, sRawSignature);
          } catch (err) {
            try {
              // Возможно это подпись из эры до 3.4 - даем второй шанс
              const oSignedData = yield CreateObject('CAdESCOM.CadesSignedData');
              yield oSignedData.propset_ContentEncoding(cadesplugin.CADESCOM_BASE64_TO_BINARY);
              yield oSignedData.propset_Content(fileBase64Str);
              yield oSignedData.VerifyCades(signature, cadesplugin.CADESCOM_CADES_BES, true);
            } catch {
              args[2]({
                valid: false,
                error: cadesplugin.getLastError(err) || err,
                fileHash: hashValue
              });
              return;
            }
          }

          args[2]({ valid: true, fileHash: hashValue });
        },
        signature,
        fileBase64Str,
        resolve,
        reject
      );
    });
  }

  //#endregion

  //#region methods

  private static getOwner(str: string | undefined) {
    return CryptoProEDSProvider.getParameter(str, 'O');
  }

  private static getParameter(str: string | undefined, marker: string) {
    if (!str) {
      return undefined;
    }

    const strArr = str.split(/=|,|\+/).map(x => x.trim());
    if (strArr.length === 1) {
      return undefined;
    }

    const snIndex = strArr.indexOf(marker);
    if (snIndex === -1 || snIndex === strArr.length) {
      return undefined;
    }

    return strArr[snIndex + 1];
  }

  private static getHashByPublicOid(keyAlgo: string, encryptAndDigestList: EncryptAndDigest[]) {
    const pair = encryptAndDigestList.find(x => x.encryptOid === keyAlgo);
    if (pair) {
      return pair.digestOid;
    } else {
      return encryptAndDigestList.find(x => !x.encryptOid)?.digestOid || '';
    }
  }

  private static getHashAlgoByOid(hashOid: string) {
    const cadesplugin = window['cadesplugin'];
    switch (hashOid) {
      case '1.2.643.2.2.9':
        return cadesplugin.CADESCOM_HASH_ALGORITHM_CP_GOST_3411;
      case '1.2.643.7.1.1.2.2':
        return cadesplugin.CADESCOM_HASH_ALGORITHM_CP_GOST_3411_2012_256;
      case '1.2.643.7.1.1.4.1':
        return cadesplugin.CADESCOM_HASH_ALGORITHM_CP_GOST_3411_2012_256_HMAC;
      case '1.2.643.7.1.1.2.3':
        return cadesplugin.CADESCOM_HASH_ALGORITHM_CP_GOST_3411_2012_512;
      case '1.2.643.7.1.1.4.2':
        return cadesplugin.CADESCOM_HASH_ALGORITHM_CP_GOST_3411_2012_512_HMAC;
      case '1.2.643.2.2.10':
        return cadesplugin.CADESCOM_HASH_ALGORITHM_CP_GOST_3411_HMAC;
      case '1.2.840.113549.2.2':
        return cadesplugin.CADESCOM_HASH_ALGORITHM_MD2;
      case '1.2.840.113549.2.4':
        return cadesplugin.CADESCOM_HASH_ALGORITHM_MD4;
      case '1.2.840.113549.2.5':
        return cadesplugin.CADESCOM_HASH_ALGORITHM_MD5;
      case '2.16.840.1.101.3.4.2.1':
        return cadesplugin.CADESCOM_HASH_ALGORITHM_SHA_256;
      case '2.16.840.1.101.3.4.2.2':
        return cadesplugin.CADESCOM_HASH_ALGORITHM_SHA_384;
      case '2.16.840.1.101.3.4.2.3':
        return cadesplugin.CADESCOM_HASH_ALGORITHM_SHA_512;
      case '1.3.14.3.2.26':
        return cadesplugin.CADESCOM_HASH_ALGORITHM_SHA1;
      default:
        throw new Error(`Unsupported digest algorithm: (${hashOid})`);
    }
  }

  private static toByteArray(hexString: string) {
    const result: number[] = [];
    for (let i = 0; i < hexString.length; i += 2) {
      result.push(parseInt(hexString.substring(i, i + 2), 16));
    }
    return result;
  }

  private static toHexString(byteArray: number[]) {
    return byteArray.map(byte => ('0' + (byte & 0xff).toString(16)).slice(-2)).join('');
  }

  private static getPluginVersion(): Promise<number> {
    const cadesplugin = window['cadesplugin'];
    const CreateObject = cadesplugin.CreateObjectAsync || cadesplugin.CreateObject;

    return new Promise((resolve, reject) => {
      cadesplugin.async_spawn(
        function* (args) {
          try {
            const oAbout = yield CreateObject('CAdESCOM.About');
            const oVersion = yield oAbout.PluginVersion;

            const majorVersion = yield oVersion.MajorVersion;
            const minorVersion = yield oVersion.MinorVersion;
            const buildVersion = yield oVersion.BuildVersion;

            args[0](Number.parseInt(`${majorVersion}${minorVersion}${buildVersion}`, 10));
          } catch (err) {
            args[1](cadesplugin.getLastError(err) || err);
          }
        },
        resolve,
        reject
      );
    });
  }

  //#endregion
}
