#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Serialization;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess
{
    /// <inheritdoc cref="IKrStageRowChecker"/>
    /// <param name="krStageSerializer"><inheritdoc cref="IKrStageSerializer" path="/summary"/></param>
    /// <param name="signatureProvider"><inheritdoc cref="ISignatureProvider" path="/summary"/></param>
    public sealed class KrStageRowChecker(
        IKrStageSerializer krStageSerializer,
        ISignatureProvider signatureProvider) :
        IKrStageRowChecker
    {
        #region Fields

        private readonly IKrStageSerializer krStageSerializer = NotNullOrThrow(krStageSerializer);
        private readonly ISignatureProvider signatureProvider = NotNullOrThrow(signatureProvider);

        #endregion

        #region Properties

        /// <summary>
        /// Поля в строке этапа, игнорируемые при отслеживании изменений.
        /// </summary>
        public static IReadOnlySet<string> UntrackedFields { get; } = new HashSet<string>(StringComparer.Ordinal)
        {
            CardRow.SystemChangedKey,
            CardRow.SystemStateKey,
            KrConstants.Keys.NestedStage,
            KrConstants.Keys.ParentStageRowID,
            KrConstants.Keys.RootStage,
            KrConstants.KrStages.BasedOnStageRowID,
            KrConstants.KrStages.BasedOnStageTemplateID,
            KrConstants.KrStages.DisplayParticipants,
            KrConstants.KrStages.DisplaySettings,
            KrConstants.KrStages.DisplayTimeLimit,
            KrConstants.KrStages.Order,
            KrConstants.KrStages.OrderChanged,
            KrConstants.KrStages.OriginalOrder,
            KrConstants.KrStages.RowChanged,
            KrConstants.KrStages.RowID,
            KrConstants.KrStages.Skip,
            KrConstants.KrStages.StateID,
            KrConstants.KrStages.StateName,
        };

        #endregion

        #region IKrStageRowChecker Members

        /// <inheritdoc/>
        public async ValueTask BeforeModifySatelliteAsync(
            Card card,
            Card krSatellite,
            ISet<Guid> changedOrders,
            ISet<Guid> changedSettings,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(card);
            ThrowIfNull(krSatellite);
            ThrowIfNull(changedOrders);
            ThrowIfNull(changedSettings);

            if (card.TryGetStagesSection(out var mainCardStages))
            {
                if (krSatellite.TryGetStagesSection(out var satelliteStages))
                {
                    CheckOrderSections(
                        mainCardStages.Rows,
                        satelliteStages.Rows,
                        changedOrders);
                }

                CheckPlainRowChanges(
                    mainCardStages.Rows,
                    changedSettings);
            }

            await this.CheckChildRowsChangesAsync(
                card.Sections,
                changedSettings,
                cancellationToken);
        }

        /// <inheritdoc/>
        public async ValueTask AfterModifySatelliteAsync(
            Card card,
            Card krSatellite,
            ISet<Guid> changedOrders,
            ISet<Guid> changedSettings,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(card);
            ThrowIfNull(krSatellite);
            ThrowIfNull(changedOrders);
            ThrowIfNull(changedSettings);

            if (!krSatellite.TryGetStagesSection(out var satelliteStages)
                || satelliteStages.TryGetRows() is not { Count: > 0 } satelliteStagesRows
                || !card.TryGetStagesSection(out var cardStages)
                || cardStages.TryGetRows() is not { Count: > 0 } cardStagesRows)
            {
                return;
            }

            var signatures = StageRowMigrationHelper.TryGetSignatures(card.Info);
            var stagePositions = card.GetStagePositions();
            var isCreateFromTemplate = card.Info.ContainsKey(CardHelper.CardWasCopiedFromIDKey)
                || card.Info.ContainsKey(CardHelper.CardWasCreatedFromTemplateIDKey);

            foreach (var row in satelliteStagesRows)
            {
                var rowID = row.RowID;
                if (row.State != CardRowState.Inserted
                    || cardStagesRows.FirstOrDefault(i => i.RowID == rowID) is not { State: CardRowState.Inserted })
                {
                    continue;
                }

                var rowChanged = true;
                var orderChanged = true;

                if (isCreateFromTemplate)
                {
                    if (!changedSettings.Contains(rowID))
                    {
                        rowChanged = await StageRowMigrationHelper.VerifyRowSettingsChangedAsync(
                            row,
                            signatures,
                            this.krStageSerializer,
                            this.signatureProvider,
                            cancellationToken);
                    }

                    if (!changedOrders.Contains(rowID))
                    {
                        orderChanged = StageRowMigrationHelper.VerifyRowOrderChanged(
                            row,
                            stagePositions,
                            isCreateFromTemplate);
                    }
                }

                if (rowChanged)
                {
                    changedSettings.Add(rowID);
                }

                if (orderChanged)
                {
                    changedOrders.Add(rowID);
                }
            }
        }

        /// <inheritdoc/>
        public async ValueTask<bool> HasAnySettingsSectionAsync(
            Card card,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(card);

            var serializerSettings = await this.krStageSerializer.GetSettingsAsync(cancellationToken);
            var mainCardSections = card.Sections;

            return serializerSettings.SettingsSectionNames.Any(mainCardSections.ContainsKey);
        }

        #endregion

        #region Private Methods

        private static void CheckOrderSections(
            IReadOnlyCollection<CardRow> mainStages,
            IReadOnlyCollection<CardRow> satelliteStages,
            ISet<Guid> changedOrders)
        {
            // 1. Сортировка сохранённых в сателлите этапов.
            var originalStages = satelliteStages
                .Where(static p => p.ContainsKey(KrConstants.KrStages.Order))
                .OrderBy(static p => p.Get<int>(KrConstants.KrStages.Order))
                .ToArray();

            var originalLength = originalStages.Length;

            // 2. Сортировка изменённых этапов (все этапы кроме удаленных).
            var changedStages = mainStages
                .Where(static i =>
                    i.State is CardRowState.Modified or CardRowState.Inserted
                    && i.ContainsKey(KrConstants.KrStages.Order))
                .OrderBy(static i => i.Get<int>(KrConstants.KrStages.Order))
                .ToArray();

            if (changedStages.Length == 0)
            {
                return;
            }

            var changedStagesModified = changedStages
                .Where(static i => i.State == CardRowState.Modified)
                .ToArray();

            if (changedStagesModified.Length > 0)
            {
                // 3. Будем вычеркивать исходные этапы, которые отсутствуют в измененном наборе.
                var crossedOutItems = new bool[originalLength];

                // 4. Спроецировать измененные на исходные - получаем измененные, но в исходном порядке.
                for (var originalStageIDIndex = 0;
                    originalStageIDIndex < originalLength;
                    originalStageIDIndex++)
                {
                    var hasStage = changedStagesModified
                        .Any(i => i.RowID == originalStages[originalStageIDIndex].RowID);

                    if (!hasStage)
                    {
                        // Для простоты лишние элементы не удаляются, а лишь помечаются.
                        crossedOutItems[originalStageIDIndex] = true;
                    }
                }

                // 5. Проходимся попарно по массивам - несовпадающие пары свидетельствуют об изменении порядка.
                var changedStageIndex = 0;
                for (var originalStageIDIndex = 0;
                    originalStageIDIndex < originalLength;
                    originalStageIDIndex++)
                {
                    // Этапа нет среди изменённых?
                    if (crossedOutItems[originalStageIDIndex])
                    {
                        continue;
                    }

                    var changedStageRowID = changedStagesModified[changedStageIndex].RowID;
                    changedStageIndex++;

                    if (originalStages[originalStageIDIndex].RowID != changedStageRowID)
                    {
                        changedOrders.Add(changedStageRowID);
                    }
                }
            }

            // 6. Определение изменения положения этапов из-за изменения положения несохранённых вручную добавленных этапов.
            foreach (var row in mainStages
                        .Where(static p =>
                            p.State == CardRowState.Inserted
                            && !p.Fields.TryGet<Guid?>(KrConstants.KrStages.BasedOnStageRowID).HasValue))
            {
                var order = row.TryGet<int?>(KrConstants.KrStages.Order);
                var originalOrder = row.TryGet<int?>(KrConstants.KrStages.OriginalOrder);

                if (order.HasValue
                    && originalOrder.HasValue
                    && order != originalOrder)
                {
                    int startOrder;
                    int endOrder;

                    if (order < originalOrder)
                    {
                        startOrder = order.Value;
                        endOrder = originalOrder.Value;
                    }
                    else
                    {
                        startOrder = originalOrder.Value;
                        endOrder = order.Value;
                    }

                    foreach (var changedStage in changedStages)
                    {
                        var changedStageOrder = changedStage.Get<int>(KrConstants.KrStages.Order);

                        if (changedStageOrder < startOrder)
                        {
                            continue;
                        }

                        changedOrders.Add(changedStage.RowID);

                        if (changedStageOrder == endOrder)
                        {
                            break;
                        }
                    }
                }
            }
        }

        private static void CheckPlainRowChanges(
            IEnumerable<CardRow> mainCardStages,
            ISet<Guid> changedSettings)
        {
            foreach (var modifiedStage in mainCardStages)
            {
                if (modifiedStage.State != CardRowState.Modified
                    || modifiedStage.Fields.Keys.All(UntrackedFields.Contains))
                {
                    continue;
                }

                changedSettings.Add(modifiedStage.RowID);
            }
        }

        private async ValueTask CheckChildRowsChangesAsync(
            IReadOnlyDictionary<string, CardSection> mainCardSections,
            ISet<Guid> changedSettings,
            CancellationToken cancellationToken = default)
        {
            var serializerSettings = await this.krStageSerializer.GetSettingsAsync(cancellationToken);

            foreach (var settingsSectionName in serializerSettings.SettingsSectionNames)
            {
                if (settingsSectionName == KrConstants.KrStages.Virtual)
                {
                    continue;
                }

                if (mainCardSections.TryGetValue(settingsSectionName, out var settingsSec)
                    && settingsSec.TryGetRows() is { } rows)
                {
                    foreach (var row in rows)
                    {
                        var parentID = row.TryGet<Guid?>(KrConstants.Keys.ParentStageRowID);
                        if (parentID.HasValue)
                        {
                            changedSettings.Add(parentID.Value);
                        }
                    }
                }
            }
        }

        #endregion
    }
}
