#nullable enable

using System;
using System.Collections.Generic;
using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Server.Workflow.KrObjectModel;
using Tessa.Extensions.Default.Server.Workflow.KrProcess;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Workflow.KrCompilers
{
    /// <summary>
    /// Контекст <see cref="IKrExecutor"/>.
    /// </summary>
    public interface IKrExecutionContext :
        IExtensionContext
    {
        #region Properties

        /// <summary>
        /// Список идентификаторов единиц выполнения, которые необходимо выполнить, или <see langword="null"/>, если выполняются все единицы выполнения.
        /// </summary>
        ISet<Guid>? ExecutionUnitIDs { get; }

        /// <summary>
        /// Стратегия загрузки основной карточки.
        /// </summary>
        IMainCardAccessStrategy MainCardAccessStrategy { get; }

        /// <summary>
        /// Идентификатор текущей карточки или <see langword="null"/>, если она не задана.
        /// </summary>
        Guid? CardID { get; }

        /// <summary>
        /// Тип текущей карточки или <see langword="null"/>, если он не задан.
        /// </summary>
        CardType? CardType { get; }

        /// <summary>
        /// Идентификатор типа документа текущей карточки или <see langword="null"/>, если тип или карточка не заданы.
        /// </summary>
        Guid? DocTypeID { get; }

        /// <summary>
        /// Идентификатор типа карточки или документа.
        /// </summary>
        /// <remarks>Возвращает идентификатор типа документа, если он задан, иначе идентификатор типа карточки.</remarks>
        Guid? TypeID { get; }

        /// <summary>
        /// Включённые компоненты типового решения для текущей карточки или <see langword="null"/>, если карточка не задана.
        /// </summary>
        KrComponents? KrComponents { get; }

        /// <inheritdoc cref="KrObjectModel.WorkflowProcess" path="/summary"/>
        WorkflowProcess WorkflowProcess { get; }

        /// <summary>
        /// Контекст расширения карточки, содержащейся в контексте выполнения.
        /// </summary>
        ICardExtensionContext? CardContext { get; }

        /// <summary>
        /// Информация о вторичном процессе, для которого выполняется пересчёт или <see langword="null"/>, если выполняется пересчёт для основного процесса.
        /// </summary>
        IKrSecondaryProcess? SecondaryProcess { get; }

        /// <summary>
        /// Идентификатор группы единиц выполнения <see cref="ExecutionUnitIDs"/>.
        /// </summary>
        /// <remarks>
        /// Используется для передачи идентификатора группы этапов при расчете шаблонов по одной группе.
        /// </remarks>
        Guid? GroupID { get; }

        /// <inheritdoc cref="IValidationResultBuilder" path="/summary"/>
        IValidationResultBuilder ValidationResult { get; }

        #endregion

        #region Methods

        /// <summary>
        /// Создаёт новый контекст выполнения на основе существующего с учётом новых единиц выполнения.
        /// </summary>
        /// <param name="executionUnitIDs">Список идентификаторов новых единиц выполнения или <see langword="null"/>, если нужно выполнить все доступные.</param>
        /// <returns>Контекст выполнения, созданный на основе существующего с учётом новых единиц выполнения.</returns>
        IKrExecutionContext Copy(
            ISet<Guid>? executionUnitIDs = null);

        /// <summary>
        /// Создаёт новый контекст выполнения на основе существующего с учетом новых единиц выполнения и идентификатора группы.
        /// </summary>
        /// <param name="groupID">Идентификатор группы единиц выполнения.</param>
        /// <param name="executionUnitIDs">Список идентификаторов новых единиц выполнения или <see langword="null"/>, если нужно выполнить все доступные.</param>
        /// <returns>Контекст выполнения, созданный на основе существующего с учётом новых единиц выполнения и идентификатора группы.</returns>
        IKrExecutionContext Copy(
            Guid? groupID,
            ISet<Guid>? executionUnitIDs = null);

        #endregion
    }
}
