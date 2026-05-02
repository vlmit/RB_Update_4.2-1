#nullable enable

using System;
using System.Collections.Generic;

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager
{
    /// <summary>
    /// Объект, управляющий созданием и выполнением действий, поддерживающих работу с несколькими типами заданий.
    /// </summary>
    /// <typeparam name="T"><inheritdoc cref="IKrTaskManager{T}" path="/typeparam[@name='T']"/></typeparam>
    public interface IKrMultiTaskManager<T> :
        IKrTaskManager<T>
        where T : IKrTaskManagerDataProvider
    {
        /// <summary>
        /// Типы обрабатываемых заданий.
        /// </summary>
        IReadOnlySet<Guid> TaskTypeIDSet { get; }
    }
}
