using System;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Ai.Plugins.CardTool;
using Tessa.Cards;
using Tessa.Extensions.Default.Server.Workflow.KrPermissions;
using Tessa.Extensions.Default.Shared.Workflow.KrPermissions;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Ai.Plugins.CardTool
{
    /// <inheritdoc/>
    public sealed class KrCardToolPermissionsProvider(IKrPermissionsManager krPermissionsManager) : ICardToolPermissionsProvider
    {
        #region Fields

        private readonly IKrPermissionsManager krPermissionsManager = NotNullOrThrow(krPermissionsManager);

        #endregion

        /// <inheritdoc/>
        public async Task<bool> CheckCardCreatePermissionAsync(
            Guid cardTypeID,
            Guid? docTypeID,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken)
        {
            var permissionsContextResult = await this.krPermissionsManager.TryCreateContextAsync(
                new KrPermissionsCreateContextParams
                {
                    CardTypeID = cardTypeID,
                    DocTypeID = docTypeID,
                    ValidationResult = validationResult,
                    ServiceType = CardServiceType.Client,
                },
                cancellationToken: cancellationToken);

            return permissionsContextResult.Status switch
            {
                KrPermissionsCreateContextStatus.Success =>
                    await this.krPermissionsManager.CheckRequiredPermissionsAsync(
                        permissionsContextResult.Context,
                        KrPermissionFlagDescriptors.CreateCard),
                KrPermissionsCreateContextStatus.Fail => false,
                KrPermissionsCreateContextStatus.NotAllowed => true,
                _ => throw ArgumentOutOfRange(permissionsContextResult.Status)
            };
        }
    }
}
