#nullable enable

using System.Linq;
using System.Threading.Tasks;
using Tessa.Extensions.Default.Server.Workflow.KrPermissions;
using Tessa.Extensions.Default.Shared.Workflow.KrPermissions;
using Tessa.Forums;
using Tessa.Forums.Mentions;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Forums.Mention
{
    /// <summary>
    /// Расширение на упоминание пользователей в обсуждении, которое проверяет наличие прав
    /// для текущего пользователя на упоминание других пользователей в обсуждениях.
    /// </summary>
    /// <param name="permissionsManager"><inheritdoc cref="IKrPermissionsManager" path="/summary"/></param>
    public class CheckPermissionsMentionExtension(
        IKrPermissionsManager permissionsManager) : ForumUserMentionExtension
    {
        #region Private Fields

        private readonly IKrPermissionsManager permissionsManager = NotNullOrThrow(permissionsManager);

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async Task BeforeRequest(IForumUserMentionExtensionContext context)
        {
            if (!context.ValidationResult.IsSuccessful())
            {
                return;
            }

            if (!context.UserModels.Any(x => x.New || x.ReadOnly))
            {
                return;
            }

            var permContextResult = await this.permissionsManager.TryCreateContextAsync(
                new KrPermissionsCreateContextParams
                {
                    CardID = context.CardID,
                    CardTypeID = context.CardTypeID,
                    AdditionalInfo = context.Info,
                },
                cancellationToken: context.CancellationToken);

            if (permContextResult.Status != KrPermissionsCreateContextStatus.Success)
            {
                context.ValidationResult.Add(
                    ForumValidationKeys.PermissionError,
                    ValidationResultType.Error,
                    "$Forum_ValidationMessage_PermissionError_MentionUsers");
                return;
            }

            var permission = await this.permissionsManager.CheckRequiredPermissionsAsync(
                permContextResult.Context,
                KrPermissionFlagDescriptors.CanMentionNewUsers);

            if (permContextResult.Status == KrPermissionsCreateContextStatus.Fail || !permission)
            {
                context.ValidationResult.Add(
                    ForumValidationKeys.PermissionError,
                    ValidationResultType.Error,
                    "$Forum_ValidationMessage_PermissionError_MentionUsers");
            }
        }

        #endregion
    }
}
