#nullable enable

using System;
using System.Threading.Tasks;
using Tessa.Cards;

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager
{
    /// <summary>
    /// Объект, управляющий созданием и выполнением действий.
    /// </summary>
    /// <typeparam name="T"><inheritdoc cref="IKrTaskManagerDataProvider" path="/summary"/></typeparam>
    public interface IKrTaskManager<T>
        where T : IKrTaskManagerDataProvider
    {
        /// <summary>
        /// Подготавливает действие к выполнению.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IKrTaskManagerContext" path="/summary"/></param>
        /// <param name="dataProvider"><inheritdoc cref="IKrTaskManager{T}" path="/typeparam[@name='T']"/></param>
        /// <returns><inheritdoc cref="ValueTask" path="/summary"/></returns>
        ValueTask PrepareAsync(
            IKrTaskManagerContext context,
            T dataProvider);

        /// <summary>
        /// Обрабатывает запуск действия.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IKrTaskManagerContext" path="/summary"/></param>
        /// <param name="dataProvider"><inheritdoc cref="IKrTaskManager{T}" path="/typeparam[@name='T']"/></param>
        /// <returns>Идентификатор варианта завершения действия или значение <see langword="null"/>, если запуск не обработан или произошла ошибка.</returns>
        ValueTask<Guid?> StartAsync(
            IKrTaskManagerContext context,
            T dataProvider);

        /// <summary>
        /// Обрабатывает завершение задания.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IKrTaskManagerContext" path="/summary"/></param>
        /// <param name="dataProvider"><inheritdoc cref="IKrTaskManager{T}" path="/typeparam[@name='T']"/></param>
        /// <param name="task">Завершаемое задание.</param>
        /// <returns>Идентификатор варианта завершения действия или значение <see langword="null"/>, если вариант завершения не обработан или произошла ошибка.</returns>
        ValueTask<Guid?> CompleteTaskAsync(
            IKrTaskManagerContext context,
            T dataProvider,
            CardTask task);

        /// <summary>
        /// Обрабатывает удаление задания.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IKrTaskManagerContext" path="/summary"/></param>
        /// <param name="dataProvider"><inheritdoc cref="IKrTaskManager{T}" path="/typeparam[@name='T']"/></param>
        /// <param name="task">Удаляемое задание.</param>
        /// <returns>Идентификатор варианта завершения действия или значение <see langword="null"/>, если задание не удаляется или произошла ошибка.</returns>
        ValueTask<Guid?> DeleteTaskAsync(
            IKrTaskManagerContext context,
            T dataProvider,
            CardTask task);
    }
}
