namespace Tessa.Test.Default.Shared
{
    /// <summary>
    /// Предоставляет стандартные пути к встроенным ресурсам.
    /// </summary>
    public static class ResourcesPaths
    {
        /// <summary>
        /// Базовый путь к ресурсам.
        /// </summary>
        public const string Resources = nameof(Resources);

        /// <summary>
        /// Путь к библиотекам локализации: <see cref="Resources"/>\<see cref="Localization"/>.
        /// </summary>
        public const string Localization = nameof(Localization);

        /// <summary>
        /// Путь к сценариям со скриптами: <see cref="Resources"/>\<see cref="Sql"/>.
        /// </summary>
        public const string Sql = nameof(Sql);

        /// <summary>
        /// Путь к схемам данных: <see cref="Resources"/>\<see cref="Tsd"/>.
        /// </summary>
        public const string Tsd = nameof(Tsd);

        /// <summary>
        /// Путь к представлениям: <see cref="Resources"/>\<see cref="Views"/>.
        /// </summary>
        public const string Views = nameof(Views);

        /// <summary>
        /// Путь к рабочим местам: <see cref="Resources"/>\<see cref="Workplaces"/>.
        /// </summary>
        public const string Workplaces = nameof(Workplaces);

        /// <summary>
        /// Предоставляет пути к карточкам.
        /// </summary>
        public static class Cards
        {
            /// <summary>
            /// Путь к карточкам ролей: <see cref="Cards"/>\<see cref="Roles"/>.
            /// </summary>
            public static class Roles
            {
                /// <summary>
                /// Путь к карточкам ролей: <see cref="Cards"/>\<see cref="Roles"/>.
                /// </summary>
                public const string Name = nameof(Roles);

                /// <summary>
                /// Путь к карточкам ролей, предназначенным для использования на СУБД SqlServer: <see cref="Roles"/>\<see cref="SqlServer"/>.
                /// </summary>
                public const string SqlServer = nameof(SqlServer);

                /// <summary>
                /// Путь к карточкам ролей, предназначенным для использования на СУБД PostgreSql: <see cref="Roles"/>\<see cref="PostgreSql"/>.
                /// </summary>
                public const string PostgreSql = nameof(PostgreSql);
            }

            /// <summary>
            /// Путь к карточкам: <see cref="Resources"/>\<see cref="Cards"/>.
            /// </summary>
            public const string Name = nameof(Cards);

            /// <summary>
            /// Путь к карточкам конфигурации: <see cref="Resources"/>\<see cref="Cards"/>\<see cref="Configuration"/>.
            /// </summary>
            public const string Configuration = nameof(Configuration);

            /// <summary>
            /// Путь к карточкам типов документов: <see cref="Cards"/>\<see cref="DocumentTypes"/>.
            /// </summary>
            public const string DocumentTypes = nameof(DocumentTypes);

            /// <summary>
            /// Путь к карточкам календарей: <see cref="Cards"/>\<see cref="Calendars"/>.
            /// </summary>
            public const string Calendars = nameof(Calendars);

            /// <summary>
            /// Путь к карточкам типов условий: <see cref="Cards"/>\<see cref="ConditionTypes"/>.
            /// </summary>
            public const string ConditionTypes = nameof(ConditionTypes);

            /// <summary>
            /// Путь к карточкам маршрутов: <see cref="Cards"/>\<see cref="KrProcess"/>.
            /// </summary>
            public const string KrProcess = nameof(KrProcess);

            /// <summary>
            /// Путь к карточкам уведомлений: <see cref="Cards"/>\<see cref="Notifications"/>.
            /// </summary>
            public const string Notifications = nameof(Notifications);

            /// <summary>
            /// Путь к карточкам типов уведомлений: <see cref="Cards"/>\<see cref="NotificationTypes"/>.
            /// </summary>
            public const string NotificationTypes = nameof(NotificationTypes);

            /// <summary>
            /// Путь к карточкам настроек: <see cref="Cards"/>\<see cref="Settings"/>.
            /// </summary>
            public const string Settings = nameof(Settings);
        }

        /// <summary>
        /// Предоставляет пути к типам: <see cref="Resources"/>\<see cref="Types"/>.
        /// </summary>
        public static class Types
        {
            /// <summary>
            /// Путь к типам: <see cref="Resources"/>\<see cref="Types"/>.
            /// </summary>
            public const string Name = nameof(Types);

            /// <summary>
            /// Путь к типам карточек: <see cref="Types"/>\<see cref="Cards"/>.
            /// </summary>
            public const string Cards = nameof(Cards);

            /// <summary>
            /// Путь к типам карточек диалогов: <see cref="Types"/>\<see cref="Dialogs"/>.
            /// </summary>
            public const string Dialogs = nameof(Dialogs);

            /// <summary>
            /// Путь к типам файлов: <see cref="Types"/>\<see cref="Files"/>.
            /// </summary>
            public const string Files = nameof(Files);

            /// <summary>
            /// Путь к типам заданий: <see cref="Types"/>\<see cref="Tasks"/>.
            /// </summary>
            public const string Tasks = nameof(Tasks);
        }
    }
}
