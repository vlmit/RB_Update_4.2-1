#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Caching;
using Tessa.Cards.Extensions.Templates;
using Tessa.Extensions.Default.Server.Forums.Notifications;
using Tessa.Forums;
using Tessa.Forums.Mentions;
using Tessa.Forums.Notifications;
using Tessa.Notices;
using Tessa.Platform;
using Tessa.Platform.Data;

namespace Tessa.Extensions.Default.Server.Forums.Mention
{
    /// <summary>
    /// Расширение на упоминание пользователей в обсуждении, которое отправляет почтовое уведомления
    /// упомянутым пользователям.
    /// </summary>
    /// <param name="backgroundServiceQueue"><inheritdoc cref="IBackgroundServiceQueue" path="/summary"/></param>
    /// <param name="cardCache"><inheritdoc cref="ICardCache" path="/summary"/></param>
    /// <param name="notificationManager"><inheritdoc cref="INotificationManager" path="/summary"/></param>
    public class NotifyUsersMentionExtension(
        IBackgroundServiceQueue backgroundServiceQueue,
        ICardCache cardCache,
        INotificationManager notificationManager) : ForumUserMentionExtension
    {
        #region Private Fields

        private readonly IBackgroundServiceQueue backgroundServiceQueue = NotNullOrThrow(backgroundServiceQueue);

        private readonly ICardCache cardCache = NotNullOrThrow(cardCache);

        private readonly INotificationManager notificationManager = NotNullOrThrow(notificationManager);

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async Task ProcessMention(IForumUserMentionExtensionContext context)
        {
            if (!context.ValidationResult.IsSuccessful())
            {
                return;
            }

            await this.backgroundServiceQueue.EnqueueAsync(async _ =>
            {
                var ct = CancellationToken.None;
                var htmlBodyInfo = EmailAttachmentsHelper.GetHtmlBodyInfo(context.MessageInfo.HtmlText);
                EmailAttachmentsHelper.AttachInlineImages(htmlBodyInfo);
                var satelliteId = await ForumProviderStrategyHelper.GetSatelliteIDAsync(context.TopicID, context.DbScope, ct);
                var notificationResult = await this.notificationManager.SendAsync(
                    ForumNotifications.ForumUserMentionNotification,
                    context.UserIDs,
                    new NotificationSendContext
                    {
                        ExcludeDeputies = true,
                        MainCardID = context.CardID,
                        Info = new()
                        {
                            [nameof(ForumUserMentionMessageInfo.Topic)] = context.MessageInfo.Topic,
                            [nameof(ForumUserMentionMessageInfo.HtmlText)] = htmlBodyInfo.HtmlBody,
                            [nameof(ForumUserMentionMessageInfo.MessageDate)] = context.MessageInfo.MessageDate?.ToLocalTime() ?? DateTime.Now,
                            [nameof(ForumUserMentionMessageInfo.AuthorName)] = context.MessageInfo.AuthorName,
                            ["WebLink"] = CardHelper.GetWebLink(
                                LinkHelper.NormalizeWebAddress(
                                    await CardHelper.TryGetWebAddressAsync(
                                        this.cardCache,
                                        cancellationToken: ct)),
                                context.CardID,
                                normalize: false)
                        },
                        ModifyEmailActionAsync = async (email, _) =>
                        {
                            if (htmlBodyInfo.FileInfos is not { Count: > 0 })
                            {
                                return;
                            }

                            email.MailInfo ??= new();

                            foreach (var fileInfo in htmlBodyInfo.FileInfos)
                            {
                                email.MailInfo.Files.Add(new MailFile
                                {
                                    FileID = fileInfo.FileID!.Value,
                                    FileName = fileInfo.FileName,
                                    VersionID = Guid.Empty,
                                    Info = new() { { EmailAttachmentsHelper.CidKey, fileInfo.Cid } },
                                    CardID = satelliteId
                                });
                            }
                        },
                    }, ct);
                context.ValidationResult.Add(notificationResult);
            }, parallel: true);
        }

        #endregion
    }
}
