#nullable enable

namespace Tessa.Extensions.Default.Server.Workflow.KrPermissions
{
    /// <summary>
    /// Статус результата создания контекста.
    /// </summary>
    public enum KrPermissionsCreateContextStatus
    {
        /// <summary>
        /// Создание контекста выполнено успешно.
        /// </summary>
        Success,

        /// <summary>
        /// При создании контекста возникла ошибка.
        /// </summary>
        Fail,

        /// <summary>
        /// Проверка доступа для данной карточки или типа карточки запрещена, контекст не создан.
        /// </summary>
        NotAllowed,
    }
}
