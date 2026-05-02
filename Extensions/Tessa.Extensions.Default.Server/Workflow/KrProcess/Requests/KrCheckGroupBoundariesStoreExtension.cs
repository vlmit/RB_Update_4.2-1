using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Server.Workflow.KrCompilers;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Localization;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Requests
{
    /// <summary>
    /// Расширение на сохранение карточки, для которой включены маршруты документов.<para/>
    /// Проверяет правильность расположения этапов маршрута в сохраняемой карточке.
    /// </summary>
    public sealed class KrCheckGroupBoundariesStoreExtension :
        CardStoreExtension
    {
        #region Fields

        private readonly IKrTypesCache typesCache;

        private readonly IKrProcessCache processCache;

        #endregion

        #region Constructors

        public KrCheckGroupBoundariesStoreExtension(
            IKrTypesCache typesCache,
            IKrProcessCache processCache)
        {
            this.typesCache = NotNullOrThrow(typesCache);
            this.processCache = NotNullOrThrow(processCache);
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async Task BeforeRequestWhenTypeResolved(
            ICardStoreExtensionContext context)
        {
            if (!context.ValidationResult.IsSuccessful())
            {
                return;
            }

            var card = context.Request.Card;

            if (!await this.CardUsesDocTypesAsync(card))
            {
                return;
            }

            if (!KrProcessSharedHelper.TryGetDocTypeID(card, out var docTypeId, checkKrToken: true))
            {
                docTypeId = await KrProcessSharedHelper.GetDocTypeIDAsync(
                                card.ID,
                                context.DbScope,
                                context.CancellationToken);
            }

            if ((await KrComponentsHelper.GetKrComponentsAsync(
                        card.TypeID,
                        docTypeId,
                        this.typesCache,
                        context.CancellationToken))
                        .HasNot(KrComponents.Routes))
            {
                return;
            }

            if (!card.TryGetStagesSection(out var mainCardStagesSection)
                || !card.TryGetStagePositions(out var stagesPositions))
            {
                return;
            }

            await this.CheckMainCardBoundariesAsync(
                mainCardStagesSection.Rows,
                stagesPositions,
                context.ValidationResult,
                context.CancellationToken);
        }

        #endregion

        #region Private Methods

        private async Task CheckMainCardBoundariesAsync(
            IList<CardRow> mainCardStagesRows,
            List<KrStagePositionInfo> stagesPositions,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            if (mainCardStagesRows.Count == 0)
            {
                return;
            }

            var rows = mainCardStagesRows
                // Исключаем удаленные этапы
                .Where(static p => p.State != CardRowState.Deleted)
                .OrderBy(static p => p.TryGet<int?>(KrConstants.KrStages.Order));

            var currentOrder = int.MinValue;
            var groupsHashSet = await this.processCache.GetAllStageGroupsAsync(cancellationToken);

            foreach (var row in rows)
            {
                if ((row.TryGet<bool>(KrConstants.Keys.RootStage)
                    || row.TryGet<bool>(KrConstants.Keys.NestedStage))
                    && !row.All(static p =>
                        StorageHelper.HasPrefix(p.Key, CardHelper.UserKeyPrefix)
                        || p.Key == CardRow.SystemStateKey
                        || p.Key == CardRow.SystemChangedKey
                        || p.Key == KrConstants.KrStages.RowID
                        || p.Key == KrConstants.KrStages.Order))
                {
                    // Кто-то каким-то образом модифицировал нестед, что делать мы не разрешаем вообще.
                    validationResult.AddError(this, "$KrProcess_Error_TreeStructureStageModified");
                    return;
                }

                if (row.TryGet<int?>(KrConstants.KrStages.Order).HasValue)
                {
                    var rowID = row.RowID;
                    foreach (var position in stagesPositions)
                    {
                        if (position.RowID == rowID)
                        {
                            if (position.GroupOrder < currentOrder)
                            {
                                var stageName = position.Name;
                                var stageGroupName = groupsHashSet.TryGetValue(position.StageGroupID, out var group)
                                    ? group.Name
                                    : "unknown";
                                validationResult.AddWarning(
                                    this,
                                    "$KrMessages_ViolationOfGroupBoundaries",
                                    await LocalizeAsync(stageName),
                                    await LocalizeAsync(stageGroupName));
                                return;
                            }

                            currentOrder = position.GroupOrder;
                            break;
                        }
                    }
                }
            }
        }

        private async Task<bool> CardUsesDocTypesAsync(Card card, CancellationToken cancellationToken = default)
        {
            var components = await KrComponentsHelper.GetKrComponentsAsync(card.TypeID, this.typesCache, cancellationToken);
            return components.Has(KrComponents.DocTypes);
        }

        #endregion
    }
}
