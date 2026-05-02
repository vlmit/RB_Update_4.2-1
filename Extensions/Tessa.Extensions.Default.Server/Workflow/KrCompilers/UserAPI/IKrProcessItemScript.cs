#nullable enable

using System.Threading.Tasks;

namespace Tessa.Extensions.Default.Server.Workflow.KrCompilers.UserAPI
{
    /// <summary>
    /// Описывает объект, поддерживающий выполнение сценариев процесса.
    /// </summary>
    /// <remarks>Объектом может быть: группа этапов, шаблон этапов и этап маршрута.</remarks>
    public interface IKrProcessItemScript
    {
        /// <summary>
        /// Выполняет сценарий инициализации <see cref="BeforeAsync"/>.
        /// </summary>
        /// <returns>Асинхронная задача.</returns>
        Task RunBeforeAsync();

        /// <summary>
        /// Сценарий инициализации.
        /// </summary>
        /// <returns>Асинхронная задача.</returns>
        Task BeforeAsync();

        /// <summary>
        /// Выполняет C#-условие <see cref="ConditionAsync"/>.
        /// </summary>
        /// <returns>Значение <see langword="true"/>, если текущий объект подтверждён для выполнения, иначе - <see langword="false"/>.</returns>
        ValueTask<bool> RunConditionAsync();

        /// <summary>
        /// Сценарий C#-условия.
        /// </summary>
        /// <returns>Значение <see langword="true"/>, если текущий объект подтверждён для выполнения, иначе - <see langword="false"/>.</returns>
        ValueTask<bool> ConditionAsync();

        /// <summary>
        /// Выполняет сценарий постобработки <see cref="AfterAsync"/>.
        /// </summary>
        /// <returns>Асинхронная задача.</returns>
        Task RunAfterAsync();

        /// <summary>
        /// Сценарий постобработки.
        /// </summary>
        /// <returns>Асинхронная задача.</returns>
        Task AfterAsync();
    }
}
