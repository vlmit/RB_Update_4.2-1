#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Extensions.Default.Server.Workflow.KrPermissions;
using Tessa.Extensions.Default.Shared.Workflow.KrPermissions;
using Tessa.Platform.Runtime;
using Tessa.Platform.Validation;
using Tessa.Roles;

namespace Tessa.Extensions.Default.Server.Login
{
    /// <inheritdoc cref="ITwoFactorAuthPermissionsManager"/>
    public class KrTwoFactorAuthPermissionsManager :
        TwoFactorAuthPermissionsManager
    {
        #region Fields

        private readonly ICardTypePermissionsManager permissionsManager;
        private readonly IKrPermissionsManager krPermissionsManager;

        #endregion

        #region Constructors

        /// <summary>
        /// Создает экземпляр класса <see cref="KrTwoFactorAuthPermissionsManager"/>.
        /// </summary>
        /// <param name="session"><inheritdoc cref="ISession" path="/summary"/></param>
        /// <param name="permissionsManager"><inheritdoc cref="ICardTypePermissionsManager" path="/summary"/></param>
        /// <param name="krPermissionsManager"><inheritdoc cref="IKrPermissionsManager" path="/summary"/></param>
        public KrTwoFactorAuthPermissionsManager(
            ISession session,
            ICardTypePermissionsManager permissionsManager,
            IKrPermissionsManager krPermissionsManager)
            : base(session)
        {
            this.permissionsManager = NotNullOrThrow(permissionsManager);
            this.krPermissionsManager = NotNullOrThrow(krPermissionsManager);
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async ValueTask<bool> CanEditAsync(
            Guid userID,
            IValidationResultBuilder? validationResult = null,
            CancellationToken cancellationToken = default)
        {
            var currentUser = this.Session.User;

            if (currentUser.ID == userID || currentUser.IsAdministrator())
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
                ID = userID,
                Version = 1,
                TypeID = RoleHelper.PersonalRoleTypeID
            };

            var permissionsContextResult = await this.krPermissionsManager.TryCreateContextAsync(
                new KrPermissionsCreateContextParams
                {
                    Card = checkCard,
                    ValidationResult = validationResult
                },
                cancellationToken);

            return permissionsContextResult.Status switch
            {
                KrPermissionsCreateContextStatus.Success =>
                    await this.krPermissionsManager.CheckRequiredPermissionsAsync(
                        permissionsContextResult.Context,
                        KrPermissionFlagDescriptors.EditCard),
                KrPermissionsCreateContextStatus.Fail => false,
                KrPermissionsCreateContextStatus.NotAllowed => await base.CanEditAsync(userID, validationResult, cancellationToken),
                _ => throw ArgumentOutOfRange(permissionsContextResult.Status)
            };
        }

        #endregion
    }
}
