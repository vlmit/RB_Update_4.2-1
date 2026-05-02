using System;
using System.Collections;
using System.Collections.Generic;
using Tessa.Cards;
using Tessa.Platform;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Shared.Acquaintance
{
    /// <summary>
    /// Вспомогательные методы для функциональности "Ознакомление".
    /// </summary>
    public static class AcquaintanceHelper
    {
        #region Constants

        /// <summary>
        /// Ключ, по которому хранится список ролей для ознакомления по умолчанию.
        /// </summary>
        public const string DefaultRolesKey = StorageHelper.SystemKeyPrefix + "DefaultRoles";

        /// <summary>
        /// Ключ, по которому хранится список идентификаторов ролей для ознакомления по умолчанию.
        /// </summary>
        public const string DefaultRolesIDListKey = "IDList";

        /// <summary>
        /// Ключ, по которому хранится список имён ролей для ознакомления по умолчанию.
        /// </summary>
        public const string DefaultRolesNameListKey = "NameList";

        /// <summary>
        /// Ключ, по которому хранится список ролей на ознакомление.
        /// </summary>
        public const string RolesKey = StorageHelper.SystemKeyPrefix + "Roles";

        /// <summary>
        /// Ключ, по которому хранится комментарий ознакомления.
        /// </summary>
        public const string CommentKey = StorageHelper.SystemKeyPrefix + "Comment";

        /// <summary>
        /// Ключ, по которому хранится флаг, который определяет, что ознакомление не должно отправляться заместителям.
        /// </summary>
        public const string ExcludeDeputiesKey = StorageHelper.SystemKeyPrefix + "ExcludeDeputies";

        /// <summary>
        /// Ключ, по которому хранится флаг, который определяет, что при успешной отправке на ознакомление требуется вывести информационное сообщение в <c>ValidationResult</c>
        /// о количестве сотрудников, которым было направлено ознакомление.
        /// </summary>
        public const string AddSuccessMessageKey = StorageHelper.SystemKeyPrefix + "AddSuccessMessage";

        #endregion

        #region Static Methods

        /// <summary>
        /// Возвращает список ролей для массового ознакомления по умолчанию.
        /// </summary>
        /// <param name="info">Info с данными.</param>
        /// <returns>Список ролей с идентификаторами.</returns>
        public static List<Tuple<Guid, string>> GetAcquaintanceDefaultRoles(Dictionary<string, object> info)
        {
            ThrowIfNull(info);

            var result = new List<Tuple<Guid, string>>();

            if (info.ContainsKey(DefaultRolesKey))
            {
                if (info[DefaultRolesKey] is Dictionary<string, object> defaultRoles
                    && defaultRoles.TryGetValue(DefaultRolesIDListKey, out object idsListValue)
                    && idsListValue is IList idsList
                    && defaultRoles.TryGetValue(DefaultRolesNameListKey, out object namesListValue)
                    && namesListValue is IList namesList)
                {
                    if (idsList.Count != namesList.Count)
                    {
                        return result;
                    }

                    for (int i = 0; i < idsList.Count; i++)
                    {
                        var id = idsList[i];
                        var name = namesList[i];

                        result.Add(new Tuple<Guid, string>((Guid) id, (string) name));
                    }
                }

            }
            return result;
        }

        /// <summary>
        /// Возвращает информацию для отправки ознакомления из <paramref name="info"/>.
        /// </summary>
        /// <param name="info">Объект с дополнительной информацией.</param>
        /// <param name="roleList">Список идентификаторов ролей.</param>
        /// <param name="comment">Комментарий.</param>
        /// <param name="excludeDeputies">Флаг, который определяет, что ознакомление не должно отправляться заместителям.</param>
        /// <param name="addSuccessMessage">
        /// Флаг, который определяет, что при успешной отправке на ознакомление требуется вывести информационное сообщение в <c>ValidationResult</c>
        /// о количестве сотрудников, которым было направлено ознакомление.
        /// </param>
        public static void TryGetAcquaintanceInfo(
            Dictionary<string, object> info,
            out List<Guid> roleList,
            out string comment,
            out bool excludeDeputies,
            out bool addSuccessMessage)
        {
            ThrowIfNull(info);

            roleList = info.TryGet<List<Guid>>(RolesKey);
            comment = info.TryGet<string>(CommentKey);
            excludeDeputies = info.TryGet<bool>(ExcludeDeputiesKey);
            addSuccessMessage = info.TryGet<bool>(AddSuccessMessageKey);
        }

        /// <summary>
        /// Добавляет информацию для отправки ознакомления в запрос <paramref name="request"/>.
        /// </summary>
        /// <param name="request">Запрос.</param>
        /// <param name="roleIDList">Список идентификаторов ролей.</param>
        /// <param name="comment">Комментарий.</param>
        /// <param name="excludeDeputies">Флаг, который определяет, что ознакомление не должно отправляться заместителям.</param>
        /// <param name="addSuccessMessage">
        /// Флаг, который определяет, что при успешной отправке на ознакомление требуется вывести информационное сообщение в <c>ValidationResult</c>
        /// о количестве сотрудников, которым было направлено ознакомление.
        /// </param>
        public static void SetAcquaintanceInfo(
            CardRequest request,
            IReadOnlyList<Guid> roleIDList,
            string comment,
            bool excludeDeputies,
            bool addSuccessMessage)
        {
            ThrowIfNull(request);
            ThrowIfNull(roleIDList);

            request.Info[CommentKey] = comment;
            request.Info[RolesKey] = roleIDList;
            request.Info[ExcludeDeputiesKey] = BooleanBoxes.Box(excludeDeputies);
            request.Info[AddSuccessMessageKey] = BooleanBoxes.Box(addSuccessMessage);
        }

        #endregion
    }
}
