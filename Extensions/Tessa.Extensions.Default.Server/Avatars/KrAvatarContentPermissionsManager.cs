#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Content.Avatars;
using Tessa.Extensions.Default.Server.Workflow.KrPermissions;
using Tessa.Extensions.Default.Shared.Workflow.KrPermissions;
using Tessa.Platform.Data;
using Tessa.Platform.Runtime;
using Tessa.Platform.Validation;
using Tessa.Roles;

namespace Tessa.Extensions.Default.Server.Avatars
{
    /// <inheritdoc cref="IAvatarContentPermissionsManager"/>
    /// <inheritdoc cref="AvatarContentPermissionsManager"/>
    /// <param name="permissionsManager"><inheritdoc cref="ICardTypePermissionsManager" path="/summary"/></param>
    /// <param name="krPermissionsManager"><inheritdoc cref="IKrPermissionsManager" path="/summary"/></param>
    public class KrAvatarContentPermissionsManager(
        IDbScope dbScope,
        ISession session,
        ICardTypePermissionsManager permissionsManager,
        IKrPermissionsManager krPermissionsManager)
        : AvatarContentPermissionsManager(dbScope, session)
    {
        #region Fields

        private readonly ICardTypePermissionsManager permissionsManager = NotNullOrThrow(permissionsManager);
        private readonly IKrPermissionsManager krPermissionsManager = NotNullOrThrow(krPermissionsManager);

        #endregion

        #region IAvatarContentPermissionsManager

        /// <inheritdoc/>
        public override async ValueTask<bool> CanEditAsync(
            Guid id,
            AvatarContentKind kind,
            IValidationResultBuilder? validationResult = null,
            CancellationToken cancellationToken = default)
        {
            // В типовом решении поддерживаются только аватары для пользователей, поэтому здесь не ожидается другой сущности.
            // В проектных решениях при реализации расширенной проверки прав доступа необходимо будет проверить тип сущности.

            if (this.Session.User.IsAdministrator() || this.Session.User.ID == id)
            {
                return true;
            }

            if (!await this.permissionsManager.CardTypeUseCustomPermissionsAsync(RoleHelper.PersonalRoleTypeID, cancellationToken))
            {
                if (validationResult is not null)
                {
                    ValidationSequence
                       .Begin(validationResult)
                       .SetObjectName(this)
                       .Error(ValidationKeys.UserIsNotAdmin)
                       .End();
                }

                return false;
            }

            var checkCard = new Card
            {
                ID = id,
                Version = 1,
                TypeID = RoleHelper.PersonalRoleTypeID,
            };

            var permissionsContextResult = await this.krPermissionsManager.TryCreateContextAsync(
                new KrPermissionsCreateContextParams
                {
                    Card = checkCard,
                    ValidationResult = validationResult
                }, cancellationToken: cancellationToken);

            return permissionsContextResult.Status switch
            {
                KrPermissionsCreateContextStatus.Success =>
                    await this.krPermissionsManager.CheckRequiredPermissionsAsync(
                        permissionsContextResult.Context,
                        KrPermissionFlagDescriptors.EditCard),
                KrPermissionsCreateContextStatus.Fail => false,
                KrPermissionsCreateContextStatus.NotAllowed => await base.CanEditAsync(id, kind, validationResult, cancellationToken),
                _ => throw ArgumentOutOfRange(permissionsContextResult.Status)
            };
        }

        #endregion
    }
}
