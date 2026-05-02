using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Files;
using Tessa.Platform;
using Tessa.Platform.Data;
using Tessa.Platform.EDS;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.EDS.SignatureArchive
{
    /// <inheritdoc cref="ISignatureArchiveManager"/>
    public sealed class SignatureArchiveManager(
        IEDSProvider edsProvider,
        ICardRepository cardRepository,
        ICardServerPermissionsProvider cardServerPermissionsProvider,
        ICardFileManager cardFileManager,
        ISignatureArchivePermissionProvider signatureArchivePermissionProvider) 
            : ISignatureArchiveManager
    {
        #region Fields

        /// <inheritdoc cref="IEDSProvider" path="/summary"/>
        private readonly IEDSProvider edsProvider = NotNullOrThrow(edsProvider);

        /// <inheritdoc cref="ICardRepository" path="/summary"/>
        private readonly ICardRepository cardRepository = NotNullOrThrow(cardRepository);

        /// <inheritdoc cref="ICardServerPermissionsProvider" path="/summary"/>
        private readonly ICardServerPermissionsProvider cardServerPermissionsProvider = NotNullOrThrow(cardServerPermissionsProvider);

        /// <inheritdoc cref="ICardFileManager" path="/summary"/>
        private readonly ICardFileManager cardFileManager = NotNullOrThrow(cardFileManager);

        /// <inheritdoc cref="ISignatureArchivePermissionProvider" path="/summary"/>
        private readonly ISignatureArchivePermissionProvider signatureArchivePermissionProvider = NotNullOrThrow(signatureArchivePermissionProvider);

        #endregion

        #region ISignatureArchiveManager implementations

        /// <inheritdoc/>
        public async Task<ValidationResult> ArchiveSignaturesAsync(
            Guid cardID,
            IEnumerable<Guid> fileIDs,
            CancellationToken cancellationToken = default)
        {
            var validationResultBuilder = new ValidationResultBuilder();

            if (!await this.signatureArchivePermissionProvider.IsAdministratorAsync(cancellationToken))
            {
                return validationResultBuilder
                    .AddError(this, "$UI_Common_PermissionDenied")
                    .Build();
            }

            var cardRequest = new CardGetRequest()
            {
                CardID = cardID,
                RestrictionFlags =
                    CardGetRestrictionFlags.RestrictSections |
                    CardGetRestrictionFlags.RestrictTasks |
                    CardGetRestrictionFlags.RestrictTaskHistory
            };

            this.cardServerPermissionsProvider.SetFullPermissions(cardRequest);
            var response = await this.cardRepository.GetAsync(cardRequest, cancellationToken);
            validationResultBuilder.Add(response.ValidationResult);

            if (!validationResultBuilder.IsSuccessful())
            {
                return validationResultBuilder.Build();
            }

            var card = response.Card;

            await using var fileContainer = await this.cardFileManager.CreateContainerAsync(card, cancellationToken: cancellationToken);

            foreach (var file in fileContainer.FileContainer.Files)
            {
                if (!fileIDs.Contains(file.ID))
                {
                    continue;
                }

                var version = file.Versions.Last;
                validationResultBuilder.Add(await version.EnsureSignaturesLoadedAsync(FileSignatureLoadingMode.WithData, cancellationToken: cancellationToken));

                if (!validationResultBuilder.IsSuccessful())
                {
                    return validationResultBuilder.Build();
                }

                validationResultBuilder.Add(await version.EnsureContentDownloadedAsync(cancellationToken: cancellationToken));

                if (!validationResultBuilder.IsSuccessful())
                {
                    return validationResultBuilder.Build();
                }

                foreach (var fileSignature in version.Signatures.ToArray())
                {
                    var bytes = await fileSignature.Data.GetBytesAsync(cancellationToken);
                    var signature = Convert.ToBase64String(bytes);

                    var signatureAttributes = await this.edsProvider.GetSignatureAttributesFromSignatureAsync(signature, null, cancellationToken);

                    var (signedDocument, validInfo) = await this.edsProvider.ExtendDocumentAsync(
                        signature,
                        signatureAttributes.Certificate,
                        version.ReadAllBytesAsync,
                        SignatureProfile.A,
                        null,
                        cancellationToken);

                    var signValInfo = validInfo.FirstOrDefault();

                    if (signValInfo.State != FileSignatureState.Checked)
                    {
                        var details = new List<string>(signValInfo.ReachedLevelErrorDesc.Length);
                        foreach (var levelDesc in signValInfo.ReachedLevelErrorDesc)
                        {
                            // Собираем строку с деталями ошибки вида: "Уровень подписи: Статус подписи, Подробности ошибки."
                            var additional = levelDesc.ElementAtOrDefault(2);
                            additional = string.IsNullOrEmpty(additional) ? string.Empty : $", {LocalizeFormat(additional)}";
                            details.Add($"{Localize(levelDesc.ElementAtOrDefault(0))}: {Localize(levelDesc.ElementAtOrDefault(1))}{additional}");
                        }

                        validationResultBuilder.Add(
                            ValidationKey.Unknown,
                            ValidationResultType.Error,
                            LocalizeFormat("$UI_Signature_Message_CannotExtendToProfile", fileSignature.ID, signValInfo.TargetSignatureProfile.GetDescription()),
                            details: string.Join(Environment.NewLine, details));
                    }

                    if (signValInfo is null)
                    {
                        continue;
                    }

                    var signatureToken = await version.Source.GetSignatureCreationTokenAsync(cancellationToken);
                    signatureToken.EventType = fileSignature.EventType;
                    signatureToken.Company = fileSignature.Company;
                    signatureToken.SubjectName = fileSignature.SubjectName;
                    signatureToken.SerialNumber = fileSignature.SerialNumber;
                    signatureToken.IssuerName = fileSignature.IssuerName;
                    signatureToken.UserID = fileSignature.UserID;
                    signatureToken.UserName = fileSignature.UserName;
                    signatureToken.Comment = fileSignature.Comment;

                    signatureToken.Data = Convert.FromBase64String(signedDocument);
                    signatureToken.SignatureType = signValInfo.ReachedSignatureType;
                    signatureToken.SignatureProfile = signValInfo.ReachedSignatureProfile;
                    signatureToken.NearestCertSerialNumber = signValInfo.NearestCertSerialNumber;
                    signatureToken.NearestCertValidTo = signValInfo.NearestCertValidTo;

                    await version.Signatures.AddWithNotificationAsync(
                        await version.Source.CreateSignatureAsync(signatureToken, version, cancellationToken),
                        cancellationToken);
                    await version.Signatures.RemoveWithNotificationAsync(fileSignature, cancellationToken);
                }
            }

            if (!validationResultBuilder.IsSuccessful())
            {
                return validationResultBuilder.Build();
            }

            var containerStoreResult = await fileContainer.StoreAsync((_, req, _) =>
            {
                this.cardServerPermissionsProvider.SetFullPermissions(req);
                return ValueTask.CompletedTask;
            }, cancellationToken: cancellationToken);

            return validationResultBuilder
                .Add(containerStoreResult.ValidationResult)
                .Build();
        }

        #endregion
    }
}
