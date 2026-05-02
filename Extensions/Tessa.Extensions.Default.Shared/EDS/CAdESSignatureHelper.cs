using System;
using System.Collections.Generic;
using System.Linq;
using Tessa.Cards;
using Tessa.Platform.EDS;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Shared.EDS
{
    public static class CAdESSignatureHelper
    {
        #region Static methods

        public static void SetBesSignaturePkcs7Info(
            CardRequest request,
            byte[] certificate,
            string file,
            DateTime signingTime,
            byte[] signature,
            string digestAlgorithmOid,
            string encryptionAlgorithmOid) =>
            request.Info[CAdESSignatureKeys.SigningInfoKey] = new SigningInfo
            {
                File = file,
                EDSAction = EDSAction.GetBesSignature,
                Certificate = certificate,
                SigningTime = signingTime,
                Signature = signature,
                DigestAlgorithm = digestAlgorithmOid,
                EncryptionAlgorithm = encryptionAlgorithmOid
            }.ToSerializedDictionary();

        public static void SetBesSignaturePkcs7InfoWithSignedAttributes(
            CardRequest request,
            byte[] certificate,
            string signedAttrebutes,
            DateTime signingTime,
            byte[] signature,
            string digestAlgorithmOid,
            string encryptionAlgorithmOid) =>
            request.Info[CAdESSignatureKeys.SigningInfoKey] = new SigningInfo
            {
                File = signedAttrebutes,
                EDSAction = EDSAction.GetBesSignatureWithSignedAttributes,
                Certificate = certificate,
                SigningTime = signingTime,
                Signature = signature,
                DigestAlgorithm = digestAlgorithmOid,
                EncryptionAlgorithm = encryptionAlgorithmOid
            }.ToSerializedDictionary();

        public static void SetToBeSignedInfo(
            CardRequest request,
            byte[] certificate,
            string file,
            DateTime signingTime,
            string digestAlgorithmOid,
            string encryptionAlgorithmOid) =>
            request.Info[CAdESSignatureKeys.SigningInfoKey] = new SigningInfo
            {
                SignatureBase64 = file,
                EDSAction = EDSAction.GetToBeSigned,
                Certificate = certificate,
                SigningTime = signingTime,
                DigestAlgorithm = digestAlgorithmOid,
                EncryptionAlgorithm = encryptionAlgorithmOid
            }.ToSerializedDictionary();
        
        public static void SetToBeSignedWithHashInfo(
            CardRequest request,
            byte[] certificate,
            string file,
            DateTime signingTime,
            string digestAlgorithmOid,
            string encryptionAlgorithmOid) =>
            request.Info[CAdESSignatureKeys.SigningInfoKey] = new SigningInfo
            {
                SignatureBase64 = file,
                EDSAction = EDSAction.GetToBeSignedWithHash,
                Certificate = certificate,
                SigningTime = signingTime,
                DigestAlgorithm = digestAlgorithmOid,
                EncryptionAlgorithm = encryptionAlgorithmOid
            }.ToSerializedDictionary();

        public static void SetGetBESInfo(
            CardRequest request,
            byte[] signature) =>
            request.Info[CAdESSignatureKeys.SigningInfoKey] = new SigningInfo
            {
                Signature = signature,
                EDSAction = EDSAction.GetBesFromExtended
            }.ToSerializedDictionary();

        public static void SetGetSignedAttributesInfo(
            CardRequest request,
            byte[] signature) =>
            request.Info[CAdESSignatureKeys.SigningInfoKey] = new SigningInfo
            {
                Signature = signature,
                EDSAction = EDSAction.GetSignatureAttributesFromSignature
            }.ToSerializedDictionary();


        public static void SetSigningInfo(
            CardInfoStorageObject storage,
            SigningInfo signingInfo) =>
            storage.Info[CAdESSignatureKeys.SigningInfoKey] = signingInfo.ToSerializedDictionary();

        public static SigningInfo GetSigningInfo(CardInfoStorageObject storage) =>
            storage.Info.GetSerializedObject<SigningInfo>(CAdESSignatureKeys.SigningInfoKey);


        public static void SetSignedData(
            CardInfoStorageObject storage,
            SignedData signedData) =>
            storage.Info[CAdESSignatureKeys.SignedDataKey] = signedData.ToSerializedDictionary();

        public static SignedData GetSignedData(CardInfoStorageObject storage) =>
            storage.Info.GetSerializedObject<SignedData>(CAdESSignatureKeys.SignedDataKey);


        public static void SetSignatureAttributes(
            CardInfoStorageObject storage,
            SignatureAttributes attributes) =>
            storage.Info[CAdESSignatureKeys.SignatureAttributesKey] = attributes.ToSerializedDictionary();

        public static SignatureAttributes GetSignatureAttributes(CardResponse response) =>
            response.Info.GetSerializedObject<SignatureAttributes>(CAdESSignatureKeys.SignatureAttributesKey);


        public static void SetValidationInfo(
            CardRequest request,
            SignedData signedData) =>
            request.Info[CAdESSignatureKeys.SigningInfoKey] = new SigningInfo
            {
                Signature = signedData.Signature,
                EDSAction = EDSAction.Verify,
                TargetSignatureProfile = signedData.SignatureProfile,
                TargetSignatureType = signedData.SignatureType,
            }.ToSerializedDictionary();

        public static void SetValidationInfo(
            CardInfoStorageObject storage,
            IReadOnlyCollection<SignatureValidationInfo> signatureValidationInfos) =>
            storage.Info[CAdESSignatureKeys.SignatureValidationKey] = signatureValidationInfos.Select(s => s.ToSerializedDictionary());

        public static IReadOnlyCollection<SignatureValidationInfo> GetValidationInfo(CardResponse response) =>
            response.Info.GetSerializedList<SignatureValidationInfo>(CAdESSignatureKeys.SignatureValidationKey);

        #endregion
    }
}
