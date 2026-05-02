using System.Threading;
using System.Threading.Tasks;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Workflow.KrCompilers
{
    /// <summary>
    /// Объект для получения блокировок на чтение и запись шаблонов этапов и групп этапов.
    /// </summary>
    public interface IKrStageTemplateLockStrategy
    {
        /// <summary>
        /// Выполняет взятие блокировки на чтение блокировки шаблонов этапов и групп этапов.
        /// </summary>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Результат взятия блокировки.</returns>
        Task<ValidationResult> ObtainReaderLockAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Выполняет взятие блокировки шаблонов этапов и групп этапов.
        /// </summary>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Результат взятия блокировки.</returns>
        Task<ValidationResult> ObtainWriterLockAsync(CancellationToken cancellationToken = default);
    }
}
