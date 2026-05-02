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
    /// Объект, ответственный за преобразование файла в формат <see cref="FileConverterFormat.Pdf"/> из формата TIFF.
    /// Обычно не используется как самостоятельный Worker, а применяется в составе <see cref="PdfFileConverterWorker"/>.
    /// Регистрация выполняется по константе <see cref="FileConverterWorkerNames.TiffToPdf"/>.
    /// </summary>
    /// <remarks>
    /// Наследники класса могут переопределять методы интерфейса, например, добавив к ним обработку файлов других форматов.
    /// Класс может также реализовывать <see cref="IAsyncDisposable"/> для очистки ресурсов, для этого в наследнике
    /// переопределяется метод <see cref="DisposeAsync"/> и вызывается сначала его базовая реализация.
    /// </remarks>
    public class TiffToPdfFileConverterWorker :
        JinniWorkerBase,
        IFileConverterWorker,
        IAsyncDisposable
    {
        #region Constructors

        /// <summary>
        /// Создаёт экземпляр класса с указанием его зависимостей.
        /// </summary>
        /// <param name="proxyFactory"><inheritdoc cref="IJinniBalancerProxyFactory" path="/summary"/></param>
        public TiffToPdfFileConverterWorker(IJinniBalancerProxyFactory proxyFactory)
            : base(proxyFactory)
        {
        }

        #endregion

        #region IFileConverterWorker Members

        /// <inheritdoc/>
        public virtual async Task ConvertFileAsync(IFileConverterContext context, CancellationToken cancellationToken = default)
        {
            // конвертируем файл
            await this.ConvertFileInternalAsync(context, cancellationToken);

            // пишем ключ, через который вызывающая сторона поймёт, что конвертация была выполнена через наш конвертер
            context.ResponseInfo[FileConverterWorkerNames.TiffToPdf] = BooleanBoxes.True;
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
