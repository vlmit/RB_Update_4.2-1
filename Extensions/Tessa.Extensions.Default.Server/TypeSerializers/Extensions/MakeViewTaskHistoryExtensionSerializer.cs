#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.TypeSerializers;
using Tessa.Cards.TypeSettings;
using Tessa.Extensions.Default.Shared.Cards;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.TypeSerializers.Extensions
{
    /// <summary>
    /// Сериализатор расширения типа <see cref="DefaultCardTypeExtensionTypes.MakeViewTaskHistory"/>.
    /// </summary>
    public sealed class MakeViewTaskHistoryExtensionSerializer : ITypeComponentSerializer<CardTypeExtension>
    {
        #region ITypeComponentSerializer<CardTypeExtension> Members

        /// <inheritdoc/>
        public ValueTask NotifyOnSerializingAsync(
            Dictionary<string, object?> settings,
            CardTypeExtension component,
            ICardSerializableContext context,
            CancellationToken cancellationToken = default)
        {
            settings.RemoveIfEmptyString(CardTypeExtensionSettings.ViewControlAlias);
            settings.RemoveIfDefault(CardTypeExtensionSettings.CollapseGroups, false);
            return ValueTask.CompletedTask;
        }

        #endregion
    }
}
