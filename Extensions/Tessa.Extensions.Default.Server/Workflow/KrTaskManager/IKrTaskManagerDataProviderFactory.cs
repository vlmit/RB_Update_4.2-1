#nullable enable

using System;

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager
{
    /// <summary>
    /// Фабрика объектов <see cref="IKrTaskManagerDataProvider"/>.
    /// </summary>
    public interface IKrTaskManagerDataProviderFactory
    {
        /// <summary>
        /// Создаёт новый <typeparamref name="TDataProvider"/>.
        /// </summary>
        /// <typeparam name="TDataProvider">Тип объекта, обеспечивающего передачу данных между внешней подсистемой и <see cref="IKrTaskManager{T}"/>.</typeparam>
        /// <param name="name">Название объекта контекста, по которому он зарегистрирован в DI.</param>
        /// <param name="configureAction">Метод, настраивающий создаваемый объект.</param>
        /// <returns>Созданный контекст.</returns>
        /// <remarks>Не сохраняйте возвращаемое значение в singleton-объекте.</remarks>
        TDataProvider Create<TDataProvider>(
            string? name = null,
            Action<TDataProvider>? configureAction = null)
            where TDataProvider : IKrTaskManagerDataProvider;

        /// <summary>
        /// Создаёт новый <typeparamref name="TDataProvider"/>.
        /// </summary>
        /// <typeparam name="TDataProvider">Тип объекта, обеспечивающего передачу данных между внешней подсистемой и <see cref="IKrTaskManager{T}"/>.</typeparam>
        /// <typeparam name="TExternalContext">Тип внешнего контекста, которым инициализируется <paramref name="externalContext"/>.</typeparam>
        /// <param name="externalContext">Внешний контекст.</param>
        /// <param name="name">Название объекта контекста, по которому он зарегистрирован в DI.</param>
        /// <param name="configureAction">Метод, настраивающий создаваемый объект.</param>
        /// <returns>Созданный контекст.</returns>
        /// <remarks>Не сохраняйте возвращаемое значение в singleton-объекте.</remarks>
        TDataProvider Create<TDataProvider, TExternalContext>(
            TExternalContext externalContext,
            string? name = null,
            Action<TDataProvider>? configureAction = null)
            where TDataProvider : IKrTaskManagerDataProvider, IExternalContextProvider<TExternalContext>;
    }
}
