using System;

namespace Tessa.Extensions.Default.Server.Web.DeskiMobile
{
    /// <summary>
    /// Перечисление разрешений на операцию с TESSA Assistant.
    /// </summary>
    [Flags]
    public enum DeskiMobileTokenPermissionFlags
    {
        /// <summary>
        /// Разрешения отсутствуют.
        /// </summary>
        None = 0,

        /// <summary>
        /// Разрешение на получение контента файла.
        /// </summary>
        GetContent = 1,

        /// <summary>
        /// Разрешение на получение сигнатур подписи файла.
        /// </summary>
        GetSignatures = 2,

        /// <summary>
        /// Разрешение на усовершенствование подписи.
        /// </summary>
        Enhance = 4,

        /// <summary>
        /// Разрешение на проверку подписи.
        /// </summary>
        Verify = 8,

        /// <summary>
        /// Разрешение на отмену операции.
        /// </summary>
        CancelOperation = 16
    }
}
