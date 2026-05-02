#nullable enable

using Tessa.Platform;

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager
{
    /// <summary>
    /// Объект, обеспечивающий передачу данных между внешней подсистемой и <see cref="IKrTaskManager{T}"/>.
    /// </summary>
    public interface IKrTaskManagerDataProvider :
        ISealable
    {
        #region Properties

        /// <summary>
        /// Действие, выполняемое над завершаемым заданием.
        /// </summary>
        TaskActionAsync? CompleteTaskActionAsync { get; set; }

        /// <summary>
        /// Действие, выполняемое над удаляемым заданием.
        /// </summary>
        TaskActionAsync? DeleteTaskActionAsync { get; set; }

        #endregion
    }
}
