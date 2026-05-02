#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Extensions.Default.Server.OnlyOffice.Token;

namespace Tessa.Extensions.Default.Server.OnlyOffice
{
    /// <summary>
    /// Объект, выполняющий операции, связанные с интеграцией с Р7-Офис/OnlyOffice.
    /// </summary>
    public interface IOnlyOfficeService
    {
        /// <summary>
        /// Возвращает признак того, что интеграция с Р7-Офис/OnlyOffice включена.
        /// Обычно это определяется в соответствии с тем, что указана настройка <c>ApiScriptUrl</c> с адресом сервиса документов.
        /// </summary>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>
        /// <c>true</c>, если интеграция с Р7-Офис/OnlyOffice включена;
        /// <c>false</c> в противном случае.
        /// </returns>
        ValueTask<bool> IsEnabledAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает состояние указанного файла, который был открыт на редактирование.
        /// </summary>
        /// <param name="id">Идентификатор редактируемого файла.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Информация о файле в кэше.</returns>
        Task<IOnlyOfficeFileCacheInfo?> GetFileStateAsync(
            Guid id,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает содержимое редактируемого файла, используемое для сервера документов Р7-Офис/OnlyOffice.
        /// </summary>
        /// <param name="id">Идентификатор редактируемого файла.</param>
        /// <param name="token">Токен прав доступа для текущей сессии.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Содержимое редактируемого файла и название.</returns>
        /// <exception cref="InvalidOperationException">Файл не найден.</exception>
        Task<(Stream Stream, string FileName)> GetEditableFileAsync(
            Guid id,
            OnlyOfficeJwtTokenInfo token,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает содержимое файла, доступное после завершения редактирования на сервере документов Р7-Офис/OnlyOffice.
        /// </summary>
        /// <param name="id">Идентификатор редактируемого файла.</param>
        /// <param name="originalFormat">Признак того, что следует произвести конвертацию в исходный формат файла.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Содержимое редактируемого файла, имя файла</returns>
        /// <exception cref="InvalidOperationException">Файл не найден.</exception>
        Task<(Stream Stream, string FileName)> GetFinalFileAsync(
            Guid id,
            bool originalFormat = false,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Добавляет содержимое файла в кэш, используемый для сервера документов Р7-Офис/OnlyOffice.
        /// Связывается с версией файла в карточке в параметре <c>versionId</c>.
        /// Вызовите удаление документа запросом по завершению работы с ним, в т.ч. при закрытии вкладки или приложения.
        /// </summary>
        /// <param name="id">
        /// Идентификатор редактируемого файла, по которому его содержимое будет доступно в методах этого API.
        /// Укажите здесь уникальный идентификатор для каждого вызова, связанного с открытием версии файла на редактирование.
        /// </param>
        /// <param name="versionId">Идентификатор версии файла в карточке.</param>
        /// <param name="name">Имя файла в карточке, включая его расширение.</param>
        /// <param name="contentStream">Контент файла.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Токен для доступа к контенту файла и обновлению статусов.</returns>
        Task<string> CreateFileAsync(
            Guid id,
            Guid versionId,
            string name,
            Stream contentStream,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Обрабатывает обратный вызов от сервера документов по запросам, связанным с сохранением файла и закрытием документа.
        /// </summary>
        /// <param name="id">Идентификатор редактируемого файла.</param>
        /// <param name="token">Токен прав доступа для текущей сессии.</param>
        /// <param name="data">Объект с параметрами обратного вызова, определяемыми сервером документов Р7-Офис/OnlyOffice.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns></returns>
        Task CallbackAsync(
            Guid id,
            OnlyOfficeJwtTokenInfo token,
            IDictionary<string, object?> data,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Удаляет файл из кэша, используемого для взаимодействия с сервером документов Р7-Офис/OnlyOffice.
        /// </summary>
        /// <param name="id">Идентификатор редактируемого файла.</param>
        /// <param name="userID">Идентификатор пользователя.</param>
        /// <param name="isAdministrator">Пользователь администратор.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns></returns>
        Task DeleteAsync(
            Guid id,
            Guid userID,
            bool isAdministrator,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Создание информации о совместном редактировании.
        /// </summary>
        /// <param name="versionID">Идентификатор файла.</param>
        /// <param name="name">Название файла.</param>
        /// <param name="userID">Идентификатор пользователя.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Информация о файле, новый ли файл, токен для доступа к контенту файла и обновлению статусов.</returns>
        Task<(IOnlyOfficeFileCacheInfo FileInfo, bool IsNew, string? AccessToken)> InitiateCoeditAsync(
            Guid versionID,
            string name,
            Guid userID,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает текущую группу пользователей и последнюю дату обращения к файлу,
        /// или (последняя версия, null, null), если совместное редактирование не выполняется.
        /// </summary>
        /// <param name="fileVersionID">Идентификатор версии для совместного редактирования.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>
        /// Актуальная версия, пользователи через запятую, дата последнего обращения.
        /// Если совместное редактирование не выполняется, то (последняя версия, null, null).
        /// </returns>
        Task<(Guid? CurrentVersionID, string? UserNames, DateTime? LastAccessTime)> TryGetCurrentCoeditWithLastVersionAsync(
            Guid fileVersionID,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает текущую группу пользователей и последнюю дату обращения к файлу для группы файлов,
        /// или (последняя версия, null, null), если совместное редактирование не выполняется.
        /// </summary>
        /// <param name="fileVersionIDs">Идентификаторы версии для совместного редактирования.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>
        /// Актуальная версия, пользователи через запятую, дата последнего обращения.
        /// Если совместное редактирование не выполняется, то (последняя версия, null, null).
        /// </returns>
        Task<IReadOnlyList<(Guid CurrentVersionID, string? UserNames, DateTime? LastAccessTime)>> TryGetCurrentCoeditAsync(
            IEnumerable<Guid> fileVersionIDs,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Устанавливает флаг закрытия сеанса работы с редактором.
        /// </summary>
        /// <remarks>
        /// Иногда Р7-Офис/OnlyOffice не присылает сигнал о закрытии редактора, что влечет бесконечное ожидание пользователем.
        /// Поэтому введён таймаут на ожидание на клиенте, и искусственное закрытие.
        /// Это сделано для корректной работы функциональности совместного редактирования.
        /// </remarks>
        /// <param name="id">Идентификатор редактируемого файла.</param>
        /// <param name="userID">Идентификатор пользователя.</param>
        /// <param name="isAdministrator">Пользователь администратор.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns></returns>
        Task CloseEditorAsync(
            Guid id,
            Guid userID,
            bool isAdministrator,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает JWT дпя переданного payload aka config
        /// </summary>
        /// <param name="id">Индентификатор файла.</param>
        /// <param name="config">Подписываемый конфиг OnlyOffice/Р7, сериализованный в веб, как JSON.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>JWT или нет</returns>
        Task<string?> GetR7JWTAsync(
            Guid id,
            string config,
            CancellationToken cancellationToken = default);
    }
}
