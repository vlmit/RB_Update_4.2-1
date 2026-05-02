#nullable enable

using System.Collections.Generic;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Scope
{
    /// <summary>
    /// Расширение на сохранение карточки, обрабатывающее результаты выполнения, записанные в <see cref="IKrScope"/>.
    /// </summary>
    public sealed class KrScopeStoreExtension :
        CardStoreExtension
    {
        #region Fields

        private readonly IKrTypesCache krTypesCache;
        private readonly IKrScope krScope;

        private List<KrProcessClientCommand>? clientCommands;
        private IValidationResultBuilder? scopeValidationResult;

        #endregion

        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="KrScopeStoreExtension"/>.
        /// </summary>
        /// <param name="krTypesCache"><inheritdoc cref="IKrTypesCache" path="/summary"/></param>
        /// <param name="krScope"><inheritdoc cref="IKrScope" path="/summary"/></param>
        public KrScopeStoreExtension(
            IKrTypesCache krTypesCache,
            IKrScope krScope)
        {
            this.krTypesCache = NotNullOrThrow(krTypesCache);
            this.krScope = NotNullOrThrow(krScope);
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override Task BeforeRequestWhenTypeResolved(ICardStoreExtensionContext context)
        {
            // Запрет передачи флага промежуточного сохранения с клиента
            if (context.Request.ServiceType != CardServiceType.Default)
            {
                context.Request.SetIntermediateApply(false);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public override async Task BeforeCommitTransaction(
            ICardStoreExtensionContext context)
        {
            if (!context.ValidationResult.IsSuccessful()
                || context.Request.IsIntermediateApply()
                || context.Request.TryGetCard() is not { } card
                || !await KrComponentsHelper.HasBaseAsync(card.TypeID, this.krTypesCache, context.CancellationToken))
            {
                return;
            }

            var currentLevel = this.krScope.CurrentLevel;
            if (currentLevel is not null)
            {
                await currentLevel.ApplyChangesAsync(
                    card.ID,
                    card.StoreMode == CardStoreMode.Update,
                    context.ValidationResult,
                    cancellationToken: context.CancellationToken);
                this.clientCommands = this.krScope.TryGetKrProcessClientCommands();
                this.scopeValidationResult = this.krScope.ValidationResult;

                context.ValidationResult.Add(this.scopeValidationResult);
                this.scopeValidationResult.Clear();
            }
        }

        /// <inheritdoc/>
        public override Task AfterRequest(
            ICardStoreExtensionContext context)
        {
            if (this.krScope.Exists)
            {
                return Task.CompletedTask;
            }

            if (this.clientCommands is not null)
            {
                context.Response.AddKrProcessClientCommands(this.clientCommands);
            }

            if (this.scopeValidationResult is not null)
            {
                var localScopeValidationResult = this.scopeValidationResult;
                this.scopeValidationResult = null;
                context.ValidationResult.Add(localScopeValidationResult);
                localScopeValidationResult.Clear();
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public override Task AfterRequestFinally(ICardStoreExtensionContext context)
        {
            if (this.krScope.Exists)
            {
                return Task.CompletedTask;
            }

            // Сохранение оставшихся результатов валидации, из-за невыполнения цепочки AfterRequest.
            if (this.scopeValidationResult is not null)
            {
                var localScopeValidationResult = this.scopeValidationResult;
                this.scopeValidationResult = null;
                context.ValidationResult.Add(localScopeValidationResult);
                localScopeValidationResult.Clear();
            }

            return Task.CompletedTask;
        }

        #endregion
    }
}
