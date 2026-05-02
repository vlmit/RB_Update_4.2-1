#nullable enable

namespace Tessa.Extensions.Default.Server.Plugins
{
    /// <summary>
    /// Имена плагинов типового решения.
    /// </summary>
    public static class DefaultPluginNames
    {
        #region Consts

        /// <summary>
        /// Имя плагина для конвертации файлов.
        /// </summary>
        public const string FileConverterPlugin = nameof(FileConverterPlugin);

        /// <summary>
        /// Имя плагина для очистки старых файлов в карточке кэша конвертации.
        /// </summary>
        public const string FileConverterRemoveCachePlugin = nameof(FileConverterRemoveCachePlugin);

        /// <summary>
        /// Имя плагина для обработки автоматического согласования.
        /// </summary>
        public const string KrAutoApprovePlugin = nameof(KrAutoApprovePlugin);

        /// <summary>
        /// Имя плагина мобильного согласования.
        /// </summary>
        public const string MobileApprovalPlugin = nameof(MobileApprovalPlugin);

        /// <summary>
        /// Имя плагина для рассылки уведомлений.
        /// </summary>
        public const string MailSenderPlugin = nameof(MailSenderPlugin);

        /// <summary>
        /// Имя плагина для очистки файлового кэша конвертации.
        /// </summary>
        public const string OnlyOfficeRemoveFileCacheInfoPlugin = nameof(OnlyOfficeRemoveFileCacheInfoPlugin);

        /// <summary>
        /// Имя плагина для отправки пользователю уведомления с напоминанием о скорой необходимости смены пароля.
        /// </summary>
        public const string PasswordNotificationsPlugin = nameof(PasswordNotificationsPlugin);

        /// <summary>
        /// Имя плагина для перерасчёта групп ссылок.
        /// </summary>
        public const string RefGroupsRecalculatePlugin = nameof(RefGroupsRecalculatePlugin);

        /// <summary>
        /// Имя плагина, возвращающего из отложенного задания, для которых срок откладывания завершился.
        /// </summary>
        public const string ReturnTasksFromPostponedPlugin = nameof(ReturnTasksFromPostponedPlugin);

        /// <summary>
        /// Имя плагина для отправки уведомлений о заданиях пользователя.
        /// </summary>
        public const string TasksNotificationsPlugin = nameof(TasksNotificationsPlugin);

        /// <summary>
        /// Имя плагина для отправки уведомлений о необходимости обновить токен для подписи.
        /// </summary>
        public const string TokenNotificationsPlugin = nameof(TokenNotificationsPlugin);

        /// <summary>
        /// Имя плагина для обогащения подписей файлов до профиля <see cref="Tessa.Platform.EDS.SignatureProfile.A"/>.
        /// </summary>
        public const string SignatureArchivePlugin = nameof(SignatureArchivePlugin);

        #endregion
    }
}
