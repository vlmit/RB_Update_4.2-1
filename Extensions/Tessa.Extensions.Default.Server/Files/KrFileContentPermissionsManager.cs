#nullable enable
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Content.Files;
using Tessa.Extensions.Default.Server.Workflow.KrPermissions;
using Tessa.Extensions.Default.Shared.Workflow.KrPermissions;
using Tessa.Platform;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Files
{
    /// <summary>
    /// <inheritdoc cref="IFileContentPermissionsManager" path="/summary"/><br/>
    /// For standard solution.
    /// </summary>
    [Order(1)]
    public sealed class KrFileContentPermissionsManager(
        IKrPermissionsManager krPermissionsManager,
        ICardTypePermissionsManager cardTypePermissionsManager)
        : IFileContentPermissionsManager
    {
        #region Fields

        private readonly IKrPermissionsManager krPermissionsManager = NotNullOrThrow(krPermissionsManager);

        private readonly ICardTypePermissionsManager cardTypePermissionsManager = NotNullOrThrow(cardTypePermissionsManager);

        #endregion

        #region IFileContentPermissionManager Implementation

        /// <inheritdoc/>
        public ValueTask<bool> CanApplyFileLinkPermissionsAsync(
            IFileContentLinkPermissionContext context,
            CancellationToken cancellationToken = default) =>
            this.cardTypePermissionsManager.CardTypeUseCustomPermissionsAsync(context.CardTypeID, cancellationToken);

        /// <inheritdoc/>
        public async ValueTask<(bool Success, ValidationResult Result)> CanCreateFileLinkAsync(
            IFileContentLinkPermissionContext context,
            CancellationToken cancellationToken = default)
        {
            var validationResult = new ValidationResultBuilder();
            var permContextResult = await this.krPermissionsManager.TryCreateContextAsync(
                new KrPermissionsCreateContextParams
                {
                    CardID = context.CardID,
                    FileID = context.FileID,
                    FileVersionID = context.FileVersionID,
                    ValidationResult = validationResult,
                    WithExtendedPermissions = true,
                    ServiceType = CardServiceType.Client,
                },
                cancellationToken: cancellationToken);

            if (permContextResult.Status != KrPermissionsCreateContextStatus.Success)
            {
                return (false, validationResult.Build());
            }

            bool hasPermissions = await this.krPermissionsManager.CheckRequiredPermissionsAsync(
                permContextResult.Context,
                KrPermissionFlagDescriptors.ReadCard, KrPermissionFlagDescriptors.CreateFileLink);

            return (hasPermissions, validationResult.Build());
        }

        #endregion
    }
}
