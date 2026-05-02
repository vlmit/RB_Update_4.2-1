#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tessa.Cards;

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager
{
    /// <summary>
    /// Базовая абстрактная реализация <see cref="IKrSingleTaskManager{T}"/>.
    /// </summary>
    /// <typeparam name="T"><inheritdoc cref="IKrSingleTaskManager{T}" path="/typeparam[@name='T']"/></typeparam>
    public abstract class KrSingleTaskManagerBase<T> :
        KrTaskManagerBase<T>,
        IKrSingleTaskManager<T>
        where T : IKrTaskManagerDataProvider
    {
        #region Properties

        /// <summary>
        /// Действия, выполняемые для обработки заданного варианта завершения.
        /// </summary>
        protected Dictionary<Guid, Func<IKrTaskManagerContext, T, CardTask, ValueTask<Guid?>>> CompleteTaskActions { get; } = [];

        #endregion

        #region IKrSingleTaskManager<T> Members

        /// <inheritdoc/>
        public abstract Guid TaskTypeID { get; }

        /// <inheritdoc/>
        public override async ValueTask<Guid?> CompleteTaskAsync(
            IKrTaskManagerContext context,
            T dataProvider,
            CardTask task)
        {
            ThrowIfNull(context);
            ThrowIfNull(dataProvider);
            ThrowIfNull(task);

            if (task.TypeID == this.TaskTypeID)
            {
                var result = await base.CompleteTaskAsync(context, dataProvider, task);
                if (task.OptionID.HasValue
                    && this.CompleteTaskActions.TryGetValue(task.OptionID.Value, out var completeTaskAction))
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

            return task.TypeID == this.TaskTypeID
                ? await base.DeleteTaskAsync(context, dataProvider, task)
                : null;
        }

        #endregion
    }
}
