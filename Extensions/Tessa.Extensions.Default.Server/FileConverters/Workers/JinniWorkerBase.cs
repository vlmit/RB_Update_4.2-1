#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Tessa.FileConverters;
using Tessa.Jinni;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.FileConverters.Workers
{
    public abstract class JinniWorkerBase
    {
        #region Private Fields

        private readonly IJinniBalancerProxyFactory proxyFactory;

        private const int millisecondsDelay = 300;
        private static readonly TimeSpan timeout = TimeSpan.FromMinutes(10);

        #endregion

        #region Constructors

        /// <summary>
        /// Создает экземпляр класса <see cref="JinniWorkerBase"/>.
        /// </summary>
        /// <param name="proxyFactory"><inheritdoc cref="IJinniBalancerProxyFactory" path="/summary"/></param>
        protected JinniWorkerBase(IJinniBalancerProxyFactory proxyFactory) =>
            this.proxyFactory = NotNullOrThrow(proxyFactory);

        #endregion

        #region Protected Methods

        protected async Task ConvertFileInternalAsync(IFileConverterContext context, CancellationToken cancellationToken)
        {
            // Получение токена веб-сервиса с учетом нагрузки
            var token = await this.proxyFactory.GetNextTokenAsync(JinniOperationType.FileConversion, cancellationToken);
            if (!token.HasValue)
            {
                context.ValidationResult.AddError(this, "A suitable web service component was not found to send a file conversion request.");
                return;
            }

            // Создание прокси объекта для обращения к веб-сервису документов
            await using var proxy = await this.proxyFactory.UseProxyAsync<FileConverterWebProxy>(token.Value, cancellationToken);

            // Создание операции конвертации файла на веб-сервисе документов
            var operationID = await proxy.CreateOperationAsync(context, cancellationToken);

            // Отслеживание статуса операции конвертации файла на веб-сервисе документов
            JinniOperationStatus? operationStatus = null;
            using (var cts = new CancellationTokenSource(timeout))
            {
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);
                do
                {
                    var delay = operationStatus is null ? FileConverterHelper.OperationCheckIntervalMilliseconds : millisecondsDelay;
                    await Task.Delay(delay, linkedCts.Token);
                    operationStatus = await proxy.GetOperationStatusAsync(operationID, linkedCts.Token);
                } while (operationStatus.State is JinniOperationState.Created or JinniOperationState.InProgress);
            }

            if (!operationStatus.IsSuccessful)
            {
                context.ValidationResult.AddError(this, operationStatus.ToString());
                return;
            }

            // Получение результата - сконвертированного файла
            var output = await proxy.GetOperationResultAsync(operationID, cancellationToken);
            context.GetOutputContentAsync = _ => new(output);
        }

        #endregion
    }
}
