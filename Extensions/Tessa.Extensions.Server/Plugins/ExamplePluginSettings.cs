using System;
using System.Collections.Generic;
using Tessa.Platform.Plugins;
using Tessa.Platform.Runtime;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Server.Plugins
{
    /// <summary>
    /// Настройки плагина <see cref="ExamplePluginHandler.PluginName"/>.
    /// </summary>
    public sealed class ExamplePluginSettings : PluginSettings
    {
        #region Constructors

        /// <inheritdoc cref="PluginSettings(string)"/>
        public ExamplePluginSettings(string name)
            : base(name)
        {
        }

        #endregion

        #region Properties

        /// <summary>
        /// Идентификатор карточки.
        /// </summary>
        public Guid CardID { get; private set; } = Session.SystemID;

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        protected override void DeserializeCore(Dictionary<string, object?> storage)
        {
            base.DeserializeCore(storage);

            this.CardID = storage.TryGet<object>(nameof(this.CardID)) switch
            {
                Guid cardID => cardID,
                string cardIDString => Guid.TryParse(cardIDString, out var cardID)
                    ? cardID
                    : Session.SystemID,
                _ => Session.SystemID,
            };
        }

        /// <inheritdoc/>
        protected override void SerializeCore(Dictionary<string, object?> storage)
        {
            base.SerializeCore(storage);

            storage[nameof(this.CardID)] = this.CardID;
        }

        #endregion
    }
}
