#nullable enable

using System.Threading;
using System.Threading.Tasks;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Serialization
{
    /// <summary>
    /// Объект, предоставляющий параметры сериализатора <see cref="IKrStageSerializer"/>.
    /// </summary>
    public interface IKrStageSerializerSettingsCache
    {
        /// <summary>
        /// Возвращает значение, сохранённое в кэше.
        /// </summary>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns><inheritdoc cref="IKrStageSerializerSettings" path="/summary"/></returns>
        ValueTask<IKrStageSerializerSettings> GetAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Устанавливает новое значение в кэш.
        /// </summary>
        /// <param name="value"><inheritdoc cref="IKrStageSerializerSettings" path="/summary"/></param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Асинхронная задача.</returns>
        ValueTask SetAsync(
            IKrStageSerializerSettings value,
            CancellationToken cancellationToken = default);
    }
}
