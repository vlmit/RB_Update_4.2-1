#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB;
using Tessa.Cards;
using Tessa.Cards.ComponentModel;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Shared.Workflow.KrPermissions;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Forums;
using Tessa.Forums.Models;
using Tessa.Platform.Data;
using Tessa.Platform.Licensing;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using static Tessa.Forums.FmSections;

namespace Tessa.Extensions.Default.Server.Forums
{
    public sealed class ForumProviderRequestExtension : CardRequestExtension
    {
        #region Fields

        private readonly IForumProviderStrategy forumProviderStrategy;

        private readonly ILicenseManager licenseManager;

        private readonly IForumPermissionsProvider permissionsProvider;

        private readonly IForumServerSettings forumServerSettings;

        private readonly Dictionary<Guid, Func<ICardRequestExtensionContext, Task>> funcByRequestType;

        private readonly IForumPermissionsDependencies forumPermissionsDependencies;

        private readonly ICardRepository cardRepository;

        private readonly IKrTypesCache krTypesCache;

        #endregion

        #region Factories

        private static readonly Func<Dictionary<string, object?>, ForumProviderGetTopicRequest> getTopicRequestFactory =
            p => new ForumProviderGetTopicRequest(p);

        private static readonly Func<Dictionary<string, object?>, ForumProviderCheckPermissionRequest> checkPermissionRequestFactory =
            p => new ForumProviderCheckPermissionRequest(p);

        private static readonly Func<Dictionary<string, object?>, ForumProviderArchiveTopicRequest> archiveTopicRequestFactory =
            p => new ForumProviderArchiveTopicRequest(p);

        private static readonly Func<Dictionary<string, object?>, ForumProviderSetForumSettingsRequest> setForumSettingsRequestFactory =
            p => new ForumProviderSetForumSettingsRequest(p);

        private static readonly Func<Dictionary<string, object?>, ForumProviderGetTopicsWithMessagesRequest> getTopicsWithMessagesRequestFactory =
            p => new ForumProviderGetTopicsWithMessagesRequest(p);

        private static readonly Func<Dictionary<string, object?>, ForumProviderSendMessageRequest> sendMessageRequestFactory =
            p => new ForumProviderSendMessageRequest(p);

        private static readonly Func<Dictionary<string, object?>, ForumProviderAddTopicRequest> addTopicRequestFactory =
            p => new ForumProviderAddTopicRequest(p);

        private static readonly Func<Dictionary<string, object?>, ForumProviderGetMessagesRequest> getMessagesRequestFactory =
            p => new ForumProviderGetMessagesRequest(p);

        private static readonly Func<Dictionary<string, object?>, ForumProviderParticipantsRequest> participantsRequestFactory =
            p => new ForumProviderParticipantsRequest(p);

        private static readonly Func<Dictionary<string, object?>, ForumProviderSubscribeRequest> subscribeRequestFactory =
            p => new ForumProviderSubscribeRequest(p);

        private static readonly Func<Dictionary<string, object?>, ForumProviderGetSatelliteIDRequest> getSatelliteIDRequestFactory =
            p => new ForumProviderGetSatelliteIDRequest(p);

        #endregion

        #region Constructors

        public ForumProviderRequestExtension(
            ILicenseManager licenseManager,
            IForumPermissionsProvider permissionsProvider,
            IForumServerSettings forumServerSettings,
            IForumProviderStrategy forumProviderStrategy,
            IForumPermissionsDependencies forumPermissionsDependencies,
            ICardRepository cardRepository,
            IKrTypesCache krTypesCache)
        {
            this.licenseManager = NotNullOrThrow(licenseManager);
            this.permissionsProvider = NotNullOrThrow(permissionsProvider);
            this.forumServerSettings = NotNullOrThrow(forumServerSettings);
            this.forumProviderStrategy = NotNullOrThrow(forumProviderStrategy);
            this.forumPermissionsDependencies = NotNullOrThrow(forumPermissionsDependencies);
            this.cardRepository = NotNullOrThrow(cardRepository);
            this.krTypesCache = NotNullOrThrow(krTypesCache);

            this.funcByRequestType = new Dictionary<Guid, Func<ICardRequestExtensionContext, Task>>
            {
                { ForumRequestTypes.AddMessage, this.AddMessageAsync },
                { ForumRequestTypes.UpdateMessage, this.UpdateMessageAsync },
                { ForumRequestTypes.GetTopic, this.GetTopicAsync },
                { ForumRequestTypes.GetMessages, this.GetMessagesAsync },
                { ForumRequestTypes.AddTopic, this.AddTopicAsync },
                { ForumRequestTypes.AddParticipants, this.AddParticipantsAsync },
                { ForumRequestTypes.AddRoles, this.AddRolesAsync },
                { ForumRequestTypes.GetTopicsWithMessages, this.GetTopicsWithMessagesAsync },
                { ForumRequestTypes.Subscribe, this.SubscribeAsync },
                { ForumRequestTypes.CheckPermission, this.CheckPermissionAsync },
                { ForumRequestTypes.ArchiveTopic, this.ArchiveTopicAsync },
                { ForumRequestTypes.SetForumSettings, this.SetForumSettingsAsync },
                { ForumRequestTypes.RemoveParticipants, this.RemoveParticipantsAsync },
                { ForumRequestTypes.RemoveRoles, this.RemoveRolesAsync },
                { ForumRequestTypes.UpdateParticipants, this.UpdateParticipantsAsync },
                { ForumRequestTypes.UpdateRoles, this.UpdateRolesAsync },
                { ForumRequestTypes.GetSatelliteID, this.GetSatelliteIDAsync },
            };
        }

        #endregion

        #region Private Methods

        private async Task AddParticipantsAsync(ICardRequestExtensionContext context)
        {
            ThrowIfNull(context.DbScope);

            var requestObject = NotNullOrThrow(context.Request.TryGetForumRequest(participantsRequestFactory));
            if (context.Request.ServiceType != CardServiceType.Default)
            {
                requestObject.ServiceMessageMode = ForumServiceMessageMode.Enabled;
            }

            var mainCardID = await ForumProviderStrategyHelper.GetMainCardIDAsync(requestObject.TopicID, context.DbScope, context.CancellationToken);

            var (participant, result) = await this.permissionsProvider.ResolveUserPermissionsAsync(
                requestObject.TopicID,
                mainCardID,
                checkElevatedPermissions: true,
                cancellationToken: context.CancellationToken);

            if (participant is null || !result.IsSuccessful)
            {
                context.ValidationResult.Add(result);
                return;
            }

            // обычный участник может добавить только обычного участника;
            // модератор может добавить обычного участника или модератора;
            // супермодератор может добавить кого угодно
            var currentUserType = participant.Type;
            var targetType = requestObject.Type ?? ParticipantType.Participant;
            var hasPermissions = currentUserType switch
            {
                ParticipantType.Participant => targetType == ParticipantType.Participant,
                ParticipantType.Moderator => targetType is ParticipantType.Participant or ParticipantType.Moderator,
                ParticipantType.SuperModerator => true,
                ParticipantType.ParticipantFromRole => throw new NotSupportedException(),
                _ => throw ArgumentOutOfRange(currentUserType)
            };

            if (!hasPermissions)
            {
                context.ValidationResult.Add(ForumValidationKeys.PermissionError, ValidationResultType.Error,
                    await LocalizeAsync("$Forum_ValidationMessage_PermissionError_AddingParticipant") ?? string.Empty);
                return;
            }

            var (response, addParticipantResult) = await this.forumProviderStrategy.AddParticipantsAsync(
                requestObject.TopicID,
                requestObject.Participants,
                requestObject.IsReadOnly ?? false,
                requestObject.CanEditMessages ?? true,
                requestObject.Type ?? ParticipantType.Participant,
                isSubscribed: requestObject.IsSubscribed ?? false,
                serviceMessageMode: requestObject.ServiceMessageMode,
                cancellationToken: context.CancellationToken);

            context.ValidationResult.Add(addParticipantResult);
            context.Response!.SetForumResponse(response);
        }

        private async Task AddRolesAsync(ICardRequestExtensionContext context)
        {
            ThrowIfNull(context.DbScope);

            var requestObject = NotNullOrThrow(context.Request.TryGetForumRequest(participantsRequestFactory));
            if (context.Request.ServiceType != CardServiceType.Default)
            {
                requestObject.ServiceMessageMode = ForumServiceMessageMode.Enabled;
            }

            var mainCardID = await ForumProviderStrategyHelper.GetMainCardIDAsync(requestObject.TopicID, context.DbScope, context.CancellationToken);

            // Добавлять роль может только супер модератор
            var (isSuperModerator, _, result) =
                await this.permissionsProvider.CheckElevatedPermissionsAsync(mainCardID, null, context.CancellationToken);

            if (!isSuperModerator)
            {
                context.ValidationResult.Add(result);
                context.Response!.SetForumResponse(new ForumResponse());
                return;
            }

            result = await this.forumProviderStrategy.AddRolesAsync(
                requestObject.TopicID,
                requestObject.Participants.Distinct().ToList(),
                requestObject.IsReadOnly ?? false,
                requestObject.CanEditMessages ?? true,
                requestObject.IsSubscribed ?? false,
                requestObject.ServiceMessageMode,
                context.CancellationToken);

            context.ValidationResult.Add(result);
        }

        private async Task UpdateRolesAsync(ICardRequestExtensionContext context)
        {
            ThrowIfNull(context.DbScope);

            var requestObject = NotNullOrThrow(context.Request.TryGetForumRequest(participantsRequestFactory));
            var mainCardID = await ForumProviderStrategyHelper.GetMainCardIDAsync(requestObject.TopicID, context.DbScope, context.CancellationToken);

            // Добавлять роль может только супер модератор
            var (isSuperModerator, _, result) =
                await this.permissionsProvider.CheckElevatedPermissionsAsync(mainCardID, null, context.CancellationToken);

            if (!isSuperModerator)
            {
                context.ValidationResult.Add(result);
                return;
            }

            result = await this.forumProviderStrategy.UpdateRolesAsync(
                requestObject.TopicID,
                requestObject.Participants.Distinct().ToList(),
                requestObject.IsReadOnly,
                requestObject.CanEditMessages,
                requestObject.IsSubscribed,
                context.CancellationToken);

            context.ValidationResult.Add(result);
        }

        private async Task UpdateParticipantsAsync(ICardRequestExtensionContext context)
        {
            ThrowIfNull(context.DbScope);

            var requestObject = NotNullOrThrow(context.Request.TryGetForumRequest(participantsRequestFactory));
            var mainCardID = await ForumProviderStrategyHelper.GetMainCardIDAsync(requestObject.TopicID, context.DbScope, context.CancellationToken);

            var (participant, result) = await this.permissionsProvider.ResolveUserPermissionsAsync(
                requestObject.TopicID,
                mainCardID,
                checkElevatedPermissions: true,
                cancellationToken: context.CancellationToken);

            if (participant is null || !result.IsSuccessful)
            {
                context.ValidationResult.Add(result);
                return;
            }

            // обычный участник не может изменять никого (даже себе не может поменять readonly);
            // модератор может изменять обычных участников и других модераторов (но не может понижать модераторов - проверка ниже);
            // супермодератор может изменять кого угодно на что угодно
            var currentUserType = participant.Type;
            var targetType = requestObject.Type;
            var hasPermissions = currentUserType switch
            {
                ParticipantType.Participant => false,
                ParticipantType.Moderator => targetType is null or ParticipantType.Participant or ParticipantType.Moderator,
                ParticipantType.SuperModerator => true,
                ParticipantType.ParticipantFromRole => false,
                _ => throw ArgumentOutOfRange(currentUserType)
            };

            if (!hasPermissions)
            {
                context.ValidationResult.Add(ForumValidationKeys.PermissionError,
                    ValidationResultType.Error,
                    await LocalizeNameAsync("Forum_ValidationMessage_PermissionError_UpdatingParticipant"));
                return;
            }

            foreach (var updatingParticipantID in requestObject.Participants)
            {
                var updatingParticipantType =
                    await TryGetDirectParticipantTypeIDAsync(
                        context.DbScope, updatingParticipantID, requestObject.TopicID, context.CancellationToken);

                if (updatingParticipantType is not { } sourceType)
                {
                    continue;
                }

                // обычный участник сюда не доходит;
                // модератор может изменять тех, кто был обычными участниками или модераторами, но не может понижать модератора;
                // супермодератор может изменять кого угодно
                var hasModifyPermissions = currentUserType switch
                {
                    ParticipantType.Participant => throw new NotSupportedException(),
                    ParticipantType.Moderator => sourceType == ParticipantType.Participant
                        || sourceType == ParticipantType.Moderator && targetType != ParticipantType.Participant,
                    ParticipantType.SuperModerator => true,
                    ParticipantType.ParticipantFromRole => throw new NotSupportedException(),
                    _ => throw ArgumentOutOfRange(currentUserType)
                };

                if (!hasModifyPermissions)
                {
                    context.ValidationResult.Add(ForumValidationKeys.PermissionError,
                        ValidationResultType.Error,
                        await LocalizeNameAsync("Forum_ValidationMessage_PermissionError_UpdatingParticipant"));
                    return;
                }
            }

            result = await this.forumProviderStrategy.UpdateParticipantsAsync(
                requestObject.TopicID,
                requestObject.Participants,
                requestObject.IsReadOnly,
                requestObject.CanEditMessages,
                requestObject.IsSubscribed,
                requestObject.Type,
                context.CancellationToken);

            context.ValidationResult.Add(result);
        }

        private async Task SubscribeAsync(ICardRequestExtensionContext context)
        {
            ThrowIfNull(context.DbScope);

            var requestObject = NotNullOrThrow(context.Request.TryGetForumRequest(subscribeRequestFactory));
            var mainCardID = await ForumProviderStrategyHelper.GetMainCardIDAsync(requestObject.TopicID, context.DbScope, context.CancellationToken);

            var (participant, accessResult) = await this.permissionsProvider.ResolveUserPermissionsAsync(
                requestObject.TopicID,
                mainCardID,
                checkElevatedPermissions: true,
                cancellationToken: context.CancellationToken);

            if (participant is null || !accessResult.IsSuccessful)
            {
                context.ValidationResult.Add(accessResult);
                return;
            }

            var result = await this.forumProviderStrategy.SubscribeAsync(
                requestObject.TopicID,
                requestObject.IsSubscribed,
                context.CancellationToken);

            context.ValidationResult.Add(result);
        }

        private async Task GetTopicAsync(ICardRequestExtensionContext context)
        {
            var requestObject = NotNullOrThrow(context.Request.TryGetForumRequest(getTopicRequestFactory));
            var (response, result) = await this.forumProviderStrategy.GetTopicAsync(
                requestObject.SingletonMode ? requestObject.CardID : requestObject.TopicID,
                requestObject.IsSuperModerator,
                requestObject.PermissionsToken,
                requestObject.SingletonMode,
                requestObject.TypeID,
                context.CancellationToken);

            if (result.IsSuccessful)
            {
                var topic = response.TryGetInfo()?.TryGet<Dictionary<string, object?>>(ForumProvider.TopicModelKey) is { } dictionary
                    ? new TopicModel(dictionary)
                    : null;

                if (topic is not null)
                {
                    // если удалось получить сообщения, то в response добавляем признаки редактирования
                    await this.CalculatePermissionEditMessagesAsync(
                        response.Info,
                        context.ValidationResult,
                        context.DbScope!,
                        topic.ID,
                        requestObject.PermissionsToken,
                        context.CancellationToken);
                }
            }

            context.ValidationResult.Add(result);
            context.Response!.SetForumResponse(response);
        }

        private async Task AddMessageAsync(ICardRequestExtensionContext context)
        {
            ThrowIfNull(context.DbScope);

            var requestObject = NotNullOrThrow(context.Request.TryGetForumRequest(sendMessageRequestFactory));
            if (context.Request.ServiceType != CardServiceType.Default)
            {
                requestObject.Message.Type = MessageType.Default;
            }

            var isSuperModerator = false;
            if (requestObject.HasElevatedPermissions)
            {
                var mainCardID = await ForumProviderStrategyHelper.GetMainCardIDAsync(requestObject.TopicID, context.DbScope, context.CancellationToken);
                (isSuperModerator, _, _) =
                    await this.permissionsProvider.CheckElevatedPermissionsAsync(
                        mainCardID,
                        null,
                        context.CancellationToken);
            }

            var (response, result) = await this.forumProviderStrategy.SendMessageAsync(
                requestObject.TopicID,
                requestObject.Message,
                isSuperModerator,
                context.CancellationToken);

            context.ValidationResult.Add(result);
            context.Response!.SetForumResponse(response);
        }

        private async Task UpdateMessageAsync(ICardRequestExtensionContext context)
        {
            ThrowIfNull(context.DbScope);

            var requestObject = NotNullOrThrow(context.Request.TryGetForumRequest(sendMessageRequestFactory));
            var actualMessage = await this.forumProviderStrategy.TryGetMessageInfoAsync(requestObject.Message.ID);
            if (actualMessage is null)
            {
                context.ValidationResult.AddError("$Forum_ValidationMessage_MessageNotFoundForEdit");
                return;
            }

            // из исходного сообщения переносим только тело и вложения, всё остальное берём из БД
            var sourceMessage = requestObject.Message;
            actualMessage.Body = sourceMessage.Body;
            actualMessage.Attachments = sourceMessage.Attachments;

            // свойства Modified*** всегда заполняются из текущей сессии в ForumProviderStrategy
            requestObject.Message = actualMessage;

            var cardID = await ForumProviderStrategyHelper.GetMainCardIDAsync(requestObject.TopicID, context.DbScope, context.CancellationToken);
            var canEditAllMessages = false;
            if (requestObject.HasElevatedPermissions)
            {
                (_, canEditAllMessages, _) =
                    await this.permissionsProvider.CheckElevatedPermissionsAsync(
                        cardID,
                        null,
                        context.CancellationToken);
            }

            //проверить права для изменения изменения,
            var (modifyOwnMessageAt, vr) = await this.CheckPermissionsEditMessagesAsync(
                context.Session.User.ID,
                cardID,
                requestObject.TopicID,
                requestObject.Message.AuthorID,
                context.CancellationToken);

            if (!vr.IsSuccessful)
            {
                context.ValidationResult.Add(vr);
                return;
            }

            var (editMessageResult, result) =
                await IsEnableEditMessageAsync(context, modifyOwnMessageAt, requestObject.Message, canEditAllMessages);

            if (!editMessageResult)
            {
                context.ValidationResult.Add(result);
                return;
            }

            var (response, updateMessageResult) = await this.forumProviderStrategy.UpdateMessageAsync(
                requestObject.TopicID,
                requestObject.Message,
                canEditAllMessages,
                context.CancellationToken);

            context.ValidationResult.Add(updateMessageResult);
            context.Response!.SetForumResponse(response);
        }

        private static async ValueTask<(bool success, ValidationResult result)> IsEnableEditMessageAsync(
            ICardExtensionContext context,
            DateTime? modifyOwnMessageAtNoOlderThan,
            MessageModelBase messageModel,
            bool canEditAllMessages)
        {
            // если null нельзя редактировать
            // если 0 можно редактировать всегда, так как modifyOwnMessageAtNoOlderThan == DateTime.MinDate
            var validationResult = new ValidationResultBuilder();

            if (!modifyOwnMessageAtNoOlderThan.HasValue)
            {
                // Редактирование всех и своих сообщений запрещено
                validationResult.Add(
                    ForumValidationKeys.UpdateMessage,
                    ValidationResultType.Error,
                    await LocalizeNameAsync("Forum_ValidationKey_EditingMessagesIsProhibited"));
                return (false, validationResult.Build());
            }

            if (canEditAllMessages)
            {
                return (true, ValidationResult.Empty);
            }

            if (messageModel.AuthorID != context.Session.User.ID)
            {
                // Попытка изменить не своё сообщение
                validationResult.Add(ForumValidationKeys.UpdateMessage,
                    ValidationResultType.Error,
                    await LocalizeNameAsync("Forum_ValidationKey_NoPermissionToChangeOtherMessages"));
                return (false, validationResult.Build());
            }

            if (messageModel.Created <= modifyOwnMessageAtNoOlderThan.Value)
            {
                // Время на изменение своих сообщений прошло
                validationResult.Add(ForumValidationKeys.UpdateMessage,
                    ValidationResultType.Error,
                    await LocalizeNameAsync("Forum_ValidationKey_TimeToChangeYourMessagesHasPassed"));
                return (false, validationResult.Build());
            }

            return (true, ValidationResult.Empty);
        }

        private async Task AddTopicAsync(ICardRequestExtensionContext context)
        {
            var requestObject = NotNullOrThrow(context.Request.TryGetForumRequest(addTopicRequestFactory));
            if (context.Request.ServiceType != CardServiceType.Default)
            {
                requestObject.AuthorAction = TopicAuthorAction.AddParticipantAndSubscribe;
            }

            var (permissionAddTopic, vrItemAddTopic) = await this.permissionsProvider.CheckAddTopicPermissionAsync(
                requestObject.CardID,
                null,
                context.CancellationToken);

            // если нет прав доступа на добавление топика, то проверяем есть ли права на супермодератора.
            if (!permissionAddTopic)
            {
                var (permissionIsSuperModerator, _, vrItemIsSuperModerator) =
                    await this.permissionsProvider.CheckElevatedPermissionsAsync(requestObject.CardID, null, context.CancellationToken);

                if (!permissionIsSuperModerator)
                {
                    // значит нет прав на добавление топика и нет прав супер модератора. выходим из метода с VR
                    context.ValidationResult.Add(vrItemAddTopic);
                    context.ValidationResult.Add(vrItemIsSuperModerator);
                    return;
                }
            }

            var (response, result) =
                await this.forumProviderStrategy.AddTopicAsync(requestObject.CardID, requestObject.Topic, requestObject.AuthorAction,
                    context.CancellationToken);

            context.ValidationResult.Add(result);
            context.Response!.SetForumResponse(response);
        }

        private async Task GetMessagesAsync(ICardRequestExtensionContext context)
        {
            ThrowIfNull(context.DbScope);

            var requestObject = NotNullOrThrow(context.Request.TryGetForumRequest(getMessagesRequestFactory));
            var isSuperModerator = false;
            if (requestObject.IsSuperModerator)
            {
                var cardID = await ForumProviderStrategyHelper.GetMainCardIDAsync(requestObject.TopicID, context.DbScope, context.CancellationToken);
                (isSuperModerator, _, _) =
                    await this.permissionsProvider.CheckElevatedPermissionsAsync(
                        cardID,
                        null,
                        context.CancellationToken);
            }

            var (response, result) = await this.forumProviderStrategy.GetMessagesAsync(
                requestObject.TopicID,
                isSuperModerator,
                requestObject.LimitMessages,
                requestObject.PageNumber,
                requestObject.MessageID,
                requestObject.LastReadMessageTime,
                requestObject.IsNeedUpdateLastReadMessageTime,
                requestObject.Query,
                requestObject.ReverseOrder,
                context.CancellationToken);

            context.ValidationResult.Add(result);
            context.Response!.SetForumResponse(response);
        }

        private async Task<(DateTime? ModifyOwnMessageAt, ValidationResult Vr)> CheckPermissionsEditMessagesAsync(
            Guid userID,
            Guid cardID,
            Guid topicID,
            Guid messageAuthorID,
            CancellationToken cancellationToken = default)
        {
            var modifyOwnMessageAtNoOlderThan = await this.forumServerSettings.GetModifyMessageAtNoOlderThanAsync(cancellationToken);
            var (canEdit, validationResult) = await this.permissionsProvider.CheckEditMessagesPermissionAsync(
                topicID,
                cardID,
                isMyMessage: userID == messageAuthorID,
                cancellationToken: cancellationToken);
            var modifyOwnMessageAt = canEdit && modifyOwnMessageAtNoOlderThan is not null
                ? modifyOwnMessageAtNoOlderThan == 0
                    ? DateTime.MinValue
                    : DateTime.UtcNow.AddMinutes(-modifyOwnMessageAtNoOlderThan.Value)
                : (DateTime?) null;

            return (modifyOwnMessageAt, validationResult);
        }


        private async Task CalculatePermissionEditMessagesAsync(
            Dictionary<string, object?> info,
            IValidationResultBuilder validationResult,
            IDbScope dbScope,
            Guid topicID,
            Dictionary<string, object?>? token,
            CancellationToken cancellationToken = default)
        {
            var krTokenProvider = this.forumPermissionsDependencies.KrTokenProvider;
            var cardID = await ForumProviderStrategyHelper.GetMainCardIDAsync(topicID, dbScope, cancellationToken);
            Dictionary<string, object?> krToken;
            bool canEdit;

            if (await this.cardRepository.GetTypeIDAsync(cardID, CardInstanceType.Card, cancellationToken) is { } cardTypeID &&
                await KrComponentsHelper.HasBaseAsync(cardTypeID, this.krTypesCache, cancellationToken))
            {
                // Карточка входит в типовое решение.
                (canEdit, _) = await this.permissionsProvider.CheckEditMessagesPermissionAsync(
                    topicID,
                    isMyMessage: true,
                    permissionsToken: token,
                    cancellationToken: cancellationToken);

                if (!canEdit)
                {
                    krToken = krTokenProvider.CreateToken(
                        cardID,
                        CardComponentHelper.DoNotCheckVersion,
                        await this.forumPermissionsDependencies.KrPermissionsCacheContainer.GetVersionAsync(cancellationToken),
                        [],
                        modifyTokenAction: t =>
                        {
                            if (t.Info is { } tokenInfo)
                            {
                                tokenInfo[nameof(topicID)] = topicID;
                            }
                        }).GetStorage();
                }
                else
                {
                    krToken = krTokenProvider.CreateToken(
                        cardID,
                        CardComponentHelper.DoNotCheckVersion,
                        await this.forumPermissionsDependencies.KrPermissionsCacheContainer.GetVersionAsync(cancellationToken),
                        [KrPermissionFlagDescriptors.EditMyMessages],
                        modifyTokenAction: t =>
                        {
                            if (t.Info is { } tokenInfo)
                            {
                                tokenInfo[nameof(topicID)] = topicID;
                            }
                        }).GetStorage();
                }
            }
            else
            {
                // Rарточка не входит в типовое решение, возвращаем в токене права на редактирование.
                krToken = krTokenProvider.CreateToken(
                    cardID,
                    CardComponentHelper.DoNotCheckVersion,
                    await this.forumPermissionsDependencies.KrPermissionsCacheContainer.GetVersionAsync(cancellationToken),
                    [KrPermissionFlagDescriptors.EditMyMessages],
                    modifyTokenAction: t =>
                    {
                        if (t.Info is { } info2)
                        {
                            info2[nameof(topicID)] = topicID;
                        }
                    }).GetStorage();
                canEdit = true;
            }

            if (canEdit)
            {
                var modifyOwnMessageAtNoOlderThan = await this.forumServerSettings.GetModifyMessageAtNoOlderThanAsync(cancellationToken);
                if (modifyOwnMessageAtNoOlderThan is not null)
                {
                    info[ForumProvider.ModifyOwnMessageAtNoOlderThanKey] = modifyOwnMessageAtNoOlderThan == 0
                        ? DateTime.MinValue
                        : DateTime.UtcNow.AddMinutes(-modifyOwnMessageAtNoOlderThan.Value);
                }
            }

            info[ForumProvider.KrTokenKey] = krToken;
        }

        private async Task GetTopicsWithMessagesAsync(ICardRequestExtensionContext context)
        {
            var requestObject = NotNullOrThrow(context.Request.TryGetForumRequest(getTopicsWithMessagesRequestFactory));
            var isSuperModerator = false;
            if (requestObject.IsSuperModerator)
            {
                (isSuperModerator, _, _) =
                    await this.permissionsProvider.CheckElevatedPermissionsAsync(requestObject.CardID, null, context.CancellationToken);
            }

            var (response, result) = await this.forumProviderStrategy.GetTopicsWithMessagesAsync(
                requestObject.CardID,
                isSuperModerator,
                requestObject.MessagesInTopicCount,
                requestObject.LastDate,
                requestObject.TypeID,
                context.CancellationToken);

            context.ValidationResult.Add(result);
            context.Response!.SetForumResponse(response);
        }

        private async Task CheckPermissionAsync(ICardRequestExtensionContext context)
        {
            ThrowIfNull(context.DbScope);

            var requestObject = NotNullOrThrow(context.Request.TryGetForumRequest(checkPermissionRequestFactory));
            var isSuperModerator = false;
            var topicID = requestObject.TopicID == Guid.Empty
                ? await ForumProviderStrategyHelper.GetTopicIDByFileIDAsync(context.DbScope, requestObject.FileID, context.CancellationToken)
                : requestObject.TopicID;

            if (requestObject.IsSuperModerator)
            {
                var mainCardID = await ForumProviderStrategyHelper.GetMainCardIDAsync(topicID, context.DbScope, context.CancellationToken);
                (isSuperModerator, _, _) =
                    await this.permissionsProvider.CheckElevatedPermissionsAsync(
                        mainCardID,
                        null,
                        context.CancellationToken);
            }

            var result = await this.forumProviderStrategy.CheckPermissionByFileAsync(
                topicID,
                requestObject.FileID,
                isSuperModerator,
                context.CancellationToken);

            context.ValidationResult.Add(result);
        }

        private async Task ArchiveTopicAsync(ICardRequestExtensionContext context)
        {
            ThrowIfNull(context.DbScope);

            var requestObject = NotNullOrThrow(context.Request.TryGetForumRequest(archiveTopicRequestFactory));
            var mainCardID = await ForumProviderStrategyHelper.GetMainCardIDAsync(requestObject.TopicID, context.DbScope, context.CancellationToken);

            var (participant, result) = await this.permissionsProvider.ResolveUserPermissionsAsync(
                requestObject.TopicID,
                mainCardID,
                checkElevatedPermissions: true,
                cancellationToken: context.CancellationToken);

            if (participant is null || !result.IsSuccessful)
            {
                context.ValidationResult.Add(result);
                return;
            }

            // выводить топики из архива может только супермодератор, а архивировать модератор и супермодератор
            if (requestObject.IsArchived && participant.Type is ParticipantType.Moderator or ParticipantType.SuperModerator)
            {
                result = await this.forumProviderStrategy.ArchiveTopicAsync(requestObject.TopicID, true, context.CancellationToken);
            }
            else if (!requestObject.IsArchived && participant.Type == ParticipantType.SuperModerator)
            {
                result = await this.forumProviderStrategy.ArchiveTopicAsync(requestObject.TopicID, false, context.CancellationToken);
            }
            else
            {
                context.ValidationResult.Add(ForumValidationKeys.ArchiveTopic,
                    ValidationResultType.Error,
                    ForumValidationKeys.ArchiveTopic.Message ?? string.Empty);
            }

            context.ValidationResult.Add(result);
        }

        private async Task SetForumSettingsAsync(ICardRequestExtensionContext context)
        {
            var requestObject = NotNullOrThrow(context.Request.TryGetForumRequest(setForumSettingsRequestFactory));
            var result = await this.forumProviderStrategy.SetForumSettingsAsync(requestObject.ForumSettings, context.CancellationToken);

            context.ValidationResult.Add(result);
        }

        private async Task RemoveRolesAsync(ICardRequestExtensionContext context)
        {
            ThrowIfNull(context.DbScope);

            var requestObject = NotNullOrThrow(context.Request.TryGetForumRequest(participantsRequestFactory));
            if (context.Request.ServiceType != CardServiceType.Default)
            {
                requestObject.ServiceMessageMode = ForumServiceMessageMode.Enabled;
            }

            var mainCardID = await ForumProviderStrategyHelper.GetMainCardIDAsync(requestObject.TopicID, context.DbScope, context.CancellationToken);

            var (participant, result) = await this.permissionsProvider.ResolveUserPermissionsAsync(
                requestObject.TopicID,
                mainCardID,
                checkElevatedPermissions: true,
                cancellationToken: context.CancellationToken);

            if (participant is null || participant.Type != ParticipantType.SuperModerator)
            {
                context.ValidationResult.Add(result);
                return;
            }

            var (response, removeRolesResult) =
                await this.forumProviderStrategy.RemoveRolesAsync(
                    requestObject.TopicID, requestObject.Participants /*roles*/,
                    requestObject.ServiceMessageMode, context.CancellationToken);

            context.ValidationResult.Add(removeRolesResult);
            context.Response!.SetForumResponse(response);
        }

        private async Task RemoveParticipantsAsync(ICardRequestExtensionContext context)
        {
            ThrowIfNull(context.DbScope);

            var requestObject = NotNullOrThrow(context.Request.TryGetForumRequest(participantsRequestFactory));
            if (context.Request.ServiceType != CardServiceType.Default)
            {
                requestObject.ServiceMessageMode = ForumServiceMessageMode.Enabled;
            }

            var mainCardID = await ForumProviderStrategyHelper.GetMainCardIDAsync(requestObject.TopicID, context.DbScope, context.CancellationToken);

            var (participant, result) = await this.permissionsProvider.ResolveUserPermissionsAsync(
                requestObject.TopicID,
                mainCardID,
                true,
                cancellationToken: context.CancellationToken);

            if (participant is null || !result.IsSuccessful)
            {
                context.ValidationResult.Add(result);
                return;
            }

            foreach (var removingParticipantID in requestObject.Participants)
            {
                var removingParticipantType =
                    await TryGetDirectParticipantTypeIDAsync(
                        context.DbScope, removingParticipantID, requestObject.TopicID, context.CancellationToken);

                if (removingParticipantType is null)
                {
                    continue;
                }

                // модератор может удалить другого участника или модератора;
                // супермодератор может удалять кого угодно
                var currentUserType = participant.Type;
                var hasRemovePermissions = currentUserType switch
                {
                    ParticipantType.Participant => participant.UserID == removingParticipantID ? true : throw new NotSupportedException(),
                    ParticipantType.Moderator => removingParticipantType is ParticipantType.Participant or ParticipantType.Moderator,
                    ParticipantType.SuperModerator => true,
                    ParticipantType.ParticipantFromRole => throw new NotSupportedException(),
                    _ => throw ArgumentOutOfRange(currentUserType)
                };

                if (!hasRemovePermissions)
                {
                    context.Response?.ValidationResult.Add(ForumValidationKeys.PermissionError,
                        ValidationResultType.Error,
                        await LocalizeNameAsync("Forum_ValidationMessage_PermissionError_RemovingParticipant"));
                    context.Response!.SetForumResponse(new ForumResponse());
                    return;
                }
            }

            var (response, removeParticipantResult) =
                await this.forumProviderStrategy.RemoveParticipantsAsync(
                    requestObject.TopicID, requestObject.Participants,
                    requestObject.ServiceMessageMode, context.CancellationToken);

            context.ValidationResult.Add(removeParticipantResult);
            context.Response!.SetForumResponse(response);
        }

        private async Task GetSatelliteIDAsync(ICardRequestExtensionContext context)
        {
            var requestObject = NotNullOrThrow(context.Request.TryGetForumRequest(getSatelliteIDRequestFactory));

            var (response, result) = await this.forumProviderStrategy.GetSatelliteIDAsync(requestObject.CardID, context.CancellationToken);

            context.ValidationResult.Add(result);
            context.Response!.SetForumResponse(response);
        }

        private static async Task<ParticipantType?> TryGetDirectParticipantTypeIDAsync(
            IDbScope dbScope,
            Guid userID,
            Guid topicID,
            CancellationToken cancellationToken = default)
        {
            var db = dbScope.Db;

            return (ParticipantType?) await db
                .SetCommand(
                    dbScope.BuilderFactory
                        .Select().C(Participants.TypeID)
                        .From(ParticipantsName).NoLock()
                        .Where().C(Participants.UserID).Equals().P(Participants.UserID)
                        .And().C(Participants.TopicID).Equals().P(Participants.TopicID)
                        .Build(),
                    db.Parameter(Participants.UserID, userID, DataType.Guid),
                    db.Parameter(Participants.TopicID, topicID, DataType.Guid))
                .LogCommand()
                .ExecuteAsync<int?>(cancellationToken);
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async Task AfterRequest(ICardRequestExtensionContext context)
        {
            if (!context.RequestIsSuccessful
                || !context.ValidationResult.IsSuccessful()
                || !this.funcByRequestType.TryGetValue(context.RequestType, out var actionAsync)
                || context.Response!.HasForumResponseObject())
            {
                // HasForumResponseObject возвращает true, если другое расширение перехватило запрос и записало объект с ответом
                return;
            }

            var license = await this.licenseManager.GetLicenseAsync(context.CancellationToken);
            if (!LicensingHelper.CheckForumLicense(license, out var errorMessage))
            {
                context.ValidationResult.Add(ForumValidationKeys.LicenseError, ValidationResultType.Error, errorMessage);
                return;
            }

            await using var _ = ForumExtensionContext.Create(context);
            await using var scope = context.DbScope?.Create();
            await actionAsync(context);
        }

        #endregion
    }
}
