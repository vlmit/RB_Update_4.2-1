using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Shared.EDS;
using Tessa.Platform;
using Tessa.Platform.Data;
using Tessa.Platform.EDS;
using Tessa.Platform.IO;
using Tessa.Platform.Validation;
using Tessa.Properties.Resharper;
using Unity;

namespace Tessa.Extensions.Default.Server.EDS
{
    public sealed class CAdESSignatureRequestExtension(
        ICardServerPermissionsProvider cardServerPermissionsProvider,
        ICardStreamServerRepository cardStreamRepository,
        [OptionalDependency] IEDSProvider edsProvider = null) :
        CardRequestExtension
    {
        #region Fields

        /// <inheritdoc cref="IEDSProvider" path="/summary"/>
        [CanBeNull]
        private readonly IEDSProvider edsProvider = edsProvider;

        /// <inheritdoc cref="ICardServerPermissionsProvider" path="/summary"/>
        private readonly ICardServerPermissionsProvider cardServerPermissionsProvider = NotNullOrThrow(cardServerPermissionsProvider);

        /// <inheritdoc cref="ICardStreamServerRepository" path="/summary"/>
        private readonly ICardStreamServerRepository cardStreamRepository = NotNullOrThrow(cardStreamRepository);

        #endregion

        #region Base Overrides

        public override async Task AfterRequest(ICardRequestExtensionContext context)
        {
            if (!context.RequestIsSuccessful)
            {
                return;
            }

            Dictionary<string, object> info = context.Request.Info;

            var signingInfo = CAdESSignatureHelper.GetSigningInfo(context.Request);
            var signature = Convert.ToBase64String(signingInfo.Signature);

            switch (signingInfo.EDSAction)
            {
                case EDSAction.Sign:
                    {
                        var (result, validationInfos) =
                            this.edsProvider is not null
                                ? await this.edsProvider.ExtendDocumentAsync(
                                    signature,
                                    signingInfo.Certificate,
                                    async (ct) => await this.GetOriginalDocumentAsync(context.DbScope, signingInfo, ct),
                                    info: info,
                                    cancellationToken: context.CancellationToken)
                                : (string.Empty, null);

                        var signValInfo = validationInfos?.FirstOrDefault();

                        CAdESSignatureHelper.SetSignedData(
                            context.Response,
                            new SignedData
                            {
                                SignatureBase64 = result,
                                SignatureType = signValInfo?.ReachedSignatureType ?? SignatureType.CAdES,
                                SignatureProfile = signValInfo?.ReachedSignatureProfile ?? SignatureProfile.Bes,
                                ValidationInfos = validationInfos,
                            });

                        break;
                    }

                case EDSAction.Verify:
                    {
                        var signaturesValidations =
                            (this.edsProvider is not null
                                ? await this.edsProvider.ValidateDocumentAsync(
                                    signature,
                                    signingInfo.TargetSignatureType,
                                    signingInfo.TargetSignatureProfile,
                                    async (ct) => await this.GetOriginalDocumentAsync(context.DbScope, signingInfo, ct),
                                    info,
                                    context.CancellationToken)
                                : null)
                            ?? [];

                        CAdESSignatureHelper.SetValidationInfo(context.Response, signaturesValidations);
                        break;
                    }

                case EDSAction.GetBesFromExtended:
                    {
                        var result =
                            (this.edsProvider is not null
                                ? await this.edsProvider.GetBesSignatureAsync(
                                    signature,
                                    info,
                                    context.CancellationToken)
                                : null)
                            ?? string.Empty;

                        CAdESSignatureHelper.SetSignedData(
                            context.Response,
                            new SignedData
                            {
                                SignatureBase64 = result
                            });
                        break;
                    }

                case EDSAction.GetToBeSigned:
                    {
                        var result =
                            (this.edsProvider is not null
                                ? await this.edsProvider.GetToBeSignedAsync(
                                    signature,
                                    signingInfo.Certificate,
                                    signingInfo.SigningTime,
                                    signingInfo.DigestAlgorithm,
                                    signingInfo.EncryptionAlgorithm,
                                    info,
                                    context.CancellationToken)
                                : null)
                            ?? string.Empty;

                        CAdESSignatureHelper.SetSignedData(
                            context.Response,
                            new SignedData
                            {
                                SignatureBase64 = result
                            });
                        break;
                    }

                case EDSAction.GetToBeSignedWithHash:
                    {
                        var result =
                            (this.edsProvider is not null
                                ? await this.edsProvider.GetToBeSignedWithHashAsync(
                                    signature,
                                    signingInfo.Certificate,
                                    signingInfo.SigningTime,
                                    signingInfo.DigestAlgorithm,
                                    signingInfo.EncryptionAlgorithm,
                                    info,
                                    context.CancellationToken)
                                : null)
                            ?? string.Empty;

                        CAdESSignatureHelper.SetSignedData(
                            context.Response,
                            new SignedData
                            {
                                SignatureBase64 = result
                            });
                        break;
                    }

                case EDSAction.GetBesSignature:
                    {
                        var result =
                            (this.edsProvider is not null
                                ? await this.edsProvider.GetSignedDocumentAsync(
                                    signature,
                                    signingInfo.File,
                                    signingInfo.Certificate,
                                    signingInfo.SigningTime,
                                    signingInfo.DigestAlgorithm,
                                    signingInfo.EncryptionAlgorithm,
                                    info,
                                    context.CancellationToken)
                                : null)
                            ?? string.Empty;

                        CAdESSignatureHelper.SetSignedData(
                            context.Response,
                            new SignedData
                            {
                                SignatureBase64 = result
                            });
                        break;
                    }

                case EDSAction.GetBesSignatureWithSignedAttributes:
                    {
                        var result =
                            (this.edsProvider is not null
                                ? await this.edsProvider.GetSignedDocumentWithSignedAttributesAsync(
                                    signature,
                                    signingInfo.File,
                                    signingInfo.Certificate,
                                    signingInfo.SigningTime,
                                    signingInfo.DigestAlgorithm,
                                    signingInfo.EncryptionAlgorithm,
                                    info,
                                    context.CancellationToken)
                                : null)
                            ?? string.Empty;

                        CAdESSignatureHelper.SetSignedData(
                            context.Response,
                            new SignedData
                            {
                                SignatureBase64 = result
                            });
                        break;
                    }

                case EDSAction.GetSignatureAttributesFromSignature:
                    {
                        SignatureAttributes signatureAttributes =
                            this.edsProvider is not null
                                ? await this.edsProvider.GetSignatureAttributesFromSignatureAsync(
                                    signature,
                                    info,
                                    context.CancellationToken)
                                : null;

                        CAdESSignatureHelper.SetSignatureAttributes(context.Response, signatureAttributes);
                        break;
                    }
            }
        }

        #endregion

        #region Private Methods

        private async Task<byte[]?> GetOriginalDocumentAsync(
            IDbScope dbScope,
            SigningInfo signingInfo,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(dbScope);

            if (signingInfo.VersionID is null)
            {
                throw new ArgumentNullException(
                    nameof(signingInfo),
                    $"Parameter request.Info.[\"{CAdESSignatureKeys.SigningInfoKey}\"][\"{nameof(signingInfo.VersionID)}\"] is not specified.");
            }

            return await this.GetVersionContentAsync(
                dbScope,
                signingInfo.VersionID.Value,
                cancellationToken);
        }

        private async Task<byte[]?> GetVersionContentAsync(
            IDbScope dbScope,
            Guid versionID,
            CancellationToken cancellationToken = default)
        {
            CardGetFileContentRequest contentRequest;

            await using (dbScope.CreateNew())
            {
                await using var reader = await dbScope.Db.SetCommand(
                    dbScope.BuilderFactory
                        .Select().Top(1)
                            .C("ID")
                            .C("RowID")
                        .From("Files").NoLock()
                        .Where()
                            .C("VersionRowID")
                            .Equals()
                            .P(nameof(versionID))
                        .Limit(1)
                        .Build(),
                        dbScope.Db.Parameter(nameof(versionID), versionID, LinqToDB.DataType.Guid))
                    .LogCommand()
                    .ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    throw new InvalidOperationException($"Couldn't find card and file associated with version: '{versionID}'.");
                }

                contentRequest = new CardGetFileContentRequest
                {
                    CardID = reader.GetGuid(0),
                    FileID = reader.GetGuid(1),
                    VersionRowID = versionID
                };
            }

            this.cardServerPermissionsProvider.SetFullPermissions(contentRequest);
            var contentResult = await this.cardStreamRepository.GetFileContentAsync(contentRequest, cancellationToken);
            var contentResponse = contentResult.Response;

            if (!contentResponse.ValidationResult.IsSuccessful())
            {
                throw new ValidationException(contentResponse.ValidationResult.Build());
            }

            await using var contentStream = await contentResult.GetContentOrThrowAsync(cancellationToken);
            return await contentStream.ReadAllBytesAsync(cancellationToken);
        }

        #endregion
    }
}
