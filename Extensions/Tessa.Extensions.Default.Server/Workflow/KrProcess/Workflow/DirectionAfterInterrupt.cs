#nullable enable

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow
{
    /// <summary>
    /// Направление перехода после отмены выполнения этапа.
    /// </summary>
    public enum DirectionAfterInterrupt
    {
        /// <summary>
        /// Вперёд.
        /// </summary>
        Forward = 0,

        /// <summary>
        /// Назад.
        /// </summary>
        Backward = 1,
    }
}
