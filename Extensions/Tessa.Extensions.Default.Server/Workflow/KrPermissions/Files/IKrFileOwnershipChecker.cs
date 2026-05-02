#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;

namespace Tessa.Extensions.Default.Server.Workflow.KrPermissions.Files
{
    /// <summary>
    /// Объект, проверяющий принадлежность файла или версии файла.
    /// </summary>
    public interface IKrFileOwnershipChecker
    {
        /// <summary>
        /// Определяет, принадлежит ли файл или версия файла указанному пользователю 
        /// или также, если установлен флаг <paramref name="checkDeputized"/>, одному из замещаемых им сотрудников.
        /// </summary>
        /// <param name="fileOrVersionCreatedByID">Идентификатор создателя файла или версии файла.</param>
        /// <param name="userId">Идентификатор текущего пользователя.</param>
        /// <param name="checkDeputized">Если флаг установлен, то метод вернет <see langword="true"/>,
        /// если создателем файла или версии файла является один замещаемых сотрудников. 
        /// В ином случае проверка проводится только по идентификатору сотрудника.</param>
        /// <param name="card">Карточка или <see langword="null"/>, если значение не задано.</param>
        /// <param name="cardId">Идентификатор карточки или <see langword="null"/>, если значение не задано.</param>
        /// <param name="cardDocTypeId">Идентификатор типа документа карточки,
        /// или идентификатор типа карточки, если для типа карточки не используется тип документа, но карточка находится в типовом решении,
        /// или <see langword="null"/>, если значение не задано.</param>
        /// <param name="cacheHolder">Словарь, содержащий кэш сотрудников, замещаемых пользователем с идентификатором <paramref name="userId"/> для даннной карточки.</param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="summary"/></param>
        /// <returns><see langword="true"/>, если файл принадлежит указанному пользователю или также, 
        /// если установлен флаг <paramref name="checkDeputized"/>, одному из замещаемых им сотрудников, иначе <see langword="false"/>.</returns>
        public ValueTask<bool> IsOwnAsync(
            Guid fileOrVersionCreatedByID,
            Guid userId,
            bool checkDeputized,
            Card? card = null,
            Guid? cardId = null,
            Guid? cardDocTypeId = null,
            Dictionary<string, object?>? cacheHolder = null,
            CancellationToken cancellationToken = default);
    }
}
