#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Caching;
using Tessa.Notices;
using Tessa.Platform.Placeholders;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Shared.Notices
{
    /// <summary>
    /// Вспомогательные средства для использования в уведомлениях.
    /// </summary>
    public static class DefaultNotificationHelper
    {
        #region Public Methods

        /// <summary>
        /// Формирует строку для параметра Name для использования в ссылках.
        /// </summary>
        /// <param name="digest">Дайджест.</param>
        /// <param name="fullNumber">Номер карточки.</param>
        /// <param name="typeCaption">Отображаемое название типа карточки.</param>
        /// <returns>Сформированная строка.</returns>
        public static string GetNameForLink(
            string? digest,
            string? fullNumber,
            string? typeCaption)
        {
            if (!string.IsNullOrEmpty(digest))
            {
                return digest;
            }

            if (!string.IsNullOrEmpty(fullNumber))
            {
                return fullNumber;
            }

            if (!string.IsNullOrEmpty(typeCaption))
            {
                return typeCaption;
            }

            return LocalizeName("UI_Common_DefaultDigest_Card");
        }

        public static void ModifyTaskCaption(NotificationEmail email, CardTask task)
        {
            ThrowIfNull(email);
            ThrowIfNull(task);

            if (task.Card.Sections.TryGetValue("TaskCommonInfo", out var section))
            {
                var kindCaption = section.RawFields.TryGet<string>("KindCaption");

                if (!string.IsNullOrWhiteSpace(kindCaption))
                {
                    email.PlaceholderAliases.SetReplacement("taskType", "f:TaskCommonInfo.KindCaption task");
                }
            }
        }

        public static Dictionary<string, object?> GetInfoWithTask(CardTask? task) =>
            new(StringComparer.Ordinal)
            {
                [PlaceholderHelper.TaskKey] = task
            };

        public static async ValueTask<string?> GetMobileApprovalEmailAsync(
            ICardCache cardCache,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(cardCache);

            var serverInstance = await cardCache.Cards.GetAsync(CardHelper.ServerInstanceTypeName, cancellationToken).ConfigureAwait(false);

            return serverInstance.IsSuccess
                ? serverInstance.GetValue().Sections["ServerInstances"].RawFields.Get<string>("MobileApprovalEmail")
                : null;
        }

        #endregion
    }
}
