#nullable enable

using System;

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager
{
    /// <summary>
    /// Объект, обеспечивающий возможность создания дочерних провайдеров данных.
    /// </summary>
    public interface IKrTaskManagerNestedDataProviderProvider
    {
        /// <summary>
        /// Создаёт новый <typeparamref name="TDataProvider"/>, основанный на текущем провайдере данных.
        /// </summary>
        /// <typeparam name="TDataProvider">Тип объекта, обеспечивающего передачу данных между внешней подсистемой и <see cref="IKrTaskManager{T}"/>.</typeparam>
        /// <param name="name">Название объекта контекста, по которому он зарегистрирован в DI.</param>
        /// <param name="configureAction">Метод, настраивающий создаваемый объект.</param>
        /// <returns>Созданный контекст.</returns>
        /// <remarks>Не сохраняйте возвращаемое значение в singleton-объекте.</remarks>
        TDataProvider CreateNested<TDataProvider>(
            string? name = null,
            Action<TDataProvider>? configureAction = null)
            where TDataProvider : IKrTaskManagerDataProvider;
    }
}
