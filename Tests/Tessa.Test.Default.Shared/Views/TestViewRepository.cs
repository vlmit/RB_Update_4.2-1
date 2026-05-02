#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Views;
using Tessa.Views.Metadata;

namespace Tessa.Test.Default.Shared.Views
{
    /// <summary>
    /// Репозиторий для обращения к фальшивому сервису представлений.
    /// </summary>
    /// <param name="mediator"><inheritdoc cref="IMediatorServer" path="/summary"/></param>
    /// <param name="fakeModelGenerator">Генератор фальшивых моделей представлений, предназначенных для тестирования.</param>
    /// <remarks>
    /// Реализация не поддерживает импорт представлений <see cref="ImportAsync"/>.
    /// </remarks>
    public class TestViewRepository(
        IMediatorServer mediator,
        Func<IEnumerable<TessaViewModel>>? fakeModelGenerator = null)
        : IViewRepository
    {
        #region Fields

        private readonly List<TessaViewModel> models = fakeModelGenerator?.Invoke().ToList() ?? [];

        #endregion

        #region ITessaViewRepository Members

        /// <inheritdoc/>
        public async Task ChangeAsync(IViewStoreRequest request, CancellationToken cancellationToken = default)
        {
            ThrowIfNull(request);

            if (request.Models.Count == 0)
            {
                return;
            }

            await this.DeleteAsync(request, cancellationToken);
            await this.NewAsync(request, cancellationToken);
        }

        /// <inheritdoc/>
        public Task DeleteAsync(IViewStoreRequest request, CancellationToken cancellationToken = default)
        {
            ThrowIfNull(request);

            if (request.Models.Count == 0)
            {
                return Task.CompletedTask;
            }

            foreach (var model in request.Models)
            {
                this.models.RemoveAll(x => x.Id == model.Id);
            }

            mediator.Notify();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<List<TessaViewModel>> GetAsync(
            IViewGetRequest request,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(request);

            List<TessaViewModel> result =
                request.RequestedModels.Count == 0
                    ? this.models.ToList()
                    : request.RequestedModels.Select(modelId => this.models.FirstOrDefault(x => x.Id == modelId))
                        .Where(model => model is not null)
                        .ToList()!;

            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public Task ImportAsync(IViewImportRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        /// <inheritdoc/>
        public Task NewAsync(IViewStoreRequest request, CancellationToken cancellationToken = default)
        {
            ThrowIfNull(request);

            if (request.Models.Count == 0)
            {
                return Task.CompletedTask;
            }

            foreach (var model in request.Models)
            {
                if (this.models.Exists(m => ParserNames.IsEquals(m.Alias, model.Alias)))
                {
                    throw new UniqueAliasException(model.Alias);
                }

                this.models.Add(model);
            }

            mediator.Notify();
            return Task.CompletedTask;
        }

        #endregion
    }
}
