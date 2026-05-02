using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Tessa.Extensions.Default.Server.Web.DeskiMobile.Models;
using Tessa.Platform.Runtime;

namespace Tessa.Extensions.Default.Server.Web.DeskiMobile
{
    /// <summary>
    /// Manager, управляющий созданием и проверкой JWT токенов при взаимодействии с мобильным приложением.
    /// </summary>
    public interface IDeskiMobileTokenManager
    {
        /// <summary>
        /// Создание JWT токена.
        /// </summary>
        /// <param name="jwtLifeTime">Интервал жизни токена.</param>
        /// <param name="user">Информация о текущем пользователе.</param>
        /// <param name="operationID">Идентификатор операции, к которой привязывается JWT токен.</param>
        /// <param name="flags">Указывает доступные операции для создаваемого JWT токена.</param>
        /// <returns>JWT токен для взаимодействия с TESSA Assistant.</returns>
        string CreateToken(TimeSpan jwtLifeTime, IUser user, Guid operationID, DeskiMobileTokenPermissionFlags flags);

        /// <summary>
        /// Получение JWT токена из <see cref="HttpRequest" />.
        /// </summary>
        /// <param name="request">Объект, в котором хранится информация о запросе.</param>
        /// <returns>JWT токен для взаимодействия с TESSA Assistant.</returns>
        TokenInfo GetToken(HttpRequest request);
    }

}
