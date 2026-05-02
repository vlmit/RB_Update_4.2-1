#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Ganss.Xss;
using Tessa.Cards;
using Tessa.Cards.Caching;
using Tessa.Cards.Extensions.Templates;
using Tessa.Forums;
using Tessa.Forums.Models;
using Tessa.Forums.Notifications;
using Tessa.Notices;
using Tessa.Platform;
using Tessa.Platform.Data;
using Tessa.Platform.Runtime;
using Unity;

namespace Tessa.Extensions.Default.Server.Forums.Notifications
{
    /// <inheritdoc />
    public class TopicNotificationService(
        IDbScope dbScope,
        ISession session,
        ITopicParticipantsProvider topicParticipantsProvider,
        IBackgroundServiceQueue backgroundServiceQueue,
        INotificationManager notificationManager,
        ICardCache cardCache,
        [Dependency(nameof(ForumSerializationHelper.MessageHtmlSanitizer))] IHtmlSanitizer htmlSanitizer,
        [OptionalDependency] IForumUserNamingStrategy? forumUserNamingStrategy = null)
        : TopicNotificationServiceBase(dbScope)
    {
        #region Private Fields

        private readonly ISession session = NotNullOrThrow(session);

        private readonly ITopicParticipantsProvider topicParticipantsProvider = NotNullOrThrow(topicParticipantsProvider);

        private readonly IBackgroundServiceQueue backgroundServiceQueue = NotNullOrThrow(backgroundServiceQueue);

        private readonly INotificationManager notificationManager = NotNullOrThrow(notificationManager);

        private readonly ICardCache cardCache = NotNullOrThrow(cardCache);

        private readonly IHtmlSanitizer htmlSanitizer = NotNullOrThrow(htmlSanitizer);

        #endregion

        #region Base Overrides

        /// <inheritdoc />
        public override ValueTask NotifyUsersHaveBeenAddedAsync(
            IReadOnlyCollection<Guid>? users,
            TopicModel topicModel,
            Guid? mainCardId = null,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(topicModel);

            if (users is null || users.Count == 0)
            {
                return ValueTask.CompletedTask;
            }

            return this.backgroundServiceQueue.EnqueueAsync(async _ =>
            {
                // send notifications in any case
                var ct = CancellationToken.None;
                mainCardId ??= await ForumProviderStrategyHelper.GetMainCardIDAsync(topicModel.ID, this.DbScope, ct);
                await this.SendNotificationCoreAsync(
                    ForumNotifications.ForumBeingAddedNewParticipantNotification,
                    mainCardId.Value,
                    users.Where(u => u != this.session.User.ID).ToList(),
                    new Dictionary<string, object?>
                    {
                        {
                            "Topic", topicModel.GetStorage()
                        },
                        {
                            "WebLink", CardHelper.GetWebLink(
                                LinkHelper.NormalizeWebAddress(
                                    await CardHelper.TryGetWebAddressAsync(
                                        this.cardCache,
                                        cancellationToken: ct)),
                                mainCardId.Value,
                                normalize: false)
                        }
                    },
                    cancellationToken: ct);
            }, parallel: true);
        }

        /// <inheritdoc />
        public override ValueTask NotifyMessageHasBeenSentAsync(
            TopicModel topicModel,
            MessageModel messageModel,
            Guid? mainCardId = null,
            Guid? satelliteId = null,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(topicModel);
            ThrowIfNull(messageModel);

            return this.backgroundServiceQueue.EnqueueAsync(async _ =>
            {
                var mailMessage = messageModel.GetStyledMessageText();
                mailMessage = !string.IsNullOrEmpty(mailMessage)
                    ? this.htmlSanitizer.Sanitize(mailMessage).Trim()
                    : messageModel.Body.Text;

                // send notifications in any case
                var ct = CancellationToken.None;
                var notificationEmailInfo = EmailAttachmentsHelper.GetHtmlBodyInfo(mailMessage);
                EmailAttachmentsHelper.AttachInlineImages(notificationEmailInfo);
                EmailAttachmentsHelper.AddOtherAttachments(notificationEmailInfo, messageModel.Attachments);

                messageModel.Created = messageModel.Created!.Value.ToLocalTime();
                mainCardId ??= await ForumProviderStrategyHelper.GetMainCardIDAsync(topicModel.ID, this.DbScope, ct);
                satelliteId ??= await ForumProviderStrategyHelper.GetSatelliteIDAsync(topicModel.ID, this.DbScope, ct);
                var topicNotification = new TopicNotificationInfo
                {
                    CardID = mainCardId.Value,
                    TopicID = topicModel.ID,
                    TopicTitle = HttpUtility.HtmlEncode(topicModel.Title),
                    MessageDate = messageModel.Created,
                    Type = messageModel.Type,
                    Info = messageModel.Body.Info,
                    MessageID = messageModel.ID,
                    AuthorName = messageModel.AuthorName,
                    HtmlText = notificationEmailInfo.HtmlBody,
                    TopicDescription = topicModel.Description,
                    WebLink = CardHelper.GetWebLink(
                        LinkHelper.NormalizeWebAddress(
                            await CardHelper.TryGetWebAddressAsync(
                                this.cardCache,
                                cancellationToken: ct)),
                        mainCardId.Value,
                        normalize: false)
                };

                if (forumUserNamingStrategy is not null)
                {
                    await forumUserNamingStrategy.ReplaceAsync(new List<ITopicNotificationInfo>(1) { topicNotification }, ct);
                }

                var finalRecipients = await this.GetRecipientsAsync(topicModel, messageModel.Mentioned, ct);
                await this.SendNotificationCoreAsync(
                    ForumNotifications.ForumNewMessagesNotification,
                    mainCardId.Value,
                    finalRecipients,
                    new Dictionary<string, object?>
                    {
                        {
                            "TopicNotification", topicNotification.GetStorage()
                        },
                        {
                            "HasWeb", BooleanBoxes.Box(!string.IsNullOrEmpty(topicNotification.WebLink))
                        }
                    },
                    async (email, _) =>
                    {
                        if (notificationEmailInfo.FileInfos is not { Count: > 0 })
                        {
                            return;
                        }

                        email.MailInfo ??= new();

                        foreach (var fileInfo in notificationEmailInfo.FileInfos)
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
                    ct);
            }, parallel: true);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Returns final list of recipients form notification about new message.
        /// </summary>
        /// <param name="topicModel">Topic model.</param>
        /// <param name="mentionedUserIDs">List of mentioned users.</param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>List of recipients.</returns>
        protected virtual async ValueTask<List<Guid>> GetRecipientsAsync(
            TopicModel topicModel,
            IEnumerable<Guid> mentionedUserIDs,
            CancellationToken cancellationToken = default)
        {
            var topicParticipants = await this.topicParticipantsProvider.GetTopicParticipantsAsync(topicModel.ID, cancellationToken);
            var subscribedRecipients = topicParticipants.Users
                .Where(i => i.IsSubscribed)
                .Select(t => t.UserID);
            var subscribedRoleRecipients = topicParticipants.RoleUsers
                .SelectMany(r => r.Value
                    .Where(i => i.IsSubscribed)
                    .Select(i => i.UserID));
            var totalUnsubscribed = topicParticipants.Users
                .Where(u => !u.IsSubscribed)
                .Select(u => u.UserID)
                .Union(topicParticipants.RoleUsers
                    .SelectMany(r => r.Value
                        .Where(ru => !ru.IsSubscribed)
                        .Select(ru => ru.UserID)))
                .ToArray();
            var subscribedToAllTopics = await this.GetSubscribedToAllTopics(totalUnsubscribed, cancellationToken);
            return subscribedRecipients
                .Union(subscribedRoleRecipients)
                .Union(subscribedToAllTopics)
                .Except(mentionedUserIDs)
                .Where(u => u != this.session.User.ID)
                .ToList();
        }

        /// <summary>
        /// Sends email notification.
        /// </summary>
        /// <param name="notificationId">Id of the notification.</param>
        /// <param name="mainCardId">Id of main card for notification.</param>
        /// <param name="recipients">List of recipietns.</param>
        /// <param name="emailInfo">Additional information which is passed
        /// into placeholders replacement methods info.</param>
        /// <param name="modifyEmailFuncAsync">Function for email template modification.</param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        protected virtual async ValueTask SendNotificationCoreAsync(
            Guid notificationId,
            Guid mainCardId,
            List<Guid> recipients,
            Dictionary<string, object?> emailInfo,
            Func<NotificationEmail, CancellationToken, Task>? modifyEmailFuncAsync = null,
            CancellationToken cancellationToken = default)
        {
            var sendResult = await this.notificationManager.SendAsync(
                notificationId,
                recipients,
                new NotificationSendContext
                {
                    ExcludeDeputies = true,
                    MainCardID = mainCardId,
                    ModifyEmailActionAsync = modifyEmailFuncAsync,
                    Info = emailInfo,
                },
                cancellationToken);
            TessaLoggers.Web.LogResult(sendResult);
        }

        #endregion

        #region Private Methods

        private async ValueTask<List<Guid>> GetSubscribedToAllTopics(Guid[] usersToGet, CancellationToken cancellationToken = default)
        {
            if (usersToGet.Length == 0)
            {
                return new List<Guid>();
            }

            await using var _ = this.DbScope.Create();
            var db = this.DbScope.Db;
            var builder = await this.DbScope.GetBuilderFactoryAsync(cancellationToken);

            return await db
                .SetCommand(
                    builder
                        .Select()
                        .C("sat", "MainCardID")
                        .From("PersonalRoleSatellite", "p").NoLock()
                        .InnerJoin(CardSatelliteHelper.SatellitesSectionName, "sat").NoLock()
                        .On().C("sat", "ID").Equals().C("p", "ID")
                        .Where().C("sat", CardSatelliteHelper.MainCardIDColumn).InArray(usersToGet, "Users", out var usersParam)
                        .And().C("p", "SubscribeToAllTopics").Equals().V(true)
                        .Build(),
                    DataParameters.Get(usersParam))
                .LogCommand()
                .ExecuteListAsync<Guid>(cancellationToken);
        }

        #endregion
    }
}
