#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Serialization
{
    /// <inheritdoc cref="IKrStageSerializerSettingsCache"/>
    /// <param name="cardCachedMetadata"><inheritdoc cref="ICardCachedMetadata" path="/summary"/></param>
    public sealed class KrStageSerializerSettingsCache(ICardCachedMetadata cardCachedMetadata)
        : IKrStageSerializerSettingsCache
    {
        #region Fields

        private readonly ICardCachedMetadata cardCachedMetadata = NotNullOrThrow(cardCachedMetadata);

        // volatile не нужен, вторая проверка выполняется после await,
        // а значит значение result2 будет актуально
        private IKrStageSerializerSettings? value;

        #endregion

        #region IKrStageSerializerSettingsCache Members

        /// <inheritdoc/>
        public async ValueTask<IKrStageSerializerSettings> GetAsync(
            CancellationToken cancellationToken = default)
        {
            if (this.value is { } result)
            {
                return result;
            }

            await this.cardCachedMetadata.GetCachedMetadataAsync(cancellationToken);

            if (this.value is { } result2)
            {
                return result2;
            }

            throw new InvalidOperationException(
                $"Can't acquire {nameof(IKrStageSerializerSettings)}. Check that {nameof(ICardMetadata)} is valid.");
        }

        /// <inheritdoc/>
        public ValueTask SetAsync(
            IKrStageSerializerSettings value,
            CancellationToken cancellationToken = default)
        {
            this.value = NotNullOrThrow(value);
            return ValueTask.CompletedTask;
        }

        #endregion
    }
}
