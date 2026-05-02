#nullable enable
using System;
using System.Collections.Generic;
using Tessa.Platform;
using Tessa.Platform.EDS;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Shared.EDS
{
    public class SigningInfo :
        StorageSerializable
    {
        #region Properties

        /// <summary>
        /// <para>
        /// Подпись в формате массива байт.
        /// </para>
        /// <para>
        /// Данное свойство заполняется через явное присваивание значения методом <c>set</c>, либо при десериализации объекта.
        /// При сериализации объекта оно добавляется в сериализуемую структуру, если оно не равно <c>null</c>, иначе будет использовано поле <see cref="SignatureBase64"/> .
        /// </para>
        /// </summary>
        public byte[]? Signature { get; set; }

        /// <summary>
        /// <para>
        /// Подпись в формате base64.
        /// </para>
        /// <para>
        /// Данное свойство может быть заполнено только через явное присваивание методом <c>set</c>.
        /// При сериализации объекта оно будет добавлено в сериализуемую структуру только в случае, если <see cref="Signature"/> равно <c>null</c>.
        /// </para>
        /// </summary>
        public string? SignatureBase64 { get; set; }

        public EDSAction EDSAction { get; set; }

        public byte[]? Certificate { get; set; }

        public SignatureType TargetSignatureType { get; set; }

        public SignatureProfile TargetSignatureProfile { get; set; }

        public DateTime SigningTime { get; set; }

        public string? DigestAlgorithm { get; set; }

        public string? EncryptionAlgorithm { get; set; }

        public string? File { get; set; }

        public Guid? VersionID{ get; set; }

        #endregion

        #region Base Overrrides

        protected override void SerializeCore(Dictionary<string, object?> storage)
        {
            storage[nameof(this.Signature)] = (object?) this.Signature ?? this.SignatureBase64;
            storage[nameof(this.EDSAction)] = Int32Boxes.Box((int) this.EDSAction);
            storage[nameof(this.Certificate)] = this.Certificate;
            storage[nameof(this.TargetSignatureType)] = Int32Boxes.Box((int) this.TargetSignatureType);
            storage[nameof(this.TargetSignatureProfile)] = Int32Boxes.Box((int) this.TargetSignatureProfile);
            storage[nameof(this.SigningTime)] = this.SigningTime;
            storage[nameof(this.DigestAlgorithm)] = this.DigestAlgorithm;
            storage[nameof(this.EncryptionAlgorithm)] = this.EncryptionAlgorithm;
            storage[nameof(this.File)] = this.File;
            storage[nameof(this.VersionID)] = this.VersionID;
        }

        protected override void DeserializeCore(Dictionary<string, object?> storage)
        {
            this.Signature = storage.TryConvertBytes(nameof(this.Signature));
            this.EDSAction = storage.ConvertEnum<EDSAction>(nameof(this.EDSAction));
            this.Certificate = storage.TryConvertBytes(nameof(this.Certificate));
            this.TargetSignatureType = storage.ConvertEnum<SignatureType>(nameof(this.TargetSignatureType));
            this.TargetSignatureProfile = storage.ConvertEnum<SignatureProfile>(nameof(this.TargetSignatureProfile));
            this.SigningTime = storage.TryConvertDateTime(nameof(this.SigningTime)) ?? DateTime.MinValue;
            this.DigestAlgorithm = storage.TryGet<string>(nameof(this.DigestAlgorithm));
            this.EncryptionAlgorithm = storage.TryGet<string>(nameof(this.EncryptionAlgorithm));
            this.File = storage.TryGet<string>(nameof(this.File));
            this.VersionID = storage.TryGet<Guid?>(nameof(this.VersionID));
        }

        #endregion
    }
}
