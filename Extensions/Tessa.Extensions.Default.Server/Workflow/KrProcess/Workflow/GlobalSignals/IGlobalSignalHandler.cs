#nullable enable

using System.Threading.Tasks;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow.GlobalSignals
{
    /// <summary>
    /// Объект, обрабатывающий глобальные сигналы подсистемы маршрутов.
    /// </summary>
    public interface IGlobalSignalHandler
    {
        /// <summary>
        /// Обрабатывает глобальный сигнал.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IGlobalSignalHandlerContext" path="/summary"/></param>
        /// <returns><inheritdoc cref="IGlobalSignalHandlerResult" path="/summary"/></returns>
        Task<IGlobalSignalHandlerResult> Handle(
            IGlobalSignalHandlerContext context);
    }
}
