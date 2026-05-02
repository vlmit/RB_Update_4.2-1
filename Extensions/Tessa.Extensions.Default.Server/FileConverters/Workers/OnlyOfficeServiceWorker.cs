#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NLog;
using Tessa.Cards;
using Tessa.Extensions.Default.Server.OnlyOffice;
using Tessa.Extensions.Default.Server.OnlyOffice.Token;
using Tessa.FileConverters;
using Tessa.Platform;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.FileConverters.Workers
{
    /// <summary>
    /// Объект, ответственный за преобразование файла в формат <see cref="FileConverterFormat.Pdf"/>
    /// посредством внешней программы OnlyOffice Document Builder
    /// </summary>
    /// <remarks>
    /// Наследники класса могут переопределять методы интерфейса, например, добавив к ним обработку файлов других форматов.
    /// Класс может также реализовывать <see cref="IAsyncDisposable"/> для очистки ресурсов, для этого в наследнике
    /// переопределяется метод <see cref="DisposeAsync"/> и вызывается сначала его базовая реализация.
    /// </remarks>
    public class OnlyOfficeServiceWorker :
        IFileConverterWorker,
        IAsyncDisposable
    {
        #region Fields

        private readonly IOnlyOfficeSettingsProvider settingsProvider;

        private readonly IOnlyOfficeService onlyOfficeService;

        private readonly ICardStreamServerRepository streamServerRepositoryExt;

        private readonly IOnlyOfficeR7TokenManager r7TokenManager;

        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        #endregion

        #region Constructor

        /// <summary>
        /// Создаёт экземпляр класса с указанием его зависимостей.
        /// </summary>
        /// <param name="settingsProvider"><inheritdoc cref="IOnlyOfficeSettingsProvider" path="/summary"/></param>
        /// <param name="onlyOfficeService"><inheritdoc cref="IOnlyOfficeService" path="/summary"/></param>
        /// <param name="streamServerRepositoryExt"><inheritdoc cref="ICardStreamServerRepository" path="/summary"/></param>
        public OnlyOfficeServiceWorker(
            IOnlyOfficeSettingsProvider settingsProvider,
            IOnlyOfficeService onlyOfficeService,
            ICardStreamServerRepository streamServerRepositoryExt,
            IOnlyOfficeR7TokenManager r7TokenManager)
        {
            this.settingsProvider = NotNullOrThrow(settingsProvider);
            this.onlyOfficeService = NotNullOrThrow(onlyOfficeService);
            this.streamServerRepositoryExt = NotNullOrThrow(streamServerRepositoryExt);
            this.r7TokenManager = NotNullOrThrow(r7TokenManager);
        }

        #endregion

        #region IFileConverterWorker members

        public async Task ConvertFileAsync(IFileConverterContext context, CancellationToken cancellationToken = default)
        {
            logger.Trace("Start converting");

            var settings = await this.settingsProvider.GetSettingsAsync(cancellationToken);
            await this.ConvertFileWithServiceAsync(context, settings, cancellationToken);

            // Запись ключа, через который вызывающая сторона поймёт, что конвертация была выполнена посредством OnlyOffice
            context.ResponseInfo[FileConverterWorkerNames.OnlyOfficeServiceToPdf] = BooleanBoxes.True;

            logger.Trace("End converting");
        }

        /// <inheritdoc/>
        public Task PerformMaintenanceAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        /// <inheritdoc/>
        public Task PreprocessAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        #endregion

        #region IAsyncDisposable

        /// <inheritdoc/>
        public virtual ValueTask DisposeAsync() => ValueTask.CompletedTask;

        #endregion

        #region Private Methods

        /// <summary>
        /// Конвертирует файл через сервис OnlyOffice
        /// </summary>
        /// <param name="context">Контекст конвертации.</param>
        /// <param name="settings">Настройки для OnlyOffice.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Контент файла</returns>
        private async Task ConvertFileWithServiceAsync(
            IFileConverterContext context,
            IOnlyOfficeSettings settings,
            CancellationToken cancellationToken = default)
        {
            var id = Guid.NewGuid();
            string accessToken;

            var request = NotNullOrThrow(context.Request);
            ThrowIfNullOrEmpty(request.FileName); // file name is used in CreateFileAsync

            await using (var stream =
                         await await this.GetFileStreamAsync(
                             request.FileName,
                             request.CardID,
                             request.FileID,
                             request.VersionID,
                             request.FileTypeName,
                             request.Info.ToDictionaryStorage(),
                             cancellationToken))
            {
                accessToken = await this.onlyOfficeService.CreateFileAsync(
                    id,
                    request.VersionID,
                    request.FileName,
                    stream,
                    cancellationToken);
            }

            var httpClient = CreateOnlyOfficeHttpClient(settings);
            context.FinalizationQueue.Add(() =>
            {
                httpClient.Dispose();
                return ValueTask.CompletedTask;
            });

            var output = await this.ConvertFileInternalAsync(
                httpClient,
                settings.ConverterUrl ?? string.Empty,
                GetFileUrl(settings, id, accessToken),
                context.OutputExtension,
                context.InputExtension,
                cancellationToken);

            context.GetOutputContentAsync = _ => new(output);

            await this.onlyOfficeService.DeleteAsync(id, Guid.Empty, true, cancellationToken);
        }

        private async Task<ValueTask<Stream>> GetFileStreamAsync(
            string? name,
            Guid cardId,
            Guid fileId,
            Guid versionId,
            string? fileTypeName,
            Dictionary<string, object?>? info = null,
            CancellationToken cancellationToken = default)
        {
            var contentRequest = new CardGetFileContentRequest
            {
                ServiceType = CardServiceType.Default,
                VersionRowID = versionId,
                CardID = cardId,
                FileID = fileId,
                FileName = name,
                FileTypeName = fileTypeName,
                Info = info ?? new()
            };

            // Нам не нужно снова запускать конвертацию файла, если мы хотим получить его контент.
            // Актуально для шаблонных файлов, которые необходимо конвертировать в PDF.
            contentRequest.SetConverterFormat(FileConverterFormat.Unknown);

            var contentResult = await this.streamServerRepositoryExt.GetFileContentAsync(contentRequest, cancellationToken);
            var contentResponse = contentResult.Response;

            if (!contentResult.HasContent || !contentResponse.ValidationResult.IsSuccessful())
            {
                // логирование для необработанного исключения
                throw new ValidationException(contentResponse.ValidationResult.Build());
            }

            return contentResult.GetContentOrThrowAsync(cancellationToken);
        }

        /// <summary>
        /// Конвертирует указанный файл в необходимый формат, используя сервер документов.
        /// </summary>
        /// <param name="httpClient">Клиент для работы с HTTP-запросами.</param>
        /// <param name="converterUrl">Ссылка на эндпоинт конвертера.</param>
        /// <param name="fileUrl">Путь к исходному файлу.</param>
        /// <param name="outExt">Расширение, в которое необходимо конвертировать.</param>
        /// <param name="inputExt">Возможность передать расширение файла. Необязательный параметр</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>
        /// Сконвертированный файл в виде файлового потока и его размер.
        /// Если размер файла не удалось определить, то его значение будет равно <c>-1</c>.
        /// </returns>
        private async Task<(Stream Stream, long Length)> ConvertFileInternalAsync(
            HttpClient httpClient,
            string converterUrl,
            string fileUrl,
            string outExt,
            string inputExt,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNullOrWhiteSpace(converterUrl);

            var body = new Dictionary<string, object?>
            {
                { "filetype", inputExt },
                { "outputtype", outExt },
                { "key", Guid.NewGuid().ToString("N")[..20] },
                { "url", fileUrl }
            };
            var bodyStr = JsonConvert.SerializeObject(body);

            var token = await this.r7TokenManager.CreateTokenAsync(bodyStr);
            if (token is not null) {
                body["token"] = token;
                bodyStr = JsonConvert.SerializeObject(body);
            }

            string? convertedFileUrl;

            using (var content = new StringContent(bodyStr, Encoding.UTF8, MediaTypeNames.Application.Json))
            {
                using var convertFileResponse = await httpClient.PostAsync(converterUrl, content, cancellationToken);
                var convertFileDataJson = await convertFileResponse.Content.ReadAsStringAsync(cancellationToken);
                logger.Trace("ConvertFile response: " + convertFileDataJson);
                var convertFileDataStorage = JsonConvert.DeserializeObject<Dictionary<string, object?>>(convertFileDataJson);
                convertedFileUrl = convertFileDataStorage?.Get<string>("fileUrl") ?? string.Empty;
            }

            return await GetFileStreamByUrlAsync(httpClient, convertedFileUrl, cancellationToken);
        }

        private static string GetFileUrl(IOnlyOfficeSettings settings, Guid id, string fileToken) =>
            $"{settings.WebApiBasePath?.TrimEnd('/')}/files/{id}/editor?token={fileToken}";

        /// <summary>
        /// Получает поток файла по указанному пути.
        /// </summary>
        /// <param name="httpClient">Объект, посредством которого выполняется вызов OnlyOffice.</param>
        /// <param name="fileUrl">URL к файлу.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>
        /// Сконвертированный файл в виде файлового потока и его размер.
        /// Если размер файла не удалось определить, то его значение будет равно <c>-1</c>.
        /// </returns>
        private static async Task<(Stream Stream, long Length)> GetFileStreamByUrlAsync(
            HttpClient httpClient,
            string fileUrl,
            CancellationToken cancellationToken = default)
        {
            // Не выполняем освобождение ресурсов для response, так как ответ еще не прочитан (прочитаны только заголовки).
            // Освобождение будет выполняться вместе с HttpClient в FinalizationQueue.
            HttpResponseMessage response = await httpClient.GetAsync(fileUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            long length = response.Content.Headers.ContentLength
                ?? (stream.CanSeek ? stream.Length : (long?) null)
                ?? -1L;

            return (stream, length);
        }

        /// <summary>
        /// Создаёт объект <see cref="HttpClient"/> для обращения к веб-сервису OnlyOffice.
        /// Используйте конструкцию <c>using</c>, чтобы закрыть соединение с сервисом.
        /// </summary>
        /// <param name="settings">Настройки OnlyOffice, полученные из карточки настроек.</param>
        /// <returns>Созданный объект.</returns>
        private static HttpClient CreateOnlyOfficeHttpClient(IOnlyOfficeSettings settings)
        {
            HttpClient? httpClient = null;

            try
            {
                httpClient = new HttpClient { Timeout = settings.LoadTimeout };
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));

                HttpClient result = httpClient;
                httpClient = null;

                return result;
            }
            finally
            {
                httpClient?.Dispose();
            }
        }

        #endregion
    }
}
