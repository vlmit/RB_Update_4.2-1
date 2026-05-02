#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Tessa.Platform;
using Tessa.Platform.Validation;
using Tessa.Test.Default.Shared.Kr;

namespace Tessa.Test.Default.Shared.Validation
{
    /// <summary>
    /// Объект, предоставляющий методы для работы с отложенными действиями, используемыми в <see cref="ValidatorBase{TObject, TValue}"/>.
    /// </summary>
    public sealed class ValidatorPendingActionsProvider :
        PendingActionsProvider<IPendingAction, ValidatorPendingActionsProvider>
    {
        #region Fields

        private readonly ISealable sealable;

        private readonly Func<string?> getValueDescriptionFunc;

        #endregion

        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса.
        /// </summary>
        /// <param name="sealable">Объект, предоставляющий информацию о возможности внесения изменений в валидатор.</param>
        /// <param name="getValueDescriptionFunc">Метод, возвращающий строковое описание проверяемого объекта или значение <see langword="null"/>, или <see cref="string.Empty"/>, если его не надо включать в результаты валидации.</param>
        public ValidatorPendingActionsProvider(
            ISealable sealable,
            Func<string?> getValueDescriptionFunc)
        {
            this.sealable = NotNullOrThrow(sealable);
            this.getValueDescriptionFunc = NotNullOrThrow(getValueDescriptionFunc);
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async ValueTask<ValidatorPendingActionsProvider> GoAsync(
            Action<ValidationResult>? validationFunc = null,
            CancellationToken cancellationToken = default)
        {
            if (!this.HasPendingActions)
            {
                return this;
            }

            this.PreparePendingActions(this.PendingActions);
            this.Seal();

            try
            {
                await this.GoCoreAsync(
                    validationFunc: validationFunc,
                    cancellationToken: cancellationToken);
            }
            finally
            {
                if (!this.sealable.IsSealed)
                {
                    this.PendingActions.Clear();
                    this.IsSealed = false;
                }
            }

            return this;
        }

        /// <inheritdoc/>
        protected override async ValueTask<ValidatorPendingActionsProvider> GoCoreAsync(
            Action<ValidationResult>? validationFunc = null,
            CancellationToken cancellationToken = default)
        {
            validationFunc ??= static result =>
            {
                if (result.Items.Count > 0)
                {
                    Assert.Fail(result.ToString(ValidationLevel.Message) ?? string.Empty);
                }
            };

            var validationResult = new ValidationResultBuilder();

            foreach (var pendingAction in this)
            {
                validationResult.Add(await pendingAction.ExecuteAsync(cancellationToken));
            }

            if (validationResult.Count > 0)
            {
                var description = this.getValueDescriptionFunc();

                if (!string.IsNullOrEmpty(description))
                {
                    validationResult.AddInfo(
                        this,
                        $"Validate object:{Environment.NewLine}{description}");
                }
            }

            validationFunc(validationResult.Build());

            return this;
        }

        #endregion
    }
}
