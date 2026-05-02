#nullable enable

using System;

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager
{
    /// <summary>
    /// Фабрика объектов <see cref="IKrTaskManagerContext{T}"/>.
    /// </summary>
    public interface IKrTaskManagerContextFactory
    {
        /// <summary>
        /// Создаёт новый контекст.
        /// </summary>
        /// <typeparam name="TContext">Тип контекста.</typeparam>
        /// <typeparam name="TExternalContext">Тип внешнего контекста, которым инициализируется создаваемый контекст.</typeparam>
        /// <param name="externalContext">Внешний контекст.</param>
        /// <param name="name">Название объекта контекста, по которому он зарегистрирован в DI.</param>
        /// <param name="configureAction">Метод, настраивающий создаваемый объект.</param>
        /// <returns>Созданный контекст.</returns>
        /// <remarks>Не сохраняйте возвращаемое значение в singleton-объекте.</remarks>
        TContext Create<TContext, TExternalContext>(
            TExternalContext externalContext,
            string? name = null,
            Action<TContext>? configureAction = null)
            where TContext : IKrTaskManagerContext, IExternalContextProvider<TExternalContext>;
    }
}
