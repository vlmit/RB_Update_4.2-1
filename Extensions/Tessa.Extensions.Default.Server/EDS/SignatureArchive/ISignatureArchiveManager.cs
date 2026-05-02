using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.EDS.SignatureArchive
{
    /// <summary>
    /// Объект, предоставляющий методы для обогащения подписей до архивного формата.
    /// </summary>
    public interface ISignatureArchiveManager
    {
        /// <summary>
        /// Обогащает указанные подписи до архивного формата.
        /// </summary>
        /// <param name="cardID">Идентификатор карточки.</param>
        /// <param name="fileIDs">Список файлов.</param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Результат валидации.</returns>
        public Task<ValidationResult> ArchiveSignaturesAsync(Guid cardID, IEnumerable<Guid> fileIDs, CancellationToken cancellationToken = default);
    }
}
