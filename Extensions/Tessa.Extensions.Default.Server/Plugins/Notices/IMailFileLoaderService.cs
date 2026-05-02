using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Notices;

namespace Tessa.Extensions.Default.Server.Plugins.Notices
{
    /// <summary>
    /// Mail file content loader.
    /// </summary>
    public interface IMailFileLoaderService
    {
        /// <summary>
        /// Try to load given file content.
        /// If successful content stream will be provided in callback function.
        /// If content is not ready - response will be null.
        /// </summary>
        /// <param name="cardID">Card identifier.</param>
        /// <param name="cardTypeID">Card type identifier.</param>
        /// <param name="cardTypeName">Card type name.</param>
        /// <param name="file"><inheritdoc cref="MailFile" path="/summary"/></param>
        /// <param name="processContentActionAsync">Content provider callback.</param>
        /// <param name="userID">Optional user identifier.</param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns><see cref="CardGetFileContentResponse"/> if content is ready and <see langword="null"/> in other case.</returns>
        Task<CardGetFileContentResponse?> TryLoadContentAsync(
            Guid? cardID,
            Guid? cardTypeID,
            string? cardTypeName,
            MailFile file,
            Func<Stream, CancellationToken, ValueTask> processContentActionAsync,
            Guid? userID = null,
            CancellationToken cancellationToken = default);
    }
}
