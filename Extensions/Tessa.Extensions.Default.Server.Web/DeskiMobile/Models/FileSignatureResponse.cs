using System;
using System.Collections.Generic;
using Tessa.Files;
using Tessa.Platform.EDS;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.Web.DeskiMobile.Models
{
    /// <summary>
    /// Модель для хранения информации о конкретной подписи, передаваемой в мобильное приложение.
    /// </summary>
    public sealed class FileSignatureResponse : StorageSerializable
    {
        #region Properties

        /// <summary>
        /// Идентификатор подписи.
        /// </summary>
        public Guid ID { get; set; }

        /// <summary>
        /// Данные подписи в виде base64 строки.
        /// </summary>
        public string? Signature { get; set; }

        /// <summary>
        /// Получатель сертификата, указанный в файле подписи.
        /// </summary>
        public string? SubjectName { get; set; }

        /// <summary>
        /// Название компании, указанное в файле подписи.
        /// </summary>
        public string? Company { get; set; }

        /// <summary>
        /// Дата и время подписи, указанная в файле подписи.
        /// </summary>
        public DateTime SignedDate { get; set; }

        /// <summary>
        /// Тип действия, в результате которого подпись была создана.
        /// </summary>
        public FileSignatureEventType EventType { get; set; }

        /// <summary>
        /// Профиль ЭЦП, обычно для типа подписи <see cref="Tessa.Platform.EDS.SignatureType.CAdES"/>.
        /// </summary>
        public SignatureProfile SignatureProfile { get; set; }

        /// <summary>
        /// Тип подписи.
        /// </summary>
        public SignatureType SignatureType { get; set; }

        /// <summary>
        /// Имя пользователя, который зарегистрировал подпись в системе.
        /// </summary>
        public string? UserName { get; set; }

        #endregion

        #region Base Overrides

        /// <inheritdoc />
        protected override void SerializeCore(Dictionary<string, object?> storage)
        {
            storage[nameof(this.ID)] = this.ID;
            storage[nameof(this.Signature)] = this.Signature;
            storage[nameof(this.SubjectName)] = this.SubjectName;
            storage[nameof(this.Company)] = this.Company;
            storage[nameof(this.SignedDate)] = this.SignedDate.ToUniversalTime();
            storage[nameof(this.EventType)] = this.EventType.ToString();
            storage[nameof(this.SignatureProfile)] = this.SignatureProfile.ToString();
            storage[nameof(this.SignatureType)] = this.SignatureType.ToString();
            storage[nameof(this.UserName)] = this.UserName;
        }

        /// <inheritdoc />
        protected override void DeserializeCore(Dictionary<string, object?> storage)
        {
            this.ID = storage.TryConvertGuid(nameof(this.ID)) ?? Guid.Empty;
            this.Signature = storage.TryGet<string>(nameof(this.Signature));
            this.SubjectName = storage.TryGet<string>(nameof(this.SubjectName));
            this.Company = storage.TryGet<string>(nameof(this.Company));
            this.SignedDate = storage.TryConvertDateTime(nameof(this.SignedDate)) ?? DateTime.MinValue;
            this.EventType = storage.ConvertEnum<FileSignatureEventType>(nameof(this.EventType));
            this.SignatureProfile = storage.ConvertEnum<SignatureProfile>(nameof(this.SignatureProfile));
            this.SignatureType = storage.ConvertEnum<SignatureType>(nameof(this.SignatureType));
            this.UserName = storage.TryGet<string>(nameof(this.UserName));
        }

        #endregion
    }
}
