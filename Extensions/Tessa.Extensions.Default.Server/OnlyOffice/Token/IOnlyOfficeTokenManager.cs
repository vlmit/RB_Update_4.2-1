#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tessa.Extensions.Default.Server.OnlyOffice.Token
{
    /// <summary>
    /// Объект предоставляющий методы для взаимодействия с JWT токеном.
    /// </summary>
    public interface IOnlyOfficeTokenManager
    {
        /// <summary>
        /// Создает подписанный закрытым ключом сервера JSON Web Token на выполнение операции для сервиса OnlyOffice.
        /// </summary>
        /// <param name="versionID">Идентификатор версии файла.</param>
        /// <param name="flags">Разрешения на операцию.</param>
        /// <param name="expiresPeriosInMinutes">Время действия токена.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>JSON Web Token сериализованный в формате JWS Compact на операцию.</returns>
        Task<string> CreateTokenAsync(
            Guid ID,
            Guid versionID,
            int expiresPeriosInMinutes,
            OnlyOfficeTokenPermissionFlags flags,
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
