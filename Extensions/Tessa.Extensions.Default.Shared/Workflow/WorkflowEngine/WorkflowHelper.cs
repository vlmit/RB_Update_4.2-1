#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.ComponentModel;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform;
using Tessa.Platform.Data;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.Scheme;
using Tessa.Workflow.Bindings;
using Tessa.Workflow.Helpful;
using Tessa.Workflow.Storage;

namespace Tessa.Extensions.Default.Shared.Workflow.WorkflowEngine
{
    /// <summary>
    /// Предоставляет вспомогательные методы для работы с WorkflowEngine.
    /// </summary>
    public static class WorkflowHelper
    {
        #region Static methods

        #region ProcessCycle

        /// <summary>
        /// Возвращает номер текущего цикла процесса согласования.
        /// </summary>
        /// <param name="processHash">Параметры процесса.</param>
        /// <param name="defaultValue">Номер цикла по умолчанию. Значение по умолчанию: 1.</param>
        /// <returns>Номер текущего цикла процесса согласования. Если не найден в параметрах процесса, то считается равным <paramref name="defaultValue"/>.</returns>
        /// <seealso cref="WorkflowConstants.NamesKeys.ProcessCycle"/>
        public static int GetProcessCycle(IDictionary<string, object?> processHash, int defaultValue = 1) =>
            WorkflowEngineHelper.Get<int?>(processHash, WorkflowBindingTypes.Process.Name, WorkflowConstants.NamesKeys.ProcessCycle) ?? defaultValue;

        /// <summary>
        /// Устанавливает номер цикла процесса согласования в параметры процесса.
        /// </summary>
        /// <param name="processHash">Параметры процесса.</param>
        /// <param name="cycle">Номер цикла согласования.</param>
        /// <seealso cref="WorkflowConstants.NamesKeys.ProcessCycle"/>
        public static void SetProcessCycle(IDictionary<string, object?> processHash, int cycle) =>
            WorkflowEngineHelper.Set(processHash, Int32Boxes.Box(cycle), WorkflowBindingTypes.Process.Name, WorkflowConstants.NamesKeys.ProcessCycle);

        /// <summary>
        /// Увеличивает номер цикла процесса согласования, содержащийся в параметрах процесса, на указанное значение.
        /// </summary>
        /// <param name="processHash">Параметры процесса.</param>
        /// <param name="value">Значение на которое увеличивается номер цикла согласования.</param>
        /// <returns>Новое значение номера цикла согласования.</returns>
        /// <remarks>Задавая в параметре <paramref name="value"/> отрицательные значения, можно реализовать декремент.</remarks>
        /// <seealso cref="WorkflowConstants.NamesKeys.ProcessCycle"/>
        public static int ProcessCycleIncrement(IDictionary<string, object?> processHash, int value = 1)
        {
            var newValue = GetProcessCycle(processHash) + value;
            SetProcessCycle(processHash, newValue);
            return newValue;
        }

        #endregion

        #region CurrentPerformerIndex

        /// <summary>
        /// Возвращает порядковый номер текущего исполнителя из параметров действия.
        /// </summary>
        /// <param name="actionHash">Параметры действия.</param>
        /// <param name="defaultValue">Порядковый номер текущего исполнителя по умолчанию. Значение по умолчанию: 0.</param>
        /// <returns>Порядковый номер текущего исполнителя. Если не найден в параметрах действия, то считается равным <paramref name="defaultValue"/>.</returns>
        /// <seealso cref="WorkflowConstants.NamesKeys.CurrentPerformerIndex"/>
        public static int GetCurrentPerformerIndex(IDictionary<string, object?> actionHash, int defaultValue = 0) =>
            WorkflowEngineHelper.Get<int?>(actionHash, WorkflowConstants.NamesKeys.CurrentPerformerIndex) ?? defaultValue;

        /// <summary>
        /// Устанавливает порядковый номер текущего исполнителя в параметрах действия.
        /// </summary>
        /// <param name="actionHash">Параметры действия.</param>
        /// <param name="index">Порядковый номер текущего исполнителя.</param>
        /// <seealso cref="WorkflowConstants.NamesKeys.CurrentPerformerIndex"/>
        public static void SetCurrentPerformerIndex(
            IDictionary<string, object?> actionHash,
            int index) =>
            WorkflowEngineHelper.Set(actionHash, Int32Boxes.Box(index), WorkflowConstants.NamesKeys.CurrentPerformerIndex);

        /// <summary>
        /// Увеличивает на указанное число порядковый номер текущего исполнителя.
        /// </summary>
        /// <param name="actionHash">Параметры действия.</param>
        /// <param name="value">Значение на которое выполняется увеличение.</param>
        /// <returns>Новое значение.</returns>
        /// <seealso cref="WorkflowConstants.NamesKeys.CurrentPerformerIndex"/>.
        public static int CurrentPerformerIndexIncrement(
            IDictionary<string, object?> actionHash,
            int value = 1)
        {
            var newValue = GetCurrentPerformerIndex(actionHash) + value;
            SetCurrentPerformerIndex(actionHash, newValue);
            return newValue;
        }

        #endregion

        /// <summary>
        /// Возвращает значение, показывающее, что указанное задание <paramref name="task"/> из карточки <paramref name="card"/> было отправлено из Workflow Engine.
        /// </summary>
        /// <param name="task">Проверяемое задание.</param>
        /// <param name="card">Карточка содержащая проверяемое задание или значение <see langword="null"/>, если она не доступна.</param>
        /// <param name="dbScope">Объект для взаимодействия с базой данных.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Значение <see langword="true"/>, если задание было отправлено из Workflow Engine, иначе - <see langword="false"/>.</returns>
        /// <remarks>
        /// Проверка основана на том, что при отправке задания задаётся значение <see cref="KrConstants.TaskHistorySettingsKeys.ProcessKind"/> в <see cref="CardTask.HistorySettings"/>,
        /// равное <see cref="WorkflowEngineHelper.WorkflowEngineProcessName"/>,
        /// если задание и соответствующая запись истории заданий не содержат информации о типе процесса и <paramref name="card"/> задан, то проверяется наличие подписки на задание в таблице <b>WorkflowEngineTaskSubscriptions</b>.
        /// </remarks>
        public static async Task<bool> IsWorkflowEngineTaskAsync(
            CardTask task,
            Card? card,
            IDbScope dbScope,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(task);
            ThrowIfNull(dbScope);

            var processKind = task.TryGetInfo()?.TryGet<string>(CardHelper.TaskProcessKindKey)
                ?? task.HistorySettings?.TryGet<string>(KrConstants.TaskHistorySettingsKeys.ProcessKind);
            if (processKind is null)
            {
                var isCheckInDb = true;
                if (card is not null)
                {
                    var historyItem = card.TryGetTaskHistory()?.FirstOrDefault(i => i.RowID == task.RowID);
                    if (historyItem is not null)
                    {
                        processKind = historyItem.Settings.TryGet<string>(KrConstants.TaskHistorySettingsKeys.ProcessKind);
                        isCheckInDb = false;
                    }
                }

                if (isCheckInDb)
                {
                    await using (dbScope.Create())
                    {
                        var db = dbScope.Db;
                        return await db
                            .SetCommand(
                                dbScope.BuilderFactory
                                    .Select()
                                        .V(true)
                                    .From("WorkflowEngineTaskSubscriptions").NoLock()
                                    .Where()
                                        .C("TaskID").Equals().P("TaskID")
                                    .Build(),
                                db.Parameter("TaskID", task.RowID))
                            .LogCommand()
                            .ExecuteAsync<bool>(cancellationToken);
                    }
                }
            }

            return processKind == WorkflowEngineHelper.WorkflowEngineProcessName;
        }

        /// <summary>
        /// Инициализирует указанный список строк, содержащий параметры обработки вариантов завершения действий, заданными вариантами завершения.
        /// </summary>
        /// <param name="completionOptions">Коллекция содержащая информацию о вариантах завершения.</param>
        /// <param name="rows">Инициализируемый список строк.</param>
        /// <param name="templateRow">Строка, используемая в качестве шаблона. Значение не изменяется.</param>
        /// <param name="optionIDs">Перечисление идентификаторов вариантов завершения.</param>
        /// <remarks>Секция не изменяется, если содержит значения.</remarks>
        public static void InitializeActionCompletionOptions(
            IReadOnlyDictionary<Guid, ActionCompletionOption> completionOptions,
            ListStorage<CardRow> rows,
            CardRow templateRow,
            IList<Guid> optionIDs)
        {
            ThrowIfNull(completionOptions);
            ThrowIfNull(rows);
            ThrowIfNull(templateRow);
            ThrowIfNull(optionIDs);

            if (rows.Count > 0)
            {
                return;
            }

            var index = 0;
            foreach (var optionID in optionIDs)
            {
                var row = rows.Add();
                var newRow = templateRow.Clone();
                newRow.RowID = Guid.NewGuid();

                var fields = newRow.Fields;
                var optionCaption = completionOptions[optionID].Caption;
                fields[WorkflowConstants.ActionOptionsActionBase.ActionOption + Names.Table_ID] = optionID;
                fields[WorkflowConstants.ActionOptionsActionBase.ActionOption + WorkflowConstants.Table_Field_Caption] = optionCaption;
                fields[WorkflowConstants.ActionOptionsActionBase.Order] = Int32Boxes.Box(index++);

                row.Set(newRow);
                row.State = CardRowState.Inserted;
            }
        }

        /// <summary>
        /// Инициализирует указанный список строк, содержащий параметры обработки вариантов завершения заданий указанных типов.
        /// </summary>
        /// <param name="cardMetadata">Метаинформация, необходимая для использования типов карточек совместно с пакетом карточек.</param>
        /// <param name="rows">Инициализируемый список строк.</param>
        /// <param name="templateRow">Строка, используемая в качестве шаблона. Значение не изменяется.</param>
        /// <param name="taskTypeIDs">Перечисление идентификаторов типов заданий.</param>
        /// <param name="validationResult">Результат выполнения.</param>
        /// <param name="validationResultObject">Ссылка на объект, определяющая фактический тип объекта, имя которого используется в сообщении валидации.</param>
        /// <param name="isSetTaskTypeInfo">Значение <see langword="true"/>, если необходимо задать информацию о типе задания, иначе - <see langword="false"/>.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Асинхронная задача.</returns>
        /// <remarks>Секция не изменяется, если содержит значения.</remarks>
        public static async ValueTask InitializeTaskCompletionOptionsAsync(
            ICardMetadata cardMetadata,
            ListStorage<CardRow> rows,
            CardRow templateRow,
            IEnumerable<Guid> taskTypeIDs,
            IValidationResultBuilder validationResult,
            object? validationResultObject = null,
            bool isSetTaskTypeInfo = false,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(cardMetadata);
            ThrowIfNull(rows);
            ThrowIfNull(templateRow);
            ThrowIfNull(taskTypeIDs);
            ThrowIfNull(validationResult);

            if (rows.Count > 0
                || !taskTypeIDs.Any())
            {
                return;
            }

            var metaCompletionOptions = (await cardMetadata.GetEnumerationsAsync(cancellationToken)).CompletionOptions;

            var index = 0;
            foreach (var taskTypeID in taskTypeIDs)
            {
                var taskType = await CardComponentHelper.TryGetCardTypeAsync(taskTypeID, cardMetadata, cancellationToken);
                if (taskType is null)
                {
                    ValidationSequence
                        .Begin(validationResult)
                        .SetObjectName(validationResultObject)
                        .Error(CardValidationKeys.UnknownCardType, taskTypeID)
                        .End();

                    await CardComponentHelper.AddDamagedCardTypeValidationResultAsync(
                        taskTypeID,
                        null,
                        cardMetadata,
                        validationResult,
                        cancellationToken);

                    return;
                }

                foreach (var completionOption in taskType.CompletionOptions)
                {
                    var optionID = completionOption.TypeID;
                    if (!metaCompletionOptions.TryGetValue(optionID, out var metaCompletionOption))
                    {
                        ValidationSequence
                            .Begin(validationResult)
                            .SetObjectName(validationResultObject)
                            .Error(CardValidationKeys.UnknownCompletionOption, optionID)
                            .End();
                        return;
                    }

                    var row = rows.Add();
                    var newRow = templateRow.Clone();
                    newRow.RowID = Guid.NewGuid();

                    var fields = newRow.Fields;
                    fields[WorkflowConstants.ActionOptionsBase.Option + Names.Table_ID] = optionID;
                    fields[WorkflowConstants.ActionOptionsBase.Option + WorkflowConstants.Table_Field_Caption] = metaCompletionOption.Caption;

                    if (isSetTaskTypeInfo)
                    {
                        fields[WorkflowConstants.ActionSeveralTaskTypesOptionsBase.TaskType + Names.Table_ID] = taskTypeID;
                        fields[WorkflowConstants.ActionSeveralTaskTypesOptionsBase.TaskType + WorkflowConstants.Table_Field_Name] = taskType.Name;
                        fields[WorkflowConstants.ActionSeveralTaskTypesOptionsBase.TaskType + WorkflowConstants.Table_Field_Caption] = taskType.Caption;
                    }

                    fields[WorkflowConstants.ActionOptionsBase.Order] = Int32Boxes.Box(index++);

                    row.Set(newRow);
                    row.State = CardRowState.Inserted;
                }
            }
        }

        /// <summary>
        /// Формирует сообщение валидации о том, что нет ни одного исполнителя.
        /// </summary>
        /// <param name="action">Действие.</param>
        /// <param name="node">Узел.</param>
        /// <param name="template">Шаблон.</param>
        /// <returns>Сформированное сообщение.</returns>
        public static string GetValidatePerformerNotSpecifiedMessage(
            WorkflowActionStorage action,
            WorkflowNodeStorage node,
            string template = "$KrActions_PerformerNotSpecified")
        {
            ThrowIfNull(action);
            ThrowIfNull(node);

            return
                LocalizeFormat(
                    template,
                    action.Name,
                    node.GetObjectName());
        }

        #endregion
    }
}
