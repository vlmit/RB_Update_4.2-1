#nullable enable

using System;
using System.Threading;
using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow
{
    /// <summary>
    /// Контекст <see cref="IKrProcessButtonVisibilityEvaluator"/>.
    /// </summary>
    public interface IKrProcessButtonVisibilityEvaluatorContext
    {
        /// <inheritdoc cref="IValidationResultBuilder" path="/summary"/>
        IValidationResultBuilder ValidationResult { get; }

        /// <inheritdoc cref="IMainCardAccessStrategy" path="/summary"/>
        IMainCardAccessStrategy MainCardAccessStrategy { get; }

        /// <summary>
        /// Карточка.
        /// </summary>
        Card? Card { get; }

        /// <summary>
        /// Тип карточки.
        /// </summary>
        CardType? CardType { get; }

        /// <summary>
        /// Идентификатор типа документа.
        /// </summary>
        Guid? DocTypeID { get; }

        /// <summary>
        /// Включенные компоненты типового решения для текущей карточки.
        /// </summary>
        KrComponents? KrComponents { get; }

        /// <inheritdoc cref="KrState" path="/summary"/>
        KrState? State { get; }

        /// <inheritdoc cref="ICardExtensionContext" path="/summary"/>
        ICardExtensionContext? CardContext { get; }

        /// <summary>
        /// Объект, посредством которого можно отменить асинхронную задачу.
        /// </summary>
        CancellationToken CancellationToken { get; }
    }
}
