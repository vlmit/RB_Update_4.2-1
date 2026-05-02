using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Extensions.Default.Server.Web.DeskiMobile.Models;
using Tessa.Platform.Operations;

namespace Tessa.Extensions.Default.Server.Web.DeskiMobile
{
    /// <summary>
    /// Manager, управляющий генерацией ссылки для выполнения операции с мобильным приложением,
    /// а также созданием, удалением, завершением, получением статуса операции,
    /// получением файлов и сигнатур файлов участвующих в операции,
    /// формированием OperationResponse с результатом операции.
    /// </summary>
    public interface IDeskiMobileManager
    {
        /// <summary>
        /// Генерация ссылки для выполнения операции с TESSA Assistant.
        /// </summary>
        /// <param name="files">Массив файлов над которыми будет выполняться операция.</param>
        /// <param name="operation">Тип выполняемой операции.</param>
        /// <param name="url">Url для выполнения запроса с мобильного приложения для подтверждения своего наличия на устройстве пользователя.</param>
        /// <param name="cancellationToken">Токен для отмены асинхронной задачи.</param>
        /// <returns>Cсылка для выполнения операции. Запрос выполняется с TESSA Assistant.</returns>
        ValueTask<string> GenerateLinkAsync(DeskiMobileFile[] files, string operation, string url, CancellationToken cancellationToken = default);

        /// <summary>
        /// Запуск операции.
        /// Требуется вызвать при выполнении первого запроса с мобильного приложения TESSA Assistant.
        /// </summary>
        /// <param name="tokenInfo">Объект, в котором хранится информация о токене.</param>
        /// <param name="cancellationToken">Токен для отмены асинхронной задачи.</param>
        ValueTask StartOperationAsync(TokenInfo tokenInfo, CancellationToken cancellationToken = default);

        /// <summary>
        /// Удаление операции.
        /// </summary>
        /// <param name="tokenInfo">Объект, в котором хранится информация о токене.</param>
        /// <param name="cancellationToken">Токен для отмены асинхронной задачи.</param>
        ValueTask DeleteOperationAsync(TokenInfo tokenInfo, CancellationToken cancellationToken = default);

        /// <summary>
        /// Завершение операции.
        /// </summary>
        /// <param name="tokenInfo">Объект, в котором хранится информация о токене.</param>
        /// <param name="response">Результат выполнения операции.</param>
        /// <param name="cancellationToken">Токен для отмены асинхронной задачи.</param>
        ValueTask CompleteOperationAsync(TokenInfo tokenInfo, OperationResponse response, CancellationToken cancellationToken = default);

        /// <summary>
        /// Получение статуса операции.
        /// </summary>
        /// <param name="tokenInfo">Объект, в котором хранится информация о токене.</param>
        /// <param name="cancellationToken">Токен для отмены асинхронной задачи.</param>
        /// <returns>Статус операции.</returns>
        ValueTask<OperationState> GetOperationStateAsync(TokenInfo tokenInfo, CancellationToken cancellationToken = default);

        /// <summary>
        /// Получение содержимого операции.
        /// </summary>
        /// <param name="tokenInfo">Объект, в котором хранится информация о токене.</param>
        /// <param name="cancellationToken">Токен для отмены асинхронной задачи.</param>
        /// <returns>Содержимое операции.</returns>
        ValueTask<OperationResponse?> TryGetOperationResponseAsync(TokenInfo tokenInfo, CancellationToken cancellationToken = default);

        /// <summary>
        /// Получение идентификаторов (CacheID) и имён файлов из операции.
        /// </summary>
        /// <param name="tokenInfo">Объект, в котором хранится информация о токене.</param>
        /// <param name="cancellationToken">Токен для отмены асинхронной задачи.</param>
        /// <returns>Объект с идентификаторами (CacheID) и именами файлов</returns>
        ValueTask<Dictionary<string, string?>> GetOperationFilesInfoAsync(TokenInfo tokenInfo, CancellationToken cancellationToken = default);

        /// <summary>
        /// Получение имени файла из операции по его идентификатору cacheID.
        /// </summary>
        /// <param name="tokenInfo">Объект, в котором хранится информация о токене.</param>
        /// <param name="cacheID">Идентификатор файла в операции.</param>
        /// <param name="cancellationToken">Токен для отмены асинхронной задачи.</param>
        /// <returns>Имя файла из операции.</returns>
        ValueTask<string?> GetFileNameAsync(TokenInfo tokenInfo, string cacheID, CancellationToken cancellationToken = default);

        /// <summary>
        /// Получение содержимого файла в виде двоичного потока.
        /// </summary>
        /// <param name="tokenInfo">Объект, в котором хранится информация о токене.</param>
        /// <param name="cacheID">Идентификатор файла в операции.</param>
        /// <param name="cancellationToken">Токен для отмены асинхронной задачи.</param>
        /// <returns>Содержимого файла в виде двоичного потока.</returns>
        ValueTask<Stream> GetFileContentAsync(TokenInfo tokenInfo, string cacheID, CancellationToken cancellationToken = default);

        /// <summary>
        /// Получение сигнатур подписи для всех файлов из операции.
        /// </summary>
        /// <param name="tokenInfo">Объект, в котором хранится информация о токене.</param>
        /// <param name="cancellationToken">Токен для отмены асинхронной задачи.</param>
        /// <returns>Сигнатуры подписи всех файлов из операции.</returns>
        ValueTask<Dictionary<string, List<FileSignatureResponse>>> GetSignaturesAsync(TokenInfo tokenInfo, CancellationToken cancellationToken = default);

        /// <summary>
        /// Формирование OperationResponse c результатами обогащения подписи всех файлов в операции.
        /// </summary>
        /// <param name="tokenInfo">Объект, в котором хранится информация о токене.</param>
        /// <param name="parameters">Объект, в котором содержится информация о сигнатурах подписи для обогащения.</param>
        /// <param name="cancellationToken">Токен для отмены асинхронной задачи.</param>
        /// <returns>OperationResponse c результатами обогащения подписи всех файлов в операции.</returns>
        ValueTask<OperationResponse> GetOperationResponseForEnhanceAsync(TokenInfo tokenInfo, DeskiMobileEnhanceRequest parameters, CancellationToken cancellationToken = default);

        /// <summary>
        /// Формирование OperationResponse c результатами проверки подписи всех файлов в операции.
        /// </summary>
        /// <param name="tokenInfo">Объект, в котором хранится информация о токене.</param>
        /// <param name="parameters">Объект, в котором содержится информация о сигнатурах подписи для обогащения.</param>
        /// <param name="cancellationToken">Токен для отмены асинхронной задачи.</param>
        /// <returns>OperationResponse c результатами проверки подписи всех файлов в операции.</returns>
        ValueTask<OperationResponse> GetOperationResponseForVerifyAsync(TokenInfo tokenInfo, DeskiMobileVerifyRequest parameters, CancellationToken cancellationToken = default);

        /// <summary>
        /// Формирование OperationResponse при отмене операции.
        /// </summary>
        /// <param name="tokenInfo">Объект, в котором хранится информация о токене.</param>
        OperationResponse GetOperationResponseForCancel(TokenInfo tokenInfo);
    }
}
