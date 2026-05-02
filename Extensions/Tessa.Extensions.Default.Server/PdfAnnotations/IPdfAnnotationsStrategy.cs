#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;

namespace Tessa.Extensions.Default.Server.PdfAnnotations
{
    /// <summary>
    /// Strategy for working with pdf annotations at file.
    /// </summary>
    public interface IPdfAnnotationsStrategy
    {
        /// <summary>
        /// Get list of <see cref="PdfAnnotationsData"/> with information about pdf annotations for all files in a card.
        /// </summary>
        /// <param name="card">Card with pdf annotations.</param>
        /// <param name="cancellationToken">An object that can be used to cancel an asynchronous task.</param>
        /// <returns>List of <see cref="PdfAnnotationsData"/> with information about pdf annotations.</returns>
        Task<IList<PdfAnnotationsData>> TryGetInfoAsync(Card card, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get info about pdf annotation by <see cref="PdfAnnotationsData.ID"/>, or by <see cref="PdfAnnotationsData.CardID"/>,
        /// <see cref="PdfAnnotationsData.FileID"/>, <see cref="PdfAnnotationsData.FileVersionRowID"/> if <see cref="PdfAnnotationsData.ID"/> is absent.
        /// </summary>
        /// <param name="info">
        /// Either <see cref="PdfAnnotationsData.ID"/> or <see cref="PdfAnnotationsData.CardID"/>,
        /// <see cref="PdfAnnotationsData.FileID"/>, <see cref="PdfAnnotationsData.FileVersionRowID"/> must be present.
        /// </param>
        /// <param name="cancellationToken">An object that can be used to cancel an asynchronous task.</param>
        /// <returns>Pdf annotations info.</returns>
        Task<PdfAnnotationsData?> TryGetInfoAsync(PdfAnnotationsData info, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get info about pdf annotations.
        /// </summary>
        /// <param name="infos">List of <see cref="PdfAnnotationsData"/> with information about pdf annotations.</param>
        /// <param name="cancellationToken">An object that can be used to cancel an asynchronous task.</param>
        /// <returns>Pdf annotations info list.</returns>
        Task<IList<PdfAnnotationsData>> TryGetInfoAsync(IList<PdfAnnotationsData> infos, CancellationToken cancellationToken = default);

        /// <summary>
        /// Request for merged annotations before store.
        /// </summary>
        /// <param name="info"><inheritdoc cref="PdfAnnotationsData" path="/summary"/></param>
        /// <param name="modifiedByID">User identifier which perform merging.</param>
        /// <param name="cancellationToken">An object that can be used to cancel an asynchronous task.</param>
        /// <returns>Pdf annotations info.</returns>
        Task<PdfAnnotationsData> RequestMergedAnnsBeforeStoreAsync(PdfAnnotationsData info, Guid modifiedByID, CancellationToken cancellationToken = default);

        /// <summary>
        /// Store pdf annotations.
        /// </summary>
        /// <param name="info"><inheritdoc cref="PdfAnnotationsData" path="/summary"/></param>
        /// <param name="modifiedByID">User identifier which perform merging.</param>
        /// <param name="cancellationToken">An object that can be used to cancel an asynchronous task.</param>
        /// <returns>Pdf annotations info id.</returns>
        Task<Guid> StoreAsync(PdfAnnotationsData info, Guid modifiedByID, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete pdf annotations by <paramref name="cardIDs"/>.
        /// </summary>
        /// <param name="cardIDs">Cards identifiers for filter.</param>
        /// <param name="withBackup">Delete pdf annotations with backup.</param>
        /// <param name="cancellationToken">An object that can be used to cancel an asynchronous task.</param>
        /// <returns>An asynchronous task.</returns>
        Task DeleteCardsFilesAnnotationsAsync(IList<Guid> cardIDs, bool withBackup = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Restore pdf annotations by <paramref name="cardIDs"/>/
        /// </summary>
        /// <param name="cardIDs">Cards identifiers for filter.</param>
        /// <param name="cancellationToken">An object that can be used to cancel an asynchronous task.</param>
        /// <returns>An asynchronous task.</returns>
        Task RestoreCardsFilesAnnotationsAsync(IList<Guid> cardIDs, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete pdf annotations by <paramref name="fileIDs"/>.
        /// </summary>
        /// <param name="fileIDs">Files identifiers for filter.</param>
        /// <param name="withBackup">Delete pdf annotations with backup.</param>
        /// <param name="cancellationToken">An object that can be used to cancel an asynchronous task.</param>
        /// <returns>An asynchronous task.</returns>
        Task DeleteFileAnnotationsAsync(IList<Guid> fileIDs, bool withBackup = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Restore pdf annotations by <paramref name="fileIDs"/>.
        /// </summary>
        /// <param name="fileIDs">Files identifiers for filter.</param>
        /// <param name="cancellationToken">An object that can be used to cancel an asynchronous task.</param>
        /// <returns>An asynchronous task.</returns>
        Task RestoreFileAnnotationsAsync(IList<Guid> fileIDs, CancellationToken cancellationToken = default);
    }
}
