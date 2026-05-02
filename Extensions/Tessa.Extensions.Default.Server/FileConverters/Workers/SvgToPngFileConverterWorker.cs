#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Tessa.FileConverters;
using Tessa.Jinni;
using Tessa.Platform;

namespace Tessa.Extensions.Default.Server.FileConverters.Workers
{
    /// <summary>
    /// Объект, ответственный за преобразование файла в формат PNG из формата SVG.
    /// Регистрация выполняется по константе <see cref="FileConverterWorkerNames.SvgToPng"/>.
    /// </summary>
    /// <remarks>
    /// Наследники класса могут переопределять методы интерфейса, например, добавив к ним обработку файлов других форматов.
    /// Класс может также реализовывать <see cref="IAsyncDisposable"/> для очистки ресурсов, для этого в наследнике
    /// переопределяется метод <see cref="DisposeAsync"/> и вызывается сначала его базовая реализация.
    /// </remarks>
    /// <remarks>
    /// Создаёт экземпляр класса с указанием его зависимостей.
    /// </remarks>
    /// <param name="proxyFactory"><inheritdoc cref="IJinniBalancerProxyFactory" path="/summary"/></param>
    public class SvgToPngFileConverterWorker(IJinniBalancerProxyFactory proxyFactory) :
        JinniWorkerBase(proxyFactory),
        IFileConverterWorker,
        IAsyncDisposable
    {
        #region IFileConverterWorker Members

        /// <inheritdoc/>
        public virtual async Task ConvertFileAsync(IFileConverterContext context, CancellationToken cancellationToken = default)
        {
            await this.ConvertFileInternalAsync(context, cancellationToken);

            // пишем ключ, через который вызывающая сторона поймёт, что конвертация была выполнена через наш конвертер
            context.ResponseInfo[FileConverterWorkerNames.SvgToPng] = BooleanBoxes.True;
        }

        /// <inheritdoc/>
        public virtual Task PreprocessAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        /// <inheritdoc/>
        public virtual Task PerformMaintenanceAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        #endregion

        #region IAsyncDisposable Members

        /// <inheritdoc/>
        public virtual ValueTask DisposeAsync() => ValueTask.CompletedTask;

        #endregion
    }
}
