#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tessa.Cards;

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager
{
    /// <summary>
    /// Базовая абстрактная реализация <see cref="IKrMultiTaskManager{T}"/>.
    /// </summary>
    /// <typeparam name="T"><inheritdoc cref="IKrMultiTaskManager{T}" path="/typeparam[@name='T']"/></typeparam>
    public abstract class KrMultiTaskManagerBase<T> :
        KrTaskManagerBase<T>,
        IKrMultiTaskManager<T>
        where T : IKrTaskManagerDataProvider
    {
        #region Properties

        /// <summary>
        /// Действия, выполняемые для обработки завершения задания с заданным типом.
        /// </summary>
        protected Dictionary<Guid, Func<IKrTaskManagerContext, T, CardTask, ValueTask<Guid?>>> CompleteTaskActions { get; } = [];

        /// <summary>
        /// Действия, выполняемые для обработки удаления задания с заданным типом.
        /// </summary>
        protected Dictionary<Guid, Func<IKrTaskManagerContext, T, CardTask, ValueTask<Guid?>>> DeleteTaskActions { get; } = [];

        #endregion

        #region IKrMultiTaskManager<T> Members

        /// <inheritdoc/>
        public abstract IReadOnlySet<Guid> TaskTypeIDSet { get; }

        /// <inheritdoc/>
        public override async ValueTask<Guid?> CompleteTaskAsync(
            IKrTaskManagerContext context,
            T dataProvider,
            CardTask task)
        {
            ThrowIfNull(context);
            ThrowIfNull(dataProvider);
            ThrowIfNull(task);

            if (this.TaskTypeIDSet.Contains(task.TypeID))
            {
                var result = await base.CompleteTaskAsync(context, dataProvider, task);
                if (this.CompleteTaskActions.TryGetValue(task.TypeID, out var completeTaskAction))
                {
                    return await completeTaskAction(
                        context,
                        dataProvider,
                        task);
                }

                return result;
            }

            return null;
        }

        /// <inheritdoc/>
        public override async ValueTask<Guid?> DeleteTaskAsync(
            IKrTaskManagerContext context,
            T dataProvider,
            CardTask task)
        {
            ThrowIfNull(context);
            ThrowIfNull(dataProvider);
            ThrowIfNull(task);

            if (this.TaskTypeIDSet.Contains(task.TypeID))
            {
                var result = await base.DeleteTaskAsync(context, dataProvider, task);
                if (this.DeleteTaskActions.TryGetValue(task.TypeID, out var deleteTaskAction))
                {
                    return await deleteTaskAction(
                        context,
                        dataProvider,
                        task);
                }

                return result;
            }

            return null;
        }

        #endregion
    }
}
