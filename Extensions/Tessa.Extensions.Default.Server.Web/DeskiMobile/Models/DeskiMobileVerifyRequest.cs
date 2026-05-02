using System;
using System.Linq;
using System.Collections.Generic;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.Web.DeskiMobile.Models
{
    /// <summary>
    /// Модель, которая ожидается в теле запроса на проверку подписи всех файлов из операции.
    /// </summary>
    public sealed class DeskiMobileVerifyRequest : StorageSerializable
    {
        #region Properties

        /// <summary>
        /// Информация о подписях, где
        /// Key - CacheID файла из операции,
        /// Value - информация о подписях файла с конкретным CacheID.
        ///     Key - идентификатор подписи файла,
        ///     Value - подпись файла,
        /// </summary>
        public Dictionary<string, Dictionary<string, string>> Values { get; set; } = new();

        #endregion

        #region Base Overrides

        /// <inheritdoc />
        protected override void SerializeCore(Dictionary<string, object?> storage)
        {
            foreach ((string key, Dictionary<string, string> value) in this.Values)
            {
                storage[key] = value;
            }
        }

        /// <inheritdoc />
        protected override void DeserializeCore(Dictionary<string, object?> storage)
        {
            this.Values = storage.ToDictionary(x => x.Key, x => {
                if (x.Value is null)
                {
                    throw new InvalidOperationException($"Signatures for CacheID={x.Key} is empty.");
                }

                var dictionary = x.Value as Dictionary<string, object>;
                if (dictionary is null)
                {
                    throw new InvalidOperationException($"SerializeObject for CacheID={x.Key} is empty.");
                }
                var stringDictionary = new Dictionary<string, string>(dictionary.Count);
                foreach (var kvp in dictionary)
                {
                    stringDictionary.Add(kvp.Key, (string)kvp.Value);
                }

                return stringDictionary;

            });
        }

        #endregion
    }
}
