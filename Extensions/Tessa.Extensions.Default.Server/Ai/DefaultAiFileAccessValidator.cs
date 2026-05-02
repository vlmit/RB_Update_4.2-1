#nullable enable

using System.Threading;
using System.Threading.Tasks;
using Tessa.Ai.Files;
using Tessa.Cards;
using Tessa.Cards.ComponentModel;
using Tessa.Extensions.Default.Server.Workflow.KrPermissions;
using Tessa.Extensions.Default.Shared.Workflow.KrPermissions;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Ai
{
    /// <inheritdoc cref="IAiFileAccessValidator"/>
    /// <param name="versionStrategy"><inheritdoc cref="ICardFileVersionStrategy" path="/summary"/></param>
    /// <param name="getStrategy"><inheritdoc cref="ICardGetStrategy" path="/summary"/></param>
    /// <param name="cardMetadata"><inheritdoc cref="ICardMetadata" path="/summary"/></param>
    /// <param name="krPermissionsManager"><inheritdoc cref="IKrPermissionsManager" path="/summary"/></param>
    public sealed class DefaultAiFileAccessValidator(
        ICardFileVersionStrategy versionStrategy,
        ICardGetStrategy getStrategy,
        ICardMetadata cardMetadata,
        IKrPermissionsManager krPermissionsManager)
        : IAiFileAccessValidator
    {
        #region Fields

        private readonly ICardFileVersionStrategy versionStrategy = NotNullOrThrow(versionStrategy);

        private readonly ICardGetStrategy getStrategy = NotNullOrThrow(getStrategy);

        private readonly ICardMetadata cardMetadata = NotNullOrThrow(cardMetadata);

        private readonly IKrPermissionsManager krPermissionsManager = NotNullOrThrow(krPermissionsManager);

        #endregion

        #region IAiFileAccessValidator Members

        /// <inheritdoc/>
        public async ValueTask<ValidationResult> CheckCardAccessAsync(AiCardFileRequest request, CancellationToken cancellationToken = default)
        {
            ThrowIfNull(request);

            var (cardID, fileID) = await this.versionStrategy.GetCardInfoAsync(request.FileVersionRowID, cancellationToken);
            if (!cardID.HasValue || !fileID.HasValue)
            {
                // no access error to non-existent file (maybe, virtual);
                // but later on an error will occur when trying to add such a file to the AI cache (file will be "not found", not "forbidden")
                return ValidationResult.Empty;
            }

            // most logic is borrowed from KrFileAccessHelper
            var validationResult = new ValidationResultBuilder();
            var permissionsContextResult = await this.krPermissionsManager.TryCreateContextAsync(
                new()
                {
                    CardID = cardID,
                    FileID = fileID,
                    FileVersionID = request.FileVersionRowID,
                    ValidationResult = validationResult,
                    WithExtendedPermissions = true,
                    ServiceType = CardServiceType.Client
                },
                cancellationToken: cancellationToken);

            // here validationResult is not successful (and Status is Fail), when there are
            // card structural errors (for instance, no DocTypeID in database when type is using document types),
            // or KrToken errors (but we pass no tokens currently)

            switch (permissionsContextResult.Status)
            {
                case KrPermissionsCreateContextStatus.Success:
                    // access errors are added to validationResult
                    await this.krPermissionsManager.CheckRequiredPermissionsAsync(
                        permissionsContextResult.Context,
                        KrPermissionFlagDescriptors.ReadCard);
                    break;

                case KrPermissionsCreateContextStatus.NotAllowed:
                    // for those types that are not in the standard solution:
                    // do not allow to access any files for administrative cards (including AI cache itself)
                    if (await this.getStrategy.GetTypeIDAsync(cardID.Value, cancellationToken: cancellationToken) is { } typeID
                        && (await this.cardMetadata.GetCardTypesAsync(cancellationToken)).TryGetValue(typeID, out var type)
                        && type.Flags.Has(CardTypeFlags.Administrative))
                    {
                        // do not provide any details about the file or the type to the client
                        validationResult.AddError(this, "Card type is administrative, can't access its file through the AI cache.");
                    }

                    break;
            }

            return validationResult.Build();
        }

        /// <inheritdoc/>
        public ValueTask<ValidationResult> CheckVirtualAccessAsync(AiVirtualFileRequest request, CancellationToken cancellationToken = default)
        {
            ThrowIfNull(request);

            // skip permission checks for virtual file;
            // AI cache ensures that it's not a physical file disguised as virtual (even if it has the same identifier);
            // also access checks will be performed in extensions CardGetFileContentExtension using user's session
            return new(ValidationResult.Empty);
        }

        #endregion
    }
}
