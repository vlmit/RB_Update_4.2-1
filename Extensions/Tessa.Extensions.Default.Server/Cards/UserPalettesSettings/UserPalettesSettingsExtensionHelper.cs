namespace Tessa.Extensions.Default.Server.Cards.UserPalettesSettings
{
    /// <summary>
    /// Вспомогательный статический класс для расширений, которые синхронизирует системные палитры с пользовательскими в карточке пользователя.
    /// </summary>
    internal static class UserPalettesSettingsExtensionHelper
    {
        #region Constants

        /// <summary>
        /// Ключ для хранения информация о добавленных пользовательских палитрах.
        /// </summary>
        public const string InsertedCustomPalettesKey = "InsertedPalettes";

        /// <summary>
        /// Ключ для хранения информация об измененных пользовательских палитрах.
        /// </summary>
        public const string UpdatedCustomPalettesKey = "UpdatedPalettes";

        /// <summary>
        /// Ключ для хранения информация об удаленных пользовательских палитрах.
        /// </summary>
        public const string DeletedCustomPalettesKey = "DeletedPalettes";

        #endregion
    }
}
