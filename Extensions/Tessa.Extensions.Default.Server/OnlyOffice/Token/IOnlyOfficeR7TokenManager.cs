#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tessa.Extensions.Default.Server.OnlyOffice.Token
{
    /// <summary>
    /// Объект предоставляющий методы для взаимодействия с JWT токеном с ключом от OnlyOffice/R7.
    /// </summary>
    public interface IOnlyOfficeR7TokenManager
    {
        /// <summary>
        /// Создает подписанный JSON Web Token, где payload берется из переданного параметра
        /// </summary>
        /// <param name="config">Сериализованный в вебе JSON-строка.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>JSON Web Token сериализованный в формате JWS Compact.</returns>
        Task<string?> CreateTokenAsync(
            string config,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Проверяет JSON Web Token на выполнение операции для сервиса OnlyOffice.
        /// </summary>
        /// <param name="token">Проверяемый JSON Web Token на выполнение операции с контентом версии файла сериализованный в формате JWS Compact.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Информация о токене или значение <see langword="null"/>, если токен не является корректным.</returns>
        Task<OnlyOfficeJwtTokenInfo?> VerifyTokenAsync(
            string token,
            CancellationToken cancellationToken = default);        
    }
}
