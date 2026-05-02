#nullable enable

using System;

namespace Tessa.Extensions.Default.Shared.Workflow.KrProcess.Formatters
{
    /// <summary>
    /// Контейнер, содержащий объекты, выполняющие форматирование параметров этапов.
    /// </summary>
    public interface IStageTypeFormatterContainer
    {
        /// <summary>
        /// Регистрирует объект, выполняющий форматирование параметров этапа.
        /// </summary>
        /// <typeparam name="T">Тип регистрируемого объекта, выполняющего форматирование параметров этапа.</typeparam>
        /// <param name="descriptor"><inheritdoc cref="StageTypeDescriptor" path="/summary"/></param>
        /// <returns><inheritdoc cref="IStageTypeFormatterContainer" path="/summary"/></returns>
        IStageTypeFormatterContainer RegisterFormatter<T>(
            StageTypeDescriptor descriptor) where T : IStageTypeFormatter;

        /// <summary>
        /// Регистрирует объект, выполняющий форматирование параметров этапа.
        /// </summary>
        /// <param name="descriptor"><inheritdoc cref="StageTypeDescriptor" path="/summary"/></param>
        /// <param name="handlerType">Тип регистрируемого объекта, выполняющего форматирование параметров этапа.</param>
        /// <returns><inheritdoc cref="IStageTypeFormatterContainer" path="/summary"/></returns>
        IStageTypeFormatterContainer RegisterFormatter(
            StageTypeDescriptor descriptor,
            Type handlerType);

        /// <summary>
        /// Возвращает объект, выполняющий форматирование параметров этапа, зарегистрированный под указанным идентификатором типа этапа.
        /// </summary>
        /// <param name="descriptorID">Идентификатор типа этапа, для которого требуется получить объект, выполняющий форматирование параметров.</param>
        /// <returns>Объект, выполняющий форматирование параметров этапа, или значение <see langword="null"/>, если он не найден.</returns>
        IStageTypeFormatter? ResolveFormatter(Guid descriptorID);
    }
}
