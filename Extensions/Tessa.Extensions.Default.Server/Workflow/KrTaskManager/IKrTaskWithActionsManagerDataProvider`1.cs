#nullable enable

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager
{
    /// <summary>
    /// Объект, обеспечивающий передачу данных между внешней подсистемой и <see cref="IKrTaskManager{T}"/> и предоставляет действия, выполняемые над заданием.
    /// </summary>
    /// <typeparam name="T">Тип объекта, содержащего информацию о исполнителе.</typeparam>
    public interface IKrTaskWithActionsManagerDataProvider<T> :
        IKrTaskManagerDataProvider
    {
        #region Properties

        /// <summary>
        /// Действие, выполняемое над созданным заданием.
        /// </summary>
        CreateTaskActionAsync<T>? CreateTaskActionAsync { get; set; }

        /// <summary>
        /// Действие, выполняемое над делегированным заданием.
        /// </summary>
        DelegateTaskActionAsync<T>? DelegateTaskActionAsync { get; set; }

        #endregion
    }
}
