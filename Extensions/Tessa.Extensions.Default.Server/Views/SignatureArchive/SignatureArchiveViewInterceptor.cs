#nullable enable

using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards.Caching;
using Tessa.Extensions.Default.Server.EDS.SignatureArchive;
using Tessa.Platform;
using Tessa.Platform.Data;
using Tessa.Platform.Validation;
using Tessa.Scheme;
using Tessa.Views;
using Tessa.Views.Metadata;

namespace Tessa.Extensions.Default.Server.Views.SignatureArchive
{
    /// <summary>
    /// Перехватчик представления 'SignatureArchive'.
    /// </summary>
    /// <param name="dbScope"><inheritdoc cref="IDbScope" path="/summary"/></param>
    /// <param name="viewQueryExecutor"><inheritdoc cref="IViewQueryExecutor" path="/summary"/></param>
    /// <param name="cardCache"><inheritdoc cref="ICardCache" path="/summary"/></param>
    /// <param name="viewNormalization"><inheritdoc cref="IViewNormalizationService" path="/summary"/></param>
    /// <param name="signatureArchivePermissionProvider"><inheritdoc cref="ISignatureArchivePermissionProvider" path="/summary"/></param>
    public sealed class SignatureArchiveViewInterceptor(
        IDbScope dbScope,
        IViewQueryExecutor viewQueryExecutor,
        ICardCache cardCache,
        IViewNormalizationService viewNormalization,
        ISignatureArchivePermissionProvider signatureArchivePermissionProvider) :
        ViewInterceptorBase(["SignatureArchive"])
    {
        #region Fields

        /// <inheritdoc cref="IDbScope" path="/summary"/>
        private readonly IDbScope dbScope = NotNullOrThrow(dbScope);

        /// <inheritdoc cref="IViewQueryExecutor" path="/summary"/>
        private readonly IViewQueryExecutor viewQueryExecutor = NotNullOrThrow(viewQueryExecutor);

        /// <inheritdoc cref="ICardCache" path="/summary"/>
        private readonly ICardCache cardCache = NotNullOrThrow(cardCache);

        /// <inheritdoc cref="IViewNormalizationService" path="/summary"/>
        private readonly IViewNormalizationService viewNormalization = NotNullOrThrow(viewNormalization);

        /// <inheritdoc cref="ISignatureArchivePermissionProvider" path="/summary"/>
        private readonly ISignatureArchivePermissionProvider signatureArchivePermissionProvider = NotNullOrThrow(signatureArchivePermissionProvider);

        #endregion

        #region Constants and Readonly Fields

        private const string conditionInjectionPlaceholder = "#conditions_on_table";

        private const string conditionInjectionPlaceholderKeyGroup = "ConditionsKey";

        /// <summary>
        /// Паттерн для поиска плейсхолдеров.
        /// </summary>
        private static readonly Regex Pattern = new(
            @$"{conditionInjectionPlaceholder}\((?<{conditionInjectionPlaceholderKeyGroup}>.+?)\)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        #endregion

        #region Base Overrides

        public override async ValueTask<ITessaViewResult> GetDataAsync(
            ITessaViewRequest request, 
            CancellationToken cancellationToken = default)
        {
            if (!await this.signatureArchivePermissionProvider.IsAdministratorAsync(cancellationToken))
            {
                throw new ValidationException(ValidationResult.FromText(this, "$UI_Common_PermissionDenied"));
            }

            var interceptedView = this.GetInterceptedView(request.ViewAlias);
            var (viewMetadata, viewMetadataResult) = await interceptedView.TryGetMetadataAsync(cancellationToken);

            if (!viewMetadataResult.IsSuccessful || viewMetadata is null)
            {
                throw new ValidationException(viewMetadataResult);
            }

            if (interceptedView is not IViewTextGenerator viewGenerator
                || await viewGenerator.TryGenerateAsync(request, cancellationToken) is not { } query)
            {
                return GetEmptyResult(request, viewMetadata);
            }

            var injectionPointResult = Pattern.Match(query);
            if (injectionPointResult is null)
            {
                // Нет вставок паттерна, запрос не может выполняться.
                return GetEmptyResult(request, viewMetadata);
            }

            var getSettingsCardResult = await this.cardCache.Cards.GetAsync("SignatureSettings", cancellationToken);

            if (!getSettingsCardResult.IsSuccess 
                || getSettingsCardResult.GetValue() is not { } settingsCard)
            {
                return GetEmptyResult(request, viewMetadata);
            }

            var archiveRulesRows = settingsCard.Sections.TryGet("SignatureSettingsArchiveRules")?.Rows;

            if (archiveRulesRows is not { Count: >0 })
            {
                return GetEmptyResult(request, viewMetadata);
            }

            var archiveRuleTypesRows = settingsCard.Sections.TryGet("SignatureSettingsArchiveRulesDocTypes")?.Rows;
            var archiveRuleCategoriesRows = settingsCard.Sections.TryGet("SignatureSettingsArchiveRulesFileCategories")?.Rows;

            var archiveRules = new ArchiveRuleCollection(
                archiveRulesRows,
                archiveRuleTypesRows,
                archiveRuleCategoriesRows,
                request,
                injectionPointResult.Groups[conditionInjectionPlaceholderKeyGroup].Value);

            await using var _ = this.dbScope.Create();

            var builder = this.dbScope.BuilderFactory.Create();
            var conditions = builder
                .Where(archiveRules.ToQueryExpression)
                .ToString()!;
            builder.Return();

            var countResult = await this.GetCountSubsetResultAsync(request, viewMetadata, viewGenerator, conditions, cancellationToken);

            if (!string.IsNullOrEmpty(request.SubsetName))
            {
                return countResult;
            }

            var newQuery = StringBuilderHelper
                .Acquire(query.Length - injectionPointResult.Length + conditions.Length)
                .Append(query[..injectionPointResult.Index])
                .Append(conditions)
                .Append(query[(injectionPointResult.Index + injectionPointResult.Length)..])
                .ToStringAndRelease();

            var viewResult = await this.viewQueryExecutor.ExecuteAsync(newQuery, viewMetadata, request, cancellationToken);

            await this.viewNormalization.NormalizeResultAsync(
                new ViewNormalizationContext(request, viewResult, viewMetadata),
                cancellationToken).ConfigureAwait(false);

            viewResult.RowCount = countResult.Rows.FirstOrDefault()?.FirstOrDefault() is { } countValue 
                ? Convert.ToInt64(countValue) 
                : viewResult.Rows.Count;

            return viewResult;
        }

        #endregion

        #region Private Methods

        private async Task<ITessaViewResult> GetCountSubsetResultAsync(
            ITessaViewRequest request,
            IViewMetadata viewMetadata,
            IViewTextGenerator viewTextGenerator,
            string conditions,
            CancellationToken cancellationToken = default)
        {
            var countRequest = new TessaViewRequest(request) { SubsetName = viewMetadata.RowCountSubset };
            var countQuery = await viewTextGenerator.TryGenerateAsync(countRequest, cancellationToken);

            var countInjectionPointResult = Pattern.Match(countQuery);

            if (countInjectionPointResult is null)
            {
                return new TessaViewResult { Columns = { (NotNullOrThrow(viewMetadata.RowCountSubset), SchemeType.Int64) } };
            }

            var newCountQuery = StringBuilderHelper
                .Acquire(countQuery.Length - countInjectionPointResult.Length + conditions.Length)
                .Append(countQuery[..countInjectionPointResult.Index])
                .Append(conditions)
                .Append(countQuery[(countInjectionPointResult.Index + countInjectionPointResult.Length)..])
                .ToStringAndRelease();

            return await this.viewQueryExecutor.ExecuteAsync(newCountQuery, viewMetadata, countRequest, cancellationToken);
        }

        private static TessaViewResult GetEmptyResult(
            ITessaViewRequest viewRequest,
            IViewMetadata viewMetadata) => 
            string.IsNullOrEmpty(viewRequest.SubsetName)
                ? new (viewMetadata)
                : new TessaViewResult { Columns = { (NotNullOrThrow(viewMetadata.RowCountSubset), SchemeType.Int64) }};

        #endregion
    }
}
