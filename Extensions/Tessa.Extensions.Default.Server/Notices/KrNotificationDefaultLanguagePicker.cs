#nullable enable
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards.Caching;
using Tessa.Localization;
using Tessa.Notices;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.Notices
{
    /// <inheritdoc />
    public sealed class KrNotificationDefaultLanguagePicker(ICardCache cardCache) : INotificationDefaultLanguagePicker
    {
        #region Fields

        private readonly ICardCache cardCache = NotNullOrThrow(cardCache);

        #endregion

        #region INotificationDefaultLanguagePicker Implementation

        /// <inheritdoc />
        public async ValueTask<(string LanguageCode, string FormatName)> GetDefaultLanguageAsync(CancellationToken cancellationToken = default)
        {
            var krSettings = await this.cardCache.Cards.GetAsync("KrSettings", cancellationToken);

            string? languageCode = null;
            string? formatName = null;

            if (krSettings.IsSuccess)
            {
                var fields = krSettings.GetValue().Sections["KrSettings"].RawFields;
                languageCode = fields.Get<string>("NotificationsDefaultLanguageCode");
                formatName = fields.Get<string>("NotificationsDefaultFormatName");
            }

            if (string.IsNullOrEmpty(languageCode))
            {
                languageCode = LocalizationManager.EnglishLanguageCode;
            }

            if (string.IsNullOrEmpty(formatName))
            {
                formatName = LocalizationManager.EnglishLanguageCode;
            }

            return (languageCode, formatName);
        }

        #endregion
    }
}
