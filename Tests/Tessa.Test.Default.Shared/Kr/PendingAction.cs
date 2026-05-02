#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Platform;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;

namespace Tessa.Test.Default.Shared.Kr
{
    /// <inheritdoc cref="IPendingAction"/>
    [DebuggerDisplay("Name = {" + nameof(Name) + "}, Preparation actions count = {" + nameof(PreparationActions) + ".Count}")]
    public class PendingAction :
        IPendingAction
    {
        #region Fields

        /// <summary>
        /// Метод реализующий действие.
        /// </summary>
        private readonly Func<IPendingAction, CancellationToken, ValueTask<ValidationResult>> actionAsync;

        private readonly Collection<IPendingAction> preparationActions = new Collection<IPendingAction>();

        private readonly Collection<IPendingAction> afterActions = new Collection<IPendingAction>();

        private Dictionary<string, object?>? info;

        #endregion

        #region Properties

        /// <inheritdoc/>
        public string Name { get; }

        /// <inheritdoc/>
        public IReadOnlyCollection<IPendingAction> PreparationActions { get; }

        /// <inheritdoc/>
        public IReadOnlyCollection<IPendingAction> AfterActions { get; }

        /// <inheritdoc/>
        [AllowNull]
        public Dictionary<string, object?> Info
        {
            get => this.info ??= new Dictionary<string, object?>(StringComparer.Ordinal);
            private set => this.info = value;
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="PendingAction"/>.
        /// </summary>
        /// <param name="name">Название действия.</param>
        /// <param name="actionAsync">Метод реализующий действие.</param>
        public PendingAction(
            string name,
            Func<IPendingAction, CancellationToken, ValueTask<ValidationResult>> actionAsync)
        {
            this.Name = NotEmptyOrThrow(name);
            this.actionAsync = NotNullOrThrow(actionAsync);

            this.PreparationActions = new ReadOnlyCollection<IPendingAction>(this.preparationActions);
            this.AfterActions = new ReadOnlyCollection<IPendingAction>(this.afterActions);
        }

        #endregion

        #region Public methods

        /// <inheritdoc/>
        public async ValueTask<ValidationResult> ExecuteAsync(
            CancellationToken cancellationToken = default)
        {
            this.Seal();

            var validationResults = new ValidationResultBuilder();

            await this.ExecuteAdditionalActions(
                this.PreparationActions,
                validationResults,
                TestValidationKeys.PendingActionPreparationActionMessages,
                TestValidationKeys.PendingActionPreparationActionException,
                cancellationToken);

            if (validationResults.IsSuccessful())
            {
                try
                {
                    var validationResult = await this.actionAsync(this, cancellationToken)
                        ?? throw new InvalidOperationException($"The function {nameof(this.actionAsync)} returned null value.");

                    if (validationResult.Items.Count > 0)
                    {
                        ValidationSequence
                            .Begin(validationResults)
                            .SetObjectName(this)
                            .InfoText(
                                TestValidationKeys.PendingActionMessages,
                                string.Format(
                                    TestValidationKeys.PendingActionMessages.Message!,
                                    this.Name))
                            .End();

                        validationResults.Add(validationResult);
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    ValidationSequence
                        .Begin(validationResults)
                        .SetObjectName(this)
                        .ErrorDetails(
                            TestValidationKeys.PendingActionException,
                            string.Format(
                                TestValidationKeys.PendingActionException.Message!,
                                this.Name),
                            e)
                        .End();
                }
            }

            if (validationResults.IsSuccessful())
            {
                await this.ExecuteAdditionalActions(
                    this.AfterActions,
                    validationResults,
                    TestValidationKeys.PendingActionAfterActionMessages,
                    TestValidationKeys.PendingActionAfterActionException,
                    cancellationToken);
            }

            this.IsSealed = false;

            return validationResults.Build();
        }

        /// <inheritdoc/>
        public void SetInfo(
            Dictionary<string, object?> info,
            bool isReplaceInfo = false)
        {
            if (info is null || isReplaceInfo)
            {
                this.Info = info;
            }
            else
            {
                StorageHelper.Merge(info, this.Info);
            }
        }

        /// <inheritdoc/>
        public void AddPreparationAction(
            IPendingAction pendingAction)
        {
            ThrowIfNull(pendingAction);

            if (ReferenceEquals(pendingAction, this))
            {
                throw new ArgumentException("Can't add the action into its own actions list, which is used to execute before the pending action.", nameof(pendingAction));
            }

            ThrowIfSealed(this);

            this.preparationActions.Add(pendingAction);
        }

        /// <inheritdoc/>
        public void AddAfterAction(
            IPendingAction pendingAction)
        {
            ThrowIfNull(pendingAction);

            if (ReferenceEquals(pendingAction, this))
            {
                throw new ArgumentException("Can't add the action into its own actions list, which is used to execute after the pending action.", nameof(pendingAction));
            }

            ThrowIfSealed(this);

            this.afterActions.Add(pendingAction);
        }

        /// <inheritdoc/>
        public string ToString(int offset)
        {
            ThrowIf(offset, offset < 0);

            var sb = StringBuilderHelper.Acquire();
            this.AppendAction(sb, this, offset);
            return sb.ToStringAndRelease();
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override string ToString() => this.ToString(0);

        #endregion

        #region ISealable Members

        /// <inheritdoc/>
        public bool IsSealed { get; private set; }

        /// <inheritdoc/>
        public void Seal() => this.IsSealed = true;

        #endregion

        #region Protected Methods

        /// <summary>
        /// Добавляет информацию о действиях.
        /// </summary>
        /// <param name="sb">Объект, выполняющий построение строки с трассировкой.</param>
        /// <param name="pendingActions">Перечисление отложенных действий.</param>
        /// <param name="offset">Смещение в количестве табуляций относительно начала строк, добавляемые в начало каждой строки.</param>
        protected static void AppendActions(
            StringBuilder sb,
            IEnumerable<IPendingAction> pendingActions,
            int offset)
        {
            var index = 1;
            const string formatDisplayName = "Serial number {0}: {1}";

            foreach (var pendingAction in pendingActions)
            {
                if (index > 1)
                {
                    sb.AppendLine();
                }

                sb
                    .AppendSpaces(offset)
                    .AppendFormat(formatDisplayName, index++, pendingAction.ToString(offset))
                    ;
            }
        }

        /// <summary>
        /// Добавляет информацию о действии.
        /// </summary>
        /// <param name="sb">Объект, выполняющий построение строки с трассировкой.</param>
        /// <param name="pendingAction">Обрабатываемое действие.</param>
        /// <param name="offset">Смещение в количестве табуляций относительно начала строк, добавляемые в начало каждой строки.</param>
        protected void AppendAction(
            StringBuilder sb,
            IPendingAction pendingAction,
            int offset)
        {
            sb.Append(pendingAction.Name);

            if (pendingAction.Info.Count > 0)
            {
                sb
                    .AppendLine()
                    .AppendSpaces(offset)
                    .Append(nameof(this.Info)).Append(':').AppendLine()
                    ;

                StorageHelper.Print(sb, pendingAction.Info, offset);
            }

            this.AppendActionDetailsCore(sb, offset);
        }

        /// <summary>
        /// Добавляет дополнительную информацию о действии.
        /// </summary>
        /// <param name="sb">Объект, выполняющий построение строки с трассировкой.</param>
        /// <param name="offset">Смещение в количестве табуляций относительно начала строк, добавляемые в начало каждой строки.</param>
        protected virtual void AppendActionDetailsCore(
            StringBuilder sb,
            int offset)
        {
            if (this.PreparationActions.Any())
            {
                sb
                    .AppendLine()
                    .AppendSpaces(offset)
                    .Append(nameof(this.PreparationActions)).Append(':').AppendLine();

                AppendActions(sb, this.PreparationActions, offset + 1);
            }

            if (this.AfterActions.Any())
            {
                sb
                    .AppendLine()
                    .AppendSpaces(offset)
                    .Append(nameof(this.AfterActions)).Append(':').AppendLine();

                AppendActions(sb, this.AfterActions, offset + 1);
            }
        }

        #endregion

        #region Private Members

        private async Task ExecuteAdditionalActions(
            IEnumerable<IPendingAction> pendingActions,
            ValidationResultBuilder validationResults,
            ValidationKey pendingActionMessages,
            ValidationKey pendingActionException,
            CancellationToken cancellationToken = default)
        {
            foreach (var pendingAction in pendingActions)
            {
                try
                {
                    var validationResult = await pendingAction.ExecuteAsync(cancellationToken: cancellationToken);

                    if (validationResult?.Items.Count > 0)
                    {
                        ValidationSequence
                            .Begin(validationResults)
                            .SetObjectName(this)
                            .InfoText(
                                pendingActionMessages,
                                string.Format(
                                    pendingActionMessages.Message!,
                                    pendingAction.Name,
                                    this.Name))
                            .End();

                        validationResults.Add(validationResult);

                        if (!validationResult.IsSuccessful)
                        {
                            break;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    ValidationSequence
                        .Begin(validationResults)
                        .SetObjectName(this)
                        .ErrorDetails(
                            pendingActionException,
                            string.Format(
                                pendingActionException.Message!,
                                pendingAction.Name,
                                this.Name),
                            e)
                        .End();
                }
            }
        }

        #endregion
    }
}
