#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Extensions.Default.Server.Workflow.KrPermissions;
using Tessa.Extensions.Default.Shared.Workflow.KrPermissions;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Forums;
using Tessa.Forums.Models;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Forums
{
    /// <summary>
    /// Объект, определяющий доступ к обсуждениям на основании правил доступа типовой системы прав и токена безопасности.
    /// Реализация для использования на сервере. 
    /// </summary>
    public class KrForumPermissionsProvider(IForumPermissionsDependencies forumPermissionsDependencies) : ForumPermissionsProvider
    {
        #region Fields

        private readonly IForumPermissionsDependencies forumPermissionsDependencies = NotNullOrThrow(forumPermissionsDependencies);

        #endregion

        #region Private Methods

        private async ValueTask<(IKrPermissionsManagerResult? Result, ValidationResult ValidationResult)> ResolveEffectivePermissionsAsync(
            Guid cardID,
            KrPermissionFlagDescriptor[] permissionFlagDescriptors,
            Guid? topicID = null,
            Dictionary<string, object?>? permissionsToken = null,
            CancellationToken cancellationToken = default)
        {
            var cardContext = ForumExtensionContext.Current.CardContext;
            var krPermissionsManager = this.forumPermissionsDependencies.KrPermissionsManager;
            var permissionContextResult = await krPermissionsManager.TryCreateContextAsync(
                new KrPermissionsCreateContextParams
                {
                    CardID = cardID,
                    ExtensionContext = cardContext,
                    PrevToken = permissionsToken is not null ? new KrToken(permissionsToken) : null,
                    ServerToken = cardContext?.Info.TryGetServerToken(),
                    AdditionalInfo = cardContext?.Info,
                    ValidationResult = new ValidationResultBuilder(),
                },
                cancellationToken);

            if (permissionContextResult.Status != KrPermissionsCreateContextStatus.Success)
            {
                // карточка не входит в типовое решение
                return (Result: null, permissionContextResult.ValidationResult);
            }

            permissionContextResult.Context!.Info[nameof(KrForumPermissionsProvider)] =
                new Dictionary<string, object?> { [nameof(topicID)] = topicID };

            var result = await krPermissionsManager.GetEffectivePermissionsAsync(
                permissionContextResult.Context,
                permissionFlagDescriptors);

            return (result, permissionContextResult.Context.ValidationResult.Build());
        }

        private async ValueTask<(bool Success, ValidationResult Result)> ResolvePermissionsAsync(
            KrPermissionFlagDescriptor required,
            Guid cardID,
            Dictionary<string, object?>? permissionsToken,
            Dictionary<string, object?>? info = null,
            CancellationToken cancellationToken = default)
        {
            var cardContext = ForumExtensionContext.Current.CardContext;
            var permissionsManager = this.forumPermissionsDependencies.KrPermissionsManager;
            var permissionContextResult = await permissionsManager.TryCreateContextAsync(
                new KrPermissionsCreateContextParams
                {
                    CardID = cardID,
                    ExtensionContext = cardContext,
                    PrevToken = permissionsToken is not null ? new KrToken(permissionsToken) : null,
                    ServerToken = cardContext?.Info.TryGetServerToken(),
                    AdditionalInfo = cardContext?.Info,
                    ValidationResult = new ValidationResultBuilder(),
                },
                cancellationToken);

            if (permissionContextResult.Status != KrPermissionsCreateContextStatus.Success)
            {
                // карточка не входит в типовое решение
                return (Success: permissionContextResult.Status != KrPermissionsCreateContextStatus.Fail, Result: permissionContextResult.ValidationResult);
            }

            if (info is not null)
            {
                permissionContextResult.Context!.Info[nameof(KrForumPermissionsProvider)] = info;
            }

            var checkResult = await permissionsManager.CheckRequiredPermissionsAsync(permissionContextResult.Context, required);

            return (checkResult.Result, checkResult ? ValidationResult.Empty : permissionContextResult.Context!.ValidationResult.Build());
        }

        #endregion

        #region Protected

        protected virtual async ValueTask<(ParticipantModel? Participant, ValidationResult Result)> ResolveUserPermissionsCoreAsync(
            Guid topicID,
            Guid cardID,
            bool checkElevatedPermissions = false,
            Dictionary<string, object?>? permissionToken = null,
            CancellationToken cancellationToken = default)
        {
            var session = this.forumPermissionsDependencies.Session;
            if (checkElevatedPermissions)
            {
                var (isSuperModerator, canEditAllMessages, _) = await this.CheckElevatedPermissionsAsync(cardID, cancellationToken: cancellationToken);

                if (isSuperModerator)
                {
                    return (new ParticipantModel
                    {
                        Type = ParticipantType.SuperModerator,
                        UserID = session.User.ID,
                        UserName = session.User.Name,
                        IsReadOnly = false,
                        CanEditMessages = true,
                        CanEditAllMessages = canEditAllMessages,
                        IsSubscribed = false,
                        TopicID = topicID,
                    }, ValidationResult.Empty);
                }
            }

            var participant = await this.forumPermissionsDependencies.ForumParticipantProvider.TryGetUserParticipantInfoAsync(topicID, cancellationToken);

            if (participant is not null)
            {
                return (participant, ValidationResult.Empty);
            }

            var (krResult, krValidationResult) = await this.ResolveEffectivePermissionsAsync(
                cardID,
                [
                    KrPermissionFlagDescriptors.CanReadAllTopics,
                    KrPermissionFlagDescriptors.CanReadAndSendMessageInAllTopics,
                    KrPermissionFlagDescriptors.EditMyMessages,
                    KrPermissionFlagDescriptors.EditAllMessages
                ],
                topicID,
                permissionToken,
                cancellationToken);

            if (!krValidationResult.IsSuccessful || krResult is null)
            {
                return (null, krValidationResult);
            }

            // Пользователь не супермодератор, и среди участников топика его нет,
            // Если у него нет прав читать сообщения в топиках, то не даем ему права на дальнейшие действия и
            // возвращаем ошибку о том, что его нет в списке участников.
            if (!krResult.Permissions.Contains(KrPermissionFlagDescriptors.CanReadAndSendMessageInAllTopics)
                && !krResult.Permissions.Contains(KrPermissionFlagDescriptors.CanReadAllTopics))
            {
                var validationResultBuilder = new ValidationResultBuilder
                {
                    {
                        ForumValidationKeys.PermissionError,
                        ValidationResultType.Error,
                        await LocalizeFormatAsync(
                            "$Forum_ValidationMessage_PermissionError_UserNotParticipant",
                            session.User.ID,
                            topicID)
                    }
                };
                return (null, validationResultBuilder.Build());
            }

            return (new ParticipantModel
            {
                Type = ParticipantType.Participant,
                UserID = session.User.ID,
                UserName = session.User.Name,
                IsReadOnly =
                    !krResult.Permissions.Contains(KrPermissionFlagDescriptors.CanReadAndSendMessageInAllTopics),
                CanEditMessages =
                    krResult.Permissions.Contains(KrPermissionFlagDescriptors.EditMyMessages) ||
                    krResult.Permissions.Contains(KrPermissionFlagDescriptors.EditAllMessages),
                CanEditAllMessages =
                    krResult.Permissions.Contains(KrPermissionFlagDescriptors.EditAllMessages),
                IsSubscribed = false,
                TopicID = topicID
            }, ValidationResult.Empty);
        }

        protected virtual async ValueTask<(bool Success, ValidationResult Result)> CheckEditMessagesPermissionCoreAsync(
            Guid topicID,
            Guid cardID,
            bool isMyMessage = false,
            Dictionary<string, object?>? permissionsToken = null,
            CancellationToken cancellationToken = default)
        {
            var info = new Dictionary<string, object?> { { nameof(topicID), topicID } };

            if (isMyMessage)
            {
                var (success, result) = await this.ResolvePermissionsAsync(
                    KrPermissionFlagDescriptors.EditMyMessages,
                    cardID,
                    permissionsToken,
                    info,
                    cancellationToken);

                if (success)
                {
                    return (true, result);
                }
            }

            return await this.ResolvePermissionsAsync(
                KrPermissionFlagDescriptors.EditAllMessages,
                cardID,
                permissionsToken,
                info,
                cancellationToken);
        }

        #endregion

        #region Base Overrides

        /// <inerhitdoc />
        /// <remarks>
        /// Метод вызывается в контексте расширений. Контекст можно получить, как <c>ForumExtensionContext.Current.CardContext</c>.
        /// </remarks>
        public override async ValueTask<(ParticipantModel? Participant, ValidationResult Result)> ResolveUserPermissionsAsync(
            Guid topicID,
            Guid? cardID = null,
            bool checkElevatedPermissions = false,
            Dictionary<string, object?>? permissionToken = null,
            CancellationToken cancellationToken = default)
        {
            var dbScope = this.forumPermissionsDependencies.DbScope;
            await using var _ = dbScope.Create();

            cardID ??= await ForumProviderStrategyHelper.GetMainCardIDAsync(topicID, dbScope, cancellationToken);

            return await this.ResolveUserPermissionsCoreAsync(topicID, cardID.Value, checkElevatedPermissions, permissionToken, cancellationToken);
        }

        /// <inheritdoc />
        /// <remarks>
        /// Метод вызывается в контексте расширений. Контекст можно получить, как <c>ForumExtensionContext.Current.CardContext</c>.
        /// </remarks>
        public override ValueTask<(bool Success, ValidationResult Result)> CheckAddTopicPermissionAsync(
            Guid cardID,
            Dictionary<string, object?>? permissionToken = null,
            CancellationToken cancellationToken = default) =>
            this.ResolvePermissionsAsync(
                KrPermissionFlagDescriptors.AddTopics,
                cardID,
                null,
                cancellationToken: cancellationToken);

        /// <inheritdoc />
        /// <remarks>
        /// Метод вызывается в контексте расширений. Контекст можно получить, как <c>ForumExtensionContext.Current.CardContext</c>.
        /// </remarks>
        public override async ValueTask<(bool IsSuperModerator, bool CanEditAllMessages, ValidationResult Result)> CheckElevatedPermissionsAsync(
            Guid cardID,
            Dictionary<string, object?>? permissionToken = null,
            CancellationToken cancellationToken = default)
        {
            var (result, validationResult) = await this.ResolveEffectivePermissionsAsync(
                cardID,
                [
                    KrPermissionFlagDescriptors.SuperModeratorMode,
                    KrPermissionFlagDescriptors.EditAllMessages
                ],
                permissionsToken: permissionToken,
                cancellationToken: cancellationToken);

            if (!validationResult.IsSuccessful)
            {
                return (false, false, validationResult);
            }

            // technically the only way to get null here
            // is if the card is not included in the standard solution
            if (result is null)
            {
                return (true, true, ValidationResult.Empty);
            }

            var permissions = result.Permissions;
            return (permissions.Contains(KrPermissionFlagDescriptors.SuperModeratorMode),
                permissions.Contains(KrPermissionFlagDescriptors.EditAllMessages),
                validationResult);
        }

        /// <inheritdoc />
        /// <remarks>
        /// Метод вызывается в контексте расширений. Контекст можно получить, как <c>ForumExtensionContext.Current.CardContext</c>.
        /// </remarks>
        public override async ValueTask<(bool Success, ValidationResult Result)> CheckEditMessagesPermissionAsync(
            Guid topicID,
            Guid? cardID = null,
            bool isMyMessage = false,
            Dictionary<string, object?>? permissionsToken = null,
            CancellationToken cancellationToken = default)
        {
            var dbScope = this.forumPermissionsDependencies.DbScope;
            await using var _ = dbScope.Create();

            cardID ??= await ForumProviderStrategyHelper.GetMainCardIDAsync(topicID, dbScope, cancellationToken);

            return await this.CheckEditMessagesPermissionCoreAsync(topicID, cardID.Value, isMyMessage, permissionsToken, cancellationToken);
        }

        /// <inerhitdoc />
        /// <remarks>
        /// Метод вызывается в контексте расширений. Контекст можно получить, как <c>ForumExtensionContext.Current.CardContext</c>.
        /// </remarks>
        public override async ValueTask<IReadOnlyList<TopicModel>> GetAvailableTopicsAsync(
            Guid cardID,
            bool isSuperModeratorModeEnabled,
            Func<Guid, bool, CancellationToken, ValueTask<IReadOnlyList<TopicModel>>> getCardTopicsAsync,
            Func<Guid, CancellationToken, ValueTask<IReadOnlyList<TopicModel>>> getUserTopicsAsync,
            CancellationToken cancellationToken = default)
        {
            if (isSuperModeratorModeEnabled)
            {
                return await getCardTopicsAsync(cardID, true, cancellationToken);
            }

            var (krResult, _) = await this.ResolveEffectivePermissionsAsync(
                cardID,
                [KrPermissionFlagDescriptors.CanReadAndSendMessageInAllTopics],
                cancellationToken: cancellationToken);

            if (krResult is not null
                && (krResult.Permissions.Contains(KrPermissionFlagDescriptors.CanReadAllTopics)
                    || krResult.Permissions.Contains(KrPermissionFlagDescriptors.CanReadAndSendMessageInAllTopics)))
            {
                return await getCardTopicsAsync(cardID, false, cancellationToken);
            }

            return await getUserTopicsAsync(cardID, cancellationToken);
        }

        #endregion
    }
}
