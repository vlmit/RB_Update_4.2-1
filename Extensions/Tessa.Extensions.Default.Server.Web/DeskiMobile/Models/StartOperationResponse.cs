using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.Web.DeskiMobile.Models
{
    /// <summary>
    /// Модель для хранения информации о доступных endpoint'ах при выполнении операции в TESSA Assistant.
    /// </summary>
    public sealed class StartOperationResponse : StorageSerializable
    {
        #region Properties

        /// <summary>
        /// Абсолютный URI для загрузки потока с бинарными данными файла, указанного в операции.
        /// </summary>
        public string? GetContent { get; set; }

        /// <summary>
        /// Абсолютный URI для отмены операции.
        /// </summary>
        public string? PostCancel { get; set; }

        /// <summary>
        /// Абсолютный URI для загрузки информации о подписантах.
        /// </summary>
        public string? GetSignatures { get; set; }

        /// <summary>
        /// Абсолютный URI для проверки подписи.
        /// </summary>
        public string? PostVerify { get; set; }

        /// <summary>
        /// Абсолютный URI для усовершенствования подписи.
        /// </summary>
        public string? PostEnhance { get; set; }

        /// <summary>
        /// Абсолютный URI для скрытия диалога с результатами проверки подписи в ЛК.
        /// </summary>
        public string? PostHiddenVerifyWebDialog { get; set; }

        /// <summary>
        /// Объект с идентификаторами (CacheID) и именами файлов из операции.
        /// </summary>
        public Dictionary<string, string?> FilesInfo { get; set; } = new();

        #endregion

        #region Base Overrides

        /// <inheritdoc />
        protected override void SerializeCore(Dictionary<string, object?> storage)
        {
            storage[nameof(this.GetContent)] = this.GetContent;
            storage[nameof(this.PostCancel)] = this.PostCancel;
            storage[nameof(this.GetSignatures)] = this.GetSignatures;
            storage[nameof(this.PostVerify)] = this.PostVerify;
            storage[nameof(this.PostEnhance)] = this.PostEnhance;
            storage[nameof(this.PostHiddenVerifyWebDialog)] = this.PostHiddenVerifyWebDialog;
            storage[nameof(this.FilesInfo)] = this.FilesInfo?.Count > 0 ? this.FilesInfo : null;
        }

        /// <inheritdoc />
        protected override void DeserializeCore(Dictionary<string, object?> storage)
        {
            this.GetContent = storage.TryGet<string>(nameof(this.GetContent));
            this.PostCancel = storage.TryGet<string>(nameof(this.PostCancel));
            this.GetSignatures = storage.TryGet<string>(nameof(this.GetSignatures));
            this.PostVerify = storage.TryGet<string>(nameof(this.PostVerify));
            this.PostEnhance = storage.TryGet<string>(nameof(this.PostEnhance));
            this.PostHiddenVerifyWebDialog = storage.TryGet<string>(nameof(this.PostHiddenVerifyWebDialog));
            this.FilesInfo = storage.TryGet<Dictionary<string, string?>>(nameof(this.FilesInfo), new Dictionary<string, string?>()) ?? new();
        }

        #endregion
    }
}
