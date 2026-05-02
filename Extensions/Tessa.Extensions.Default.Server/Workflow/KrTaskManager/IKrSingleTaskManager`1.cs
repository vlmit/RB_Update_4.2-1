#nullable enable

using System;

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager
{
    /// <summary>
    /// Объект, управляющий созданием и выполнением действий, поддерживающих работу с одним типом задания.
    /// </summary>
    /// <typeparam name="T"><inheritdoc cref="IKrTaskManager{T}" path="/typeparam[@name='T']"/></typeparam>
    public interface IKrSingleTaskManager<T> :
        IKrTaskManager<T>
        where T : IKrTaskManagerDataProvider
    {
        /// <summary>
        /// Тип обрабатываемого задания.
        /// </summary>
        Guid TaskTypeID { get; }
    }
}
