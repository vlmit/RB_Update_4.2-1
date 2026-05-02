using System.Threading.Tasks;
using Tessa.Cards.Extensions;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.EDS.SignatureArchive
{
    /// <summary>
    /// Расширение, обрабатывающее запрос на обогащение подписей до архивного формата. 
    /// </summary>
    /// <param name="signatureArchiveManager"><inheritdoc cref="ISignatureArchiveManager" path="/summary"/></param>
    public sealed class SignaturesArchiveRequestExtension(ISignatureArchiveManager signatureArchiveManager) :
        CardRequestExtension
    {
        #region Constants

        public const string ArchiveSignatureInfoKey = StorageHelper.SystemKeyPrefix + "archiveSignatureInfo";

        #endregion

        #region Fields

        /// <inheritdoc cref="ISignatureArchiveManager" path="/summary"/>
        private readonly ISignatureArchiveManager signatureArchiveManager = NotNullOrThrow(signatureArchiveManager);

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async Task AfterRequest(ICardRequestExtensionContext context)
        {
            if (!context.RequestIsSuccessful)
            {
                return;
            }

            var info = context.Request.Info;

            var archiveSignatureInfo = info.GetSerializedList<SignaturesArchiveRequestInfo>(ArchiveSignatureInfoKey);

            foreach (var cardInfo in archiveSignatureInfo)
            {
                context.ValidationResult.Add(
                    await this.signatureArchiveManager.ArchiveSignaturesAsync(cardInfo.CardID, cardInfo.FileIDs, context.CancellationToken)
                );
            }
        }

        #endregion
    }
}
