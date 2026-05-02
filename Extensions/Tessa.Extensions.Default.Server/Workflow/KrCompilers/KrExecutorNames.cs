#nullable enable

namespace Tessa.Extensions.Default.Server.Workflow.KrCompilers
{
    /// <summary>
    /// Имена объектов <see cref="IKrExecutor"/>, которые регистрируются в Unity.
    /// </summary>
    public class KrExecutorNames
    {
        /// <summary>
        /// Имя, по которому из Unity-контейнера можно получить объект <see cref="KrGroupExecutor"/>.
        /// </summary>
        /// <remarks>Этот же объект можно получить по интерфейсу <see cref="IKrExecutor"/>.</remarks>
        public const string GroupExecutor = nameof(GroupExecutor);

        /// <summary>
        /// Имя, по которому из Unity-контейнера можно получить объект <see cref="KrStageExecutor"/>.
        /// </summary>
        public const string StageExecutor = nameof(StageExecutor);
    }
}
