using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Server.Workflow.KrObjectModel;
using Tessa.Extensions.Default.Server.Workflow.KrProcess;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Scope;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow;
using Tessa.Extensions.Default.Shared;
using Tessa.Extensions.Default.Shared.Workflow.KrCompilers;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Workflow.KrCompilers.Requests
{
    /// <summary>
    /// Расширение на процесс сохранения карточки, выполняющее пересчёт маршрута по требованию.
    /// </summary>
    /// <param name="typesCache"><inheritdoc cref="IKrTypesCache"/></param>
    /// <param name="executor"><inheritdoc cref="IKrExecutor"/></param>
    /// <param name="krScope"><inheritdoc cref="IKrScope"/></param>
    /// <param name="mapper"><inheritdoc cref="IObjectModelMapper"/></param>
    /// <param name="lockStrategy"><inheritdoc cref="IKrStageTemplateLockStrategy"/></param>
    public sealed class KrRecalcStagesStoreExtension(
        IKrTypesCache typesCache,
        IKrExecutor executor,
        IKrScope krScope,
        IObjectModelMapper mapper,
        IKrStageTemplateLockStrategy lockStrategy) :
        CardStoreExtension
    {
        #region Constants

        private const string UnknownLiteral = "unknown";

        #endregion

        #region Fields

        private readonly IKrTypesCache typesCache = NotNullOrThrow(typesCache);
        private readonly IKrExecutor executor = NotNullOrThrow(executor);
        private readonly IKrScope krScope = NotNullOrThrow(krScope);
        private readonly IObjectModelMapper mapper = NotNullOrThrow(mapper);
        private readonly IKrStageTemplateLockStrategy lockStrategy = NotNullOrThrow(lockStrategy);

        private bool? hasChangesInfo;

        private IList<RouteDiff> diffsInfo;

        #endregion

        #region Private Methods

        private static string FormatActualName(RouteDiff diff) =>
            $"\"{Localize(diff.ActualName ?? diff.OldName ?? UnknownLiteral)}\"";

        private static string FormatOldName(RouteDiff diff)
        {
            if (!string.Equals(diff.ActualName, diff.OldName, StringComparison.Ordinal))
            {
                var renamedFrom = LocalizeName("KrCompilation_RouteElementRenamedFrom");
                return $"({renamedFrom} \"{Localize(diff.OldName)}\")";
            }

            return string.Empty;
        }

        private static string FormatHidden(RouteDiff diff) =>
            diff.HiddenStage
                ? $" ({LocalizeName("KrCompilation_RecalcChanges_HiddenStage")})"
                : string.Empty;

        private void ChangesToResponse(
            InfoAboutChanges infoAboutChanges,
            IList<RouteDiff> diffs,
            IValidationResultBuilder validationResult)
        {
            if (infoAboutChanges == InfoAboutChanges.None)
            {
                return;
            }

            if (infoAboutChanges.HasNot(InfoAboutChanges.ChangesInHiddenStages))
            {
                diffs = diffs
                    .Where(static p => !p.HiddenStage)
                    .ToList();
            }

            var hasChanges = diffs.Count > 0;

            if (infoAboutChanges.Has(InfoAboutChanges.HasChangesToInfo))
            {
                this.hasChangesInfo = hasChanges;
            }

            if (hasChanges)
            {
                if (infoAboutChanges.Has(InfoAboutChanges.HasChangesToValidationResult)
                    && infoAboutChanges.HasNot(InfoAboutChanges.ChangesListToValidationResult))
                {
                    ValidationSequence
                        .Begin(validationResult)
                        .Warning(DefaultValidationKeys.RecalcWithChanges)
                        .End();
                }
            }
            else
            {
                if (infoAboutChanges.HasAny(InfoAboutChanges.ToValidationResult))
                {
                    ValidationSequence
                        .Begin(validationResult)
                        .Warning(DefaultValidationKeys.RecalcWithoutChanges)
                        .End();
                }
            }

            if (infoAboutChanges.Has(InfoAboutChanges.ChangesListToInfo))
            {
                this.diffsInfo = diffs;
            }

            if (infoAboutChanges.Has(InfoAboutChanges.ChangesListToValidationResult))
            {
                this.ChangesListToValidationResult(diffs, validationResult);
            }
        }

        private void ChangesListToValidationResult(
            IList<RouteDiff> diffs,
            IValidationResultBuilder validationResult)
        {
            foreach (var diff in diffs)
            {
                var validator = ValidationSequence
                    .Begin(validationResult)
                    .SetObjectName(this);

                switch (diff.Action)
                {
                    case RouteDiffAction.Insert:
                        validator.Warning(DefaultValidationKeys.StageAdded, FormatActualName(diff), FormatHidden(diff));
                        break;
                    case RouteDiffAction.Delete:
                        validator.Warning(DefaultValidationKeys.StageDeleted, FormatActualName(diff), FormatHidden(diff));
                        break;
                    case RouteDiffAction.Modify:
                        validator.Warning(DefaultValidationKeys.StageModified, FormatActualName(diff), FormatOldName(diff), FormatHidden(diff));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(diff) + "." + nameof(diff.Action), diff.Action, null);
                }

                validator.End();
            }
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override Task BeforeRequest(
            ICardStoreExtensionContext context)
        {
            if (!context.ValidationResult.IsSuccessful()
                || !context.Request.GetRecalcFlag())
            {
                return Task.CompletedTask;
            }

            context.Request.ForceTransaction = true;
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public override async Task BeforeCommitTransaction(ICardStoreExtensionContext context)
        {
            if (!context.ValidationResult.IsSuccessful()
                || !context.Request.GetRecalcFlag())
            {
                return;
            }

            if (!await KrProcessHelper.CardSupportsRoutesAsync(
                context.Request.Card,
                context.DbScope,
                this.typesCache,
                context.CancellationToken))
            {
                return;
            }

            if (await this.lockStrategy.ObtainReaderLockAsync(context.CancellationToken) is not { IsSuccessful: true })
            {
                context.ValidationResult.AddError(
                    this,
                    "$KrMessages_StageTemplatesReadErrorMessage");
                return;
            }
            if (await this.krScope.GetKrSatelliteAsync(
                context.Request.Card.ID,
                cancellationToken: context.CancellationToken) is not { } satellite)
            {
                return;
            }

            if (satellite.IsMainProcessStarted())
            {
                ValidationSequence
                    .Begin(context.ValidationResult)
                    .Error(DefaultValidationKeys.MainProcessStarted)
                    .End();
                return;
            }

            NestedStagesCleaner.ClearAll(satellite);
            var pci = this.mapper.GetMainProcessCommonInfo(satellite);

            var objectModel = await this.mapper.CardRowsToObjectModelAsync(
                satellite,
                pci,
                pci,
                KrConstants.KrProcessName,
                initialStage: true,
                cancellationToken: context.CancellationToken);

            this.mapper.FillWorkflowProcessFromPci(
                objectModel,
                pci,
                pci);

            var card = context.Request.Card;
            var docTypeID = await KrProcessSharedHelper.GetDocTypeIDAsync(
                card,
                context.DbScope,
                context.CancellationToken);

            var components = await KrComponentsHelper.GetKrComponentsAsync(
                card.TypeID,
                docTypeID,
                this.typesCache,
                context.CancellationToken);

            await using var mainCardAccessStrategy = new KrScopeMainCardAccessStrategy(card.ID, this.krScope);

            var ctx = new KrExecutionContext(
                cardContext: context,
                mainCardAccessStrategy: mainCardAccessStrategy,
                cardID: card.ID,
                cardType: context.CardType,
                docTypeID: docTypeID,
                krComponents: components,
                workflowProcess: objectModel,
                validationResult: context.ValidationResult,
                cancellationToken: context.CancellationToken);

            await this.executor.ExecuteAsync(ctx);

            if (!context.ValidationResult.IsSuccessful())
            {
                return;
            }

            var diffs = await this.mapper.ObjectModelToCardRowsAsync(
                ctx.WorkflowProcess,
                satellite,
                null,
                context.CancellationToken);

            this.mapper.ObjectModelToPci(
                ctx.WorkflowProcess,
                pci,
                pci,
                pci);

            if (await mainCardAccessStrategy.GetCardAsync(cancellationToken: context.CancellationToken) is not { } mainCard)
            {
                return;
            }

            await this.mapper.SetMainProcessCommonInfoAsync(
                mainCard,
                satellite,
                pci,
                context.CancellationToken);

            if (context.ValidationResult.IsSuccessful())
            {
                // Информация о скрытых этапах отображается, если они отображаются в карточке.
                var mode = context.Request.GetInfoAboutChanges() ??
                    (card.StoreMode == CardStoreMode.Update
                    && card.GetStagePositions()?.All(static i => i.ShiftedOrder.HasValue) == true
                    ? InfoAboutChanges.ChangesListToValidationResult | InfoAboutChanges.ChangesInHiddenStages
                    : InfoAboutChanges.ChangesListToValidationResult);

                this.ChangesToResponse(
                    mode,
                    diffs,
                    context.ValidationResult);
            }
        }

        /// <inheritdoc/>
        public override Task AfterRequest(ICardStoreExtensionContext context)
        {
            if (this.hasChangesInfo.HasValue)
            {
                context.Response.SetHasRecalcChanges(this.hasChangesInfo.Value);
            }

            if (this.diffsInfo is not null)
            {
                context.Response.SetRecalcChanges(this.diffsInfo);
            }

            return Task.CompletedTask;
        }

        #endregion
    }
}
