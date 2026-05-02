#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager
{
    /// <summary>
    /// Базовая реализация <see cref="IKrTaskWithParametersManagerDataProvider{T}"/>.
    /// </summary>
    /// <typeparam name="T"><inheritdoc cref="IKrTaskWithParametersManagerDataProvider{T}" path="/typeparam[@name='T']"/></typeparam>
    public abstract class KrTaskWithParametersManagerDataProviderBase<T> :
        KrTaskWithActionsManagerDataProviderBase<T>,
        IKrTaskWithParametersManagerDataProvider<T>
    {
        #region IKrTaskWithParametersManagerDataProvider<T> Members

        /// <inheritdoc/>
        public abstract ValueTask<IReadOnlyList<T>> GetPerformersAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default);

        /// <inheritdoc/>
        public virtual ValueTask ResetStoredPerformersAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) => default;

        /// <inheritdoc/>
        public virtual ValueTask<Guid?> GetAuthorIDAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) => default;

        /// <inheritdoc/>
        public virtual ValueTask<(Guid? ID, string? Caption)> GetKindAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) => default;

        /// <inheritdoc/>
        public virtual ValueTask<string?> GetDigestAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) => default;

        /// <inheritdoc/>
        public virtual ValueTask<double?> GetPlannedWorkingDaysAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) => default;

        public virtual ValueTask<int?> GetPlannedQuantsAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) => default;

        /// <inheritdoc/>
        public virtual ValueTask<DateTime?> GetPlannedAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) => default;

        /// <inheritdoc/>
        public abstract ValueTask<Guid?> GetParentTaskRowIDAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default);

        #endregion
    }
}
