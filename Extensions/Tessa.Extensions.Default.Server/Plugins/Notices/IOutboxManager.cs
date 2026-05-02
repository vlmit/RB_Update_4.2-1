#nullable enable

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Tessa.Extensions.Default.Server.Plugins.Notices
{
    /// <summary>
    /// Outbox messages manager.
    /// </summary>
    public interface IOutboxManager
    {
        /// <summary>
        /// Select up to given number of messages from outbox.
        /// </summary>
        /// <param name="topCount">Maximum number of selected messages.</param>
        /// <param name="retryIntervalMinutes">Erroneous messages time threshold.</param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Selected messages.</returns>
        Task<ConcurrentQueue<OutboxMessage>> GetTopMessagesAsync(
            int topCount,
            int retryIntervalMinutes,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Marks the message as erroneous and, if the retries threshold is exceeded, deletes the message.
        /// </summary>
        /// <param name="id">Message identifier.</param>
        /// <param name="attemptNum">Current attempt number.</param>
        /// <param name="exceptionMessage">Error message.</param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns><inheritdoc cref="Task" path="/summary"/></returns>
        Task MarkAsBadMessageAsync(
            Guid id,
            int attemptNum,
            string? exceptionMessage,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes the message.
        /// </summary>
        /// <param name="id">Message identifier.</param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns><inheritdoc cref="Task" path="/summary"/></returns>
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
