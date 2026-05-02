#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Platform.Data;

namespace Tessa.Extensions.Default.Server.Plugins.Notices
{
    /// <summary>
    /// Вспомогательные методы для плагинов, рассылающих системные уведомления.
    /// </summary>
    public static class SettingNotificationHelper
    {
        #region Helpers

        /// <summary>
        /// Возвращает уникальный список адресов электронной почты,
        /// которым надо отослать уведомления для секции с заданным именем.
        /// </summary>
        /// <param name="dbScope">Объект, обеспечивающий взаимодействие с базой данных.</param>
        /// <param name="roleSectionName">Имя секции с ролями, которые содержат пользователей для отправки уведомлений.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Полученный список получателей</returns>
        public static async Task<List<Guid>> GetEmailSettingsAsync(IDbScope dbScope, string roleSectionName, CancellationToken cancellationToken = default)
        {
            await using var _ = dbScope.Create();
            var db = dbScope.Db;
            var builderFactory = dbScope.BuilderFactory;

            return await db
                .SetCommand(
                    builderFactory
                        .SelectDistinct().C("pr", "ID")
                        .From(roleSectionName, "n").NoLock()
                        .InnerJoin("RoleUsers", "ru").NoLock()
                        .On().C("ru", "ID").Equals().C("n", "RoleID")
                        .InnerJoin("PersonalRoles", "pr").NoLock()
                        .On().C("pr", "ID").Equals().C("ru", "UserID")
                        .Where().C("pr", "Email").IsNotNull()
                        .And().C("pr", "Email").NotEquals().V(string.Empty)
                        .Build())
                .LogCommand()
                .ExecuteListAsync<Guid>(cancellationToken);
        }

        #endregion
    }
}
