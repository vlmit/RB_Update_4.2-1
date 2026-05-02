#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LinqToDB;
using Tessa.Cards;
using Tessa.Extensions.Default.Server.Workflow.KrTaskManager.Managers;
using Tessa.Platform.Data;
using Tessa.Scheme;

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager
{
    /// <summary>
    /// Базовая абстрактная реализация <see cref="IKrTaskManager{T}"/>.
    /// </summary>
    /// <typeparam name="T"><inheritdoc cref="IKrTaskManager{T}" path="/typeparam[@name='T']"/></typeparam>
    public abstract class KrTaskManagerBase<T> :
        IKrTaskManager<T>
        where T : IKrTaskManagerDataProvider
    {
        #region IKrTaskManager<T> Members

        /// <inheritdoc/>
        public virtual ValueTask PrepareAsync(
            IKrTaskManagerContext context,
            T dataProvider) =>
            ValueTask.CompletedTask;

        /// <inheritdoc/>
        public virtual ValueTask<Guid?> StartAsync(
            IKrTaskManagerContext context,
            T dataProvider) =>
            ValueTask.FromResult<Guid?>(null);

        /// <inheritdoc/>
        public virtual async ValueTask<Guid?> CompleteTaskAsync(
            IKrTaskManagerContext context,
            T dataProvider,
            CardTask task)
        {
            await this.CompleteTaskCoreAsync(
                context,
                dataProvider,
                task);

            return task.OptionID;
        }

        /// <inheritdoc/>
        public virtual async ValueTask<Guid?> DeleteTaskAsync(
            IKrTaskManagerContext context,
            T dataProvider,
            CardTask task)
        {
            await this.DeleteTaskCoreAsync(
                context,
                dataProvider,
                task);

            return KrTaskManagerCompletionOptions.Delete;
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Завершает дочернее задание <paramref name="task"/>.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IKrTaskManagerContext" path="/summary"/></param>
        /// <param name="dataProvider"><inheritdoc cref="KrTaskManagerBase{T}" path="/typeparam[@name='T']"/></param>
        /// <param name="task">Завершаемое дочернее задание.</param>
        /// <param name="parentTask">Родительское задание.</param>
        /// <returns><see langword="true"/>, если завершение задания было обработано, иначе - <see langword="false"/>.</returns>
        protected async ValueTask<bool> CompleteSubTaskAsync(
            IKrTaskManagerContext context,
            T dataProvider,
            CardTask task,
            CardTask parentTask)
        {
            context.AddTaskToNext(task);

            var optionInfo = (await context.CardMetadata.GetCardTypesAsync(context.CancellationToken))[task.TypeID]
                .CompletionOptions
                .FirstOrDefault(x => x.TypeID == task.OptionID);

            task.Action = CardTaskAction.Complete;
            task.State = optionInfo is not null
                && optionInfo.Flags.Has(CardTypeCompletionOptionFlags.DoNotDeleteTask)
                ? CardRowState.Modified
                : CardRowState.Deleted;

            await this.CompleteSubTaskCoreAsync(
                context,
                dataProvider,
                task,
                parentTask);

            if (!context.ValidationResult.IsSuccessful())
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Удаляет дочернее задание <paramref name="task"/>.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IKrTaskManagerContext" path="/summary"/></param>
        /// <param name="dataProvider"><inheritdoc cref="KrTaskManagerBase{T}" path="/typeparam[@name='T']"/></param>
        /// <param name="task">Удаляемое дочернее задание.</param>
        /// <param name="parentTask">Родительское задание.</param>
        /// <returns><see langword="true"/>, если удаление задания было обработано, иначе - <see langword="false"/>.</returns>
        protected async ValueTask<bool> DeleteSubTaskAsync(
            IKrTaskManagerContext context,
            T dataProvider,
            CardTask task,
            CardTask parentTask)
        {
            await this.DeleteTaskCoreAsync(
                context,
                dataProvider,
                task);

            if (!context.ValidationResult.IsSuccessful())
            {
                return false;
            }

            await this.DeleteSubTaskCoreAsync(
                context,
                dataProvider,
                task,
                parentTask);

            return !context.ValidationResult.IsSuccessful();
        }

        /// <summary>
        /// Обрабатывает завершение задания текущего обработчика.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IKrTaskManagerContext" path="/summary"/></param>
        /// <param name="dataProvider"><inheritdoc cref="KrTaskManagerBase{T}" path="/typeparam[@name='T']"/></param>
        /// <param name="task"><inheritdoc cref="CardTask" path="/summary"/></param>
        /// <returns>Асинхронная задача.</returns>
        protected virtual ValueTask CompleteTaskCoreAsync(
            IKrTaskManagerContext context,
            T dataProvider,
            CardTask task)
        {
            if (dataProvider.CompleteTaskActionAsync is not null)
            {
                return dataProvider.CompleteTaskActionAsync(
                    task,
                    context.CancellationToken);
            }

            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// Обрабатывает завершение дочернего задания.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IKrTaskManagerContext" path="/summary"/></param>
        /// <param name="dataProvider"><inheritdoc cref="KrTaskManagerBase{T}" path="/typeparam[@name='T']"/></param>
        /// <param name="task">Завершаемое дочернее задание.</param>
        /// <param name="parentTask">Родительское задание.</param>
        /// <returns>Идентификатор варианта завершения действия для дочернего задания.</returns>
        protected virtual ValueTask<Guid?> CompleteSubTaskCoreAsync(
            IKrTaskManagerContext context,
            T dataProvider,
            CardTask task,
            CardTask parentTask) =>
            ValueTask.FromResult((Guid?) null);

        /// <summary>
        /// Обрабатывает удаление дочернего задания.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IKrTaskManagerContext" path="/summary"/></param>
        /// <param name="dataProvider"><inheritdoc cref="KrTaskManagerBase{T}" path="/typeparam[@name='T']"/></param>
        /// <param name="task">Удаляемое дочернее задание.</param>
        /// <param name="parentTask">Родительское задание.</param>
        /// <returns>Идентификатор варианта завершения действия для дочернего задания.</returns>
        protected virtual ValueTask<Guid?> DeleteSubTaskCoreAsync(
            IKrTaskManagerContext context,
            T dataProvider,
            CardTask task,
            CardTask parentTask) =>
            ValueTask.FromResult((Guid?) null);

        /// <summary>
        /// Обрабатывает удаление задания текущего обработчика.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IKrTaskManagerContext" path="/summary"/></param>
        /// <param name="dataProvider"><inheritdoc cref="KrTaskManagerBase{T}" path="/typeparam[@name='T']"/></param>
        /// <param name="task"><inheritdoc cref="CardTask" path="/summary"/></param>
        /// <returns>Асинхронная задача.</returns>
        protected virtual async ValueTask DeleteTaskCoreAsync(
            IKrTaskManagerContext context,
            T dataProvider,
            CardTask task)
        {
            context.AddTaskToNext(task);

            task.State = CardRowState.Deleted;

            await this.DeleteTaskHistoryAsync(
                context,
                task.RowID);

            if (dataProvider.DeleteTaskActionAsync is not null)
            {
                await dataProvider.DeleteTaskActionAsync(
                    task,
                    context.CancellationToken);
            }
        }

        /// <summary>
        /// Удаляет записи из истории заданий, связанные с заданием с указанным идентификатором.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IKrTaskManagerContext" path="/summary"/></param>
        /// <param name="taskRowID">Идентификатор задания.</param>
        /// <returns>Асинхронная задача.</returns>
        protected virtual async Task DeleteTaskHistoryAsync(
            IKrTaskManagerContext context,
            Guid taskRowID)
        {
            await using var _ = context.DbScope.Create();
            var db = context.DbScope.Db;

            await db.SetCommand(
                    context.DbScope.BuilderFactory
                        .With("ChildTaskHistory", static e => e
                                .Select().P("ParentRowID")
                                .UnionAll()
                                .Select().C("t", "RowID")
                                .From("TaskHistory", "t").NoLock()
                                .InnerJoin("ChildTaskHistory", "p")
                                    .On().C("p", "RowID").Equals().C("t", "ParentRowID"),
                            columnNames: ["RowID"],
                            recursive: true)
                        .DeleteFrom(Names.TaskHistory)
                        .If(Dbms.SqlServer, static i => i
                            .From(Names.TaskHistory, "th")
                            .InnerJoin("ChildTaskHistory", "h")
                            .On().C("th", "RowID").Equals().C("h", "RowID"))
                        .ElseIf(Dbms.PostgreSql, static i => i
                            .AppendLineIfRequired()
                            .Q("USING").Table("ChildTaskHistory", "th")
                            .Where().C(Names.TaskHistory, "RowID").Equals().C("th", "RowID"))
                        .ElseThrow()
                        .Build(),
                    db.Parameter("ParentRowID", taskRowID, DataType.Guid))
                .LogCommand()
                .ExecuteNonQueryAsync(context.CancellationToken);
        }

        /// <summary>
        /// Завершает дочерние задания указанных типов.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IKrTaskManagerContext" path="/summary"/></param>
        /// <param name="dataProvider"><inheritdoc cref="KrTaskManagerBase{T}" path="/typeparam[@name='T']"/></param>
        /// <param name="parentTask">Родительское задание.</param>
        /// <param name="taskTypeIDs">Коллекция типов обрабатываемых заданий.</param>
        /// <param name="modifyActionAsync">Действие выполняемое перед завершением задания.</param>
        /// <returns>Асинхронная задача.</returns>
        protected async ValueTask CompleteSubTasksAsync(
            IKrTaskManagerContext context,
            T dataProvider,
            CardTask parentTask,
            ICollection<Guid> taskTypeIDs,
            Func<CardTask, ValueTask> modifyActionAsync)
        {
            if (taskTypeIDs.Count == 0)
            {
                return;
            }

            var mainCard = await context.GetCardAsync(
                context.MainCardID,
                context.ValidationResult,
                true,
                context.CancellationToken);

            if (mainCard is null
                || mainCard.TryGetTasks() is not { Count: > 0 } tasks)
            {
                return;
            }

            foreach (var task in tasks
                .Where(i =>
                    i.ParentRowID == parentTask.RowID
                    && taskTypeIDs.Contains(i.TypeID)))
            {
                await modifyActionAsync(task);

                await this.CompleteSubTaskAsync(
                    context,
                    dataProvider,
                    task,
                    parentTask);
            }
        }

        /// <summary>
        /// Удаляет дочерние задания указанных типов.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IKrTaskManagerContext" path="/summary"/></param>
        /// <param name="dataProvider"><inheritdoc cref="KrTaskManagerBase{T}" path="/typeparam[@name='T']"/></param>
        /// <param name="parentTask">Родительское задание.</param>
        /// <param name="taskTypeIDs">Коллекция типов обрабатываемых заданий или значение <see langword="null"/>, если должны быть удалены все задания.</param>
        /// <param name="modifyActionAsync">Действие выполняемое перед удалением задания.</param>
        /// <returns>Асинхронная задача.</returns>
        protected async ValueTask DeleteSubTasksAsync(
            IKrTaskManagerContext context,
            T dataProvider,
            CardTask parentTask,
            ICollection<Guid>? taskTypeIDs = null,
            Func<CardTask, ValueTask>? modifyActionAsync = null)
        {
            if (taskTypeIDs?.Count == 0)
            {
                return;
            }

            var mainCard = await context.GetCardAsync(
                context.MainCardID,
                context.ValidationResult,
                true,
                context.CancellationToken);

            if (mainCard is null
                || mainCard.TryGetTasks() is not { Count: > 0 } tasks)
            {
                return;
            }

            foreach (var task in tasks
                .Where(i =>
                    i.ParentRowID == parentTask.RowID
                    && (taskTypeIDs?.Contains(i.TypeID) ?? true)))
            {
                if (modifyActionAsync is not null)
                {
                    await modifyActionAsync(task);
                }

                await this.DeleteSubTaskAsync(
                    context,
                    dataProvider,
                    task,
                    parentTask);
            }
        }

        #endregion
    }
}
