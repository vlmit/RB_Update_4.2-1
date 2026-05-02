using System;
using System.Linq;
using System.Collections.Generic;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.Web.DeskiMobile.Models
{
    /// <summary>
    /// Модель, которая ожидается в теле запроса на обогащение подписей файлов из операции.
    /// </summary>
    public sealed class DeskiMobileEnhanceRequest : StorageSerializable
    {
        #region Properties

        /// <summary>
        /// Данные подписи, где
        /// Key - CacheID файла из операции,
        /// Value - сигнатура подписи в base64.
        /// </summary>
        public Dictionary<string, string> Values { get; set; } = new();

        #endregion

        #region Base Overrides

        /// <inheritdoc />
        protected override void SerializeCore(Dictionary<string, object?> storage)
        {
            foreach ((string key, string value) in this.Values)
            {
                storage[key] = value;
            }
        }

        /// <inheritdoc />
        protected override void DeserializeCore(Dictionary<string, object?> storage)
        {
            this.Values = storage.ToDictionary(x => x.Key, x => x.Value?.ToString() ?? throw new InvalidOperationException($"Signature for CacheID={x.Key} is empty."));
        }

        #endregion
    }
}
