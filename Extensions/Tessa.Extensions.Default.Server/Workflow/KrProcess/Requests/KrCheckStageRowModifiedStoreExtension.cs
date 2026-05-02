#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Server.Workflow.KrCompilers;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Scope;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow;
using Tessa.Extensions.Default.Shared.Workflow.KrCompilers;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Requests
{
    /// <summary>
    /// Расширение проверяет, изменилась ли строка с этапом согласования в карточке.
    /// Если изменилась, делает соответствующую отметку в поле <see cref="KrConstants.KrStages.RowChanged"/> и/или <see cref="KrConstants.KrStages.OrderChanged"/>.
    /// </summary>
    /// <param name="typesCache"><inheritdoc cref="IKrTypesCache" path="/summary"/></param>
    /// <param name="scope"><inheritdoc cref="IKrScope" path="/summary"/></param>
    /// <param name="krProcessCache"><inheritdoc cref="IKrProcessCache" path="/summary"/></param>
    /// <param name="krStageRowChecker"><inheritdoc cref="IKrStageRowChecker" path="/summary"/></param>
    public sealed class KrCheckStageRowModifiedStoreExtension(
        IKrTypesCache typesCache,
        IKrScope scope,
        IKrProcessCache krProcessCache,
        IKrStageRowChecker krStageRowChecker) :
        CardStoreExtension
    {
        #region Constants And Static Fields

        private const string ChangedOrderInfoKey = nameof(ChangedOrderInfoKey);

        private const string ChangedRowInfoKey = nameof(ChangedRowInfoKey);

        #endregion

        #region Fields

        private readonly IKrTypesCache typesCache = NotNullOrThrow(typesCache);
        private readonly IKrScope scope = NotNullOrThrow(scope);
        private readonly IKrProcessCache krProcessCache = NotNullOrThrow(krProcessCache);
        private readonly IKrStageRowChecker krStageRowChecker = NotNullOrThrow(krStageRowChecker);

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async Task AfterBeginTransaction(ICardStoreExtensionContext context)
        {
            if (context.CardType is null
                || context.CardType.InstanceType != CardInstanceType.Card
                || context.CardType.Flags.Has(CardTypeFlags.Singleton)
                || !context.ValidationResult.IsSuccessful()
                || context.Request.TryGetCard() is not { } card)
            {
                return;
            }

            if (!await KrProcessHelper.CardSupportsRoutesAsync(
                    card,
                    context.DbScope!,
                    this.typesCache,
                    context.CancellationToken)
                || !await this.krStageRowChecker.HasAnySettingsSectionAsync(
                        card,
                        context.CancellationToken))
            {
                return;
            }

            var krSatellite = await this.scope.TryGetKrSatelliteAsync(
                card.ID,
                cancellationToken: context.CancellationToken);

            if (krSatellite is null)
            {
                return;
            }

            var changedOrders = new HashSet<Guid>();
            var changedRows = new HashSet<Guid>();

            await this.krStageRowChecker.BeforeModifySatelliteAsync(
                card,
                krSatellite,
                changedOrders,
                changedRows,
                context.CancellationToken);

            context.Info[ChangedOrderInfoKey] = changedOrders;
            context.Info[ChangedRowInfoKey] = changedRows;
        }

        /// <inheritdoc/>
        public override async Task BeforeCommitTransaction(
            ICardStoreExtensionContext context)
        {
            if (context.CardType is null
                || context.CardType.InstanceType != CardInstanceType.Card
                || context.CardType.Flags.Has(CardTypeFlags.Singleton)
                || !context.ValidationResult.IsSuccessful()
                || context.Request.TryGetCard() is not { } card)
            {
                return;
            }

            if (!await KrProcessHelper.CardSupportsRoutesAsync(
                    card,
                    context.DbScope!,
                    this.typesCache,
                    context.CancellationToken))
            {
                return;
            }

            var satellite = await this.scope.TryGetKrSatelliteAsync(
                card.ID,
                cancellationToken: context.CancellationToken);

            if (satellite?.TryGetStagesSection(out var satelliteStagesSection) != true)
            {
                return;
            }

            var changedRows = context.Info.TryGet<HashSet<Guid>>(ChangedRowInfoKey) ?? [];
            var changedOrders = context.Info.TryGet<HashSet<Guid>>(ChangedOrderInfoKey) ?? [];

            await this.krStageRowChecker.AfterModifySatelliteAsync(
                card,
                satellite,
                changedOrders,
                changedRows,
                context.CancellationToken);

            if (changedRows.Count == 0
                && changedOrders.Count == 0)
            {
                return;
            }

            foreach (var row in satelliteStagesSection.Rows)
            {
                if (changedRows.Contains(row.RowID))
                {
                    await this.SetRowChangedAsync(
                        row,
                        context.ValidationResult,
                        context.CancellationToken);

                    if (!context.ValidationResult.IsSuccessful())
                    {
                        return;
                    }
                }

                if (changedOrders.Contains(row.RowID))
                {
                    await this.SetOrderChangedAsync(
                        row,
                        context.ValidationResult,
                        context.CancellationToken);

                    if (!context.ValidationResult.IsSuccessful())
                    {
                        return;
                    }
                }
            }
        }

        #endregion

        #region Private Methods

        private async ValueTask SetRowChangedAsync(
            CardRow row,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            var basedOnStageTemplateID = row.TryGet<Guid?>(KrConstants.KrStages.BasedOnStageTemplateID);

            if (basedOnStageTemplateID.HasValue
                && !await this.GetIsStageReadonlyAsync(basedOnStageTemplateID.Value, cancellationToken))
            {
                validationResult.AddError(
                    this,
                    "$KrProcess_ChangingStageIsProhibited",
                    await LocalizeAsync(row.TryGet<string>(KrConstants.KrStages.NameField)));
                return;
            }

            row.Fields[KrConstants.KrStages.RowChanged] = BooleanBoxes.True;
        }

        private async ValueTask SetOrderChangedAsync(
            CardRow row,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            var basedOnStageTemplateID = row.TryGet<Guid?>(KrConstants.KrStages.BasedOnStageTemplateID);

            if (basedOnStageTemplateID.HasValue
                && !await this.GetCanChangeOrderAsync(basedOnStageTemplateID.Value, cancellationToken))
            {
                validationResult.AddError(
                    this,
                    "$KrProcess_ChangingOrderStageIsProhibited",
                    await LocalizeAsync(row.TryGet<string>(KrConstants.KrStages.NameField)));
                return;
            }

            var fields = row.Fields;
            fields[KrConstants.KrStages.OrderChanged] = BooleanBoxes.True;
            fields[KrConstants.KrStages.BasedOnStageTemplateGroupPositionID] = GroupPosition.Unspecified.ID;
            fields[KrConstants.KrStages.BasedOnStageTemplateOrder] = null;
        }

        /// <summary>
        /// Возвращает значение, показывающее, может ли быть изменён порядок этапа.
        /// </summary>
        /// <param name="basedOnStageTemplateID">Идентификатор шаблона этапов на основе которого был создан этап.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Значение <see langword="true"/>, если порядок этапа может быть изменён, иначе - <see langword="false"/>.</returns>
        private async ValueTask<bool> GetCanChangeOrderAsync(
            Guid basedOnStageTemplateID,
            CancellationToken cancellationToken = default)
        {
            var stageTemplates = await this.krProcessCache.GetAllStageTemplatesAsync(
                cancellationToken);

            if (!stageTemplates.TryGetValue(basedOnStageTemplateID, out var stageTemplate))
            {
                return true;
            }

            var stageGroups = await this.krProcessCache.GetAllStageGroupsAsync(
                cancellationToken);

            if (!stageGroups.TryGetValue(stageTemplate.StageGroupID, out var stageGroup))
            {
                return true;
            }

            return stageTemplate.CanChangeOrder && !stageGroup.IsGroupReadonly;
        }

        /// <summary>
        /// Возвращает значение, показывающее, может ли этап быть изменён.
        /// </summary>
        /// <param name="basedOnStageTemplateID">Идентификатор шаблона этапов на основе которого был создан этап.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Значение <see langword="true"/>, если этап может быть изменён, иначе - <see langword="false"/>.</returns>
        private async ValueTask<bool> GetIsStageReadonlyAsync(
            Guid basedOnStageTemplateID,
            CancellationToken cancellationToken = default)
        {
            var stageTemplates = await this.krProcessCache.GetAllStageTemplatesAsync(
                cancellationToken);

            if (!stageTemplates.TryGetValue(basedOnStageTemplateID, out var stageTemplate))
            {
                return true;
            }

            var stageGroups = await this.krProcessCache.GetAllStageGroupsAsync(
                cancellationToken);

            if (!stageGroups.TryGetValue(stageTemplate.StageGroupID, out var stageGroup))
            {
                return true;
            }

            return !stageTemplate.IsStagesReadonly && !stageGroup.IsGroupReadonly;
        }

        #endregion
    }
}
