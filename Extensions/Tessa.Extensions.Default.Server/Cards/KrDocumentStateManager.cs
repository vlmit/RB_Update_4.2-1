#nullable enable

using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.Cards
{
    /// <inheritdoc cref="IKrDocumentStateManager" />
    /// <param name="cardMetadata"><inheritdoc cref="ICardMetadata" path="/summary"/></param>
    public sealed class KrDocumentStateManager(ICardMetadata cardMetadata) :
        IKrDocumentStateManager
    {
        #region Fields

        private readonly ICardMetadata cardMetadata = NotNullOrThrow(cardMetadata);

        #endregion

        #region IDocStateManager Members

        /// <inheritdoc/>
        public async ValueTask<(bool HasCardChanges, bool HasMainSatelliteChanges, KrState? OldState)> SetStateAsync(
            Card card,
            Card mainSatelliteCard,
            KrState state,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(card);
            ThrowIfNull(mainSatelliteCard);

            (bool HasCardChanges, bool HasMainSatelliteChanges, KrState? OldStateID) result = default;

            var cardMetadata = await this.cardMetadata.GetMetadataForTypeAsync(card.TypeID, cancellationToken);
            var cardMetadataSections = await cardMetadata.GetSectionsAsync(cancellationToken);
            string? stateName = null;

            if (cardMetadataSections.TryGetValue(KrConstants.DocumentCommonInfo.Name, out var documentCommonInfoMetadata))
            {
                var documentCommonInfoMetadataColumns = documentCommonInfoMetadata.Columns;

                if (documentCommonInfoMetadataColumns.Contains(KrConstants.DocumentCommonInfo.StateID))
                {
                    var fields = card.Sections.GetOrAddEntry(KrConstants.DocumentCommonInfo.Name).Fields;
                    var oldStateID = fields.TryGet<int?>(KrConstants.KrApprovalCommonInfo.StateID);

                    if (oldStateID != state.ID)
                    {
                        fields[KrConstants.DocumentCommonInfo.StateID] = Int32Boxes.Box(state.ID);

                        if (documentCommonInfoMetadataColumns.Contains(KrConstants.DocumentCommonInfo.StateName))
                        {
                            stateName = await this.cardMetadata.GetDocumentStateNameAsync(state, cancellationToken);
                            fields[KrConstants.DocumentCommonInfo.StateName] = stateName;
                        }

                        result.HasCardChanges = true;
                    }
                }
            }

            if (mainSatelliteCard.TryGetKrApprovalCommonInfoSection(out var krApprovalCommonInfoSection))
            {
                var fields = krApprovalCommonInfoSection.Fields;
                var oldStateID = fields.TryGet<int?>(KrConstants.KrApprovalCommonInfo.StateID);
                result.OldStateID = (KrState?) oldStateID;

                if (oldStateID != state.ID)
                {
                    fields[KrConstants.KrApprovalCommonInfo.StateID] = Int32Boxes.Box(state.ID);
                    fields[KrConstants.KrApprovalCommonInfo.StateName] = stateName ?? await this.cardMetadata.GetDocumentStateNameAsync(state, cancellationToken);
                    result.HasMainSatelliteChanges = true;
                }
            }

            return result;
        }

        #endregion
    }
}
