using System.Threading.Tasks;
using Tessa.Platform.Settings;

namespace Tessa.Extensions.Default.Shared.Settings
{
    /// <summary>
    /// Расширение добавляет блок "Язык и форматирование" в настройки пользователя.
    /// </summary>
    public sealed class LanguageAndFormattingUserSettingsExtension :
        SettingsExtension
    {
        #region Base Overrides

        /// <inheritdoc/>
        public override Task Initialize(ISettingsExtensionContext context)
        {
            context.Settings.UserSettingsCardTypeIDList.Add(DefaultCardTypes.LanguageAndFormattingUserSettingsTypeID);
            return Task.CompletedTask;
        }

        #endregion
    }
}
