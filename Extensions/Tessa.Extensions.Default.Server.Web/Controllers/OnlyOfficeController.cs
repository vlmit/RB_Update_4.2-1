using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using MimeTypes;
using Newtonsoft.Json;
using NLog;
using Tessa.Extensions.Default.Server.OnlyOffice;
using Tessa.Extensions.Default.Server.Web.Filters;
using Tessa.Platform.IO;
using Tessa.Platform.Runtime;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.Web;
using Tessa.Web.Helpers;
using ISession = Tessa.Platform.Runtime.ISession;

namespace Tessa.Extensions.Default.Server.Web.Controllers
{
    /// <summary>
    /// Working with OnlyOffice document server for viewing and editing documents of office formats.
    /// </summary>
    [Route("api/v1/onlyoffice"), AllowAnonymous, ApiController]
    [ProducesErrorResponseType(typeof(PlainValidationResult))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public sealed class OnlyOfficeController(
        IWebHostEnvironment environment,
        ISession session,
        IOnlyOfficeService onlyOfficeService)
        : Controller
    {
        #region Fields

        private readonly IWebHostEnvironment environment = NotNullOrThrow(environment);

        private readonly ISession session = NotNullOrThrow(session);

        private readonly IOnlyOfficeService onlyOfficeService = NotNullOrThrow(onlyOfficeService);

        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        #endregion

        #region Private Methods

        /// <summary>
        /// Получает MIME-тип для указанного расширения.
        /// </summary>
        private static string GetContentType(string fileExt) =>
            string.IsNullOrWhiteSpace(fileExt)
                ? MediaTypeNames.Application.Octet
                : MimeTypeMap.GetMimeType(fileExt);

        #endregion

        #region Controller Methods

        /// <summary>
        /// Get contents of document template from <c>wwwroot/templates</c> folder by specified name.
        /// By default its templates of empty documents: <c>empty.docx, empty.xlsx, empty.pptx</c>.
        /// Returns 404 (No Content) if file isn't found.
        /// </summary>
        /// <param name="name">Name of file template to get.</param>
        /// <param name="cancellationToken">Token to cancel async task.</param>
        /// <returns>Contents of file template, or 404 (No Content) if file isn't found.</returns>
        // GET api/v1/onlyoffice/templates/{name}
        [HttpGet("templates/{name}"), SessionMethod]
        [Produces(MediaTypeNames.Application.Octet, MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTemplate(
            [FromRoute] string name,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNullOrWhiteSpace(name);

            if (name.Contains('/', StringComparison.Ordinal)
                || name.Contains('\\', StringComparison.Ordinal))
            {
                // относительные пути недопустимы
                return this.StatusCode(StatusCodes.Status403Forbidden, string.Empty);
            }

            string path = Path.Combine(this.environment.WebRootPath, "templates", name);
            if (!System.IO.File.Exists(path))
            {
                return this.NotFound();
            }

            FileStream? stream = null;

            try
            {
                stream = FileHelper.OpenRead(path);
                var result = new FileStreamResult(stream, MediaTypeNames.Application.Octet);

                stream = null;
                return result;
            }
            finally
            {
                if (stream is not null)
                {
                    await stream.DisposeAsync();
                }
            }
        }


        /// <summary>
        /// Get state of file opened in editor by its identifier.
        /// Returns property <c>hasChangesAfterClose</c>: if not null then use <c>files/{id}</c> route to get actual file contents,
        /// otherwise file is being edited and use <c>files/{id}/editor</c> route.
        /// </summary>
        /// <param name="id">Identifier of file being edited.</param>
        /// <param name="cancellationToken">Token to cancel async task.</param>
        /// <returns>
        /// <para>JSON object <c>{ hasChangesAfterClose: false | true | null }</c>.</para>
        /// <para><c>null</c> - editor is not yet closed.</para>
        /// <para><c>true</c> - editor is closed, file has changes available via <see cref="GetFinalFile"/> method.</para>
        /// <para><c>false</c> - editor is closed, there were no changes in file.</para>
        /// </returns>
        // GET api/v1/onlyoffice/files/{id}/state
        [HttpGet("files/{id:guid}/state"), SessionMethod]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<JsonResult> GetFileState(
            [FromRoute] Guid id,
            CancellationToken cancellationToken = default)
        {
            IOnlyOfficeFileCacheInfo fileInfo = await this.onlyOfficeService.GetFileStateAsync(id, cancellationToken)
                ?? throw new InvalidOperationException("Specified file isn't found.");

            if (!fileInfo.EditorWasOpen)
            {
                logger.Trace("Editor wasn't opened yet.");
            }

            return this.Json(new { hasChangesAfterClose = fileInfo.EditorWasOpen ? fileInfo.HasChangesAfterClose : false });
        }

        /// <summary>
        /// Get contents of file being edited used for OnlyOffice documents server.
        /// Result doesn't have a content disposition header.
        /// </summary>
        /// <param name="id">Identifier of file being edited.</param>
        /// <param name="token">Access token.</param>
        /// <param name="cancellationToken">Token to cancel async task.</param>
        /// <returns>Contents of the file.</returns>
        // GET api/v1/onlyoffice/files/{id}/editor/?token=...
        [HttpGet("files/{id:guid}/editor"), OnlyOfficeJwtAuthorization]
        [Produces(MediaTypeNames.Application.Octet, MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<FileStreamResult> GetEditableFile(
            [FromRoute] Guid id,
            [FromQuery, OnlyOfficeJwtToken] string token,
            CancellationToken cancellationToken = default)
        {
            var jwtToken = NotNullOrThrow(this.HttpContext.TryGetJwtToken());

            Stream? stream = null;
            try
            {
                (stream, string fileName) = await this.onlyOfficeService.GetEditableFileAsync(id, jwtToken, cancellationToken);
                var contentType = GetContentType(Path.GetExtension(fileName));

                var streamResult = new FileStreamResult(stream, contentType);

                stream = null;
                return streamResult;
            }
            finally
            {
                if (stream is not null)
                {
                    await stream.DisposeAsync();
                }
            }
        }


        /// <summary>
        /// Get contents of file after its editing has been finished using OnlyOffice documents server.
        /// File then can be downloaded by browser.
        /// </summary>
        /// <param name="id">Identifier of file being edited.</param>
        /// <param name="originalFormat">Flag if file should be converted back to its original format.</param>
        /// <param name="cancellationToken">Token to cancel async task.</param>
        /// <returns>Contents of the file.</returns>
        // GET api/v1/onlyoffice/files/{id}/?original-format=false
        [HttpGet("files/{id:guid}"), SessionMethod]
        [Produces(MediaTypeNames.Application.Octet, MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<FileStreamResult> GetFinalFile(
            [FromRoute] Guid id,
            [FromQuery(Name = "original-format")] bool originalFormat = false,
            CancellationToken cancellationToken = default)
        {
            Stream? stream = null;
            try
            {
                (stream, string fileName) = await this.onlyOfficeService.GetFinalFileAsync(id, originalFormat, cancellationToken);
                var contentType = GetContentType(Path.GetExtension(fileName));

                fileName = fileName.Trim();
                if (fileName.Length == 0)
                {
                    fileName = "file";
                }

                this.Response.Headers.ContentDisposition = FileContentHelper.GetAttachmentContentDisposition(fileName);

                var result = new FileStreamResult(stream, contentType);

                stream = null;
                return result;
            }
            finally
            {
                if (stream is not null)
                {
                    await stream.DisposeAsync();
                }
            }
        }


        /// <summary>
        /// <para>Add file contents (passed as request body) to the cache for usage with OnlyOffice document server.
        /// File is linked to the card by its version identifier.</para>
        /// <para>Return JSON with <c>"accessToken"</c> attribute to authenticate requests.</para>
        /// <para>Use route <c>DELETE files/{id}</c> to delete file from the cache when work is finished,
        /// for instance when browser's tab or window is closing.</para>
        /// </summary>
        /// <param name="id">
        /// Internal document identifier, should be unique for each time the file is being opened for editing.
        /// Generate new identifier, pass it to this method, and then to other methods of this API in order to work with it.
        /// </param>
        /// <param name="versionId">Identifier of file version to create in the card.</param>
        /// <param name="name">File name including extension.</param>
        /// <param name="cancellationToken">Token to cancel async task.</param>
        /// <returns>JSON with <c>"accessToken"</c> attribute to authenticate requests.</returns>
        // POST api/v1/onlyoffice/files/{id}/?versionId=...&name=...
        [HttpPost("files/{id:guid}/create"), DisableRequestSizeLimit, SessionMethod]
        [Consumes(MediaTypeNames.Application.Octet), Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateFile(
            [FromRoute] Guid id,
            [FromQuery] Guid versionId,
            [FromQuery] string name,
            CancellationToken cancellationToken = default)
        {
            // берем поток напрямую из запроса, а не параметром имеющим [FromBody],
            // т.к. биндинг не позволяет слать пустое тело, коим может являться пустой текстовый файл.
            Stream contentStream = this.Request.Body;

            try
            {
                string accessToken = await this.onlyOfficeService.CreateFileAsync(id, versionId, name, contentStream, cancellationToken);
                return this.Json(new { accessToken });
            }
            finally
            {
                // чтобы не упасть с ошибкой из-за непрочитанного тела запроса,
                // если в процессе создания файла в кэше возникло исключение
                await contentStream.DrainAsync(cancellationToken);
            }
        }


        /// <summary>
        /// Process callback from OnlyOffice document server, used for serving requests such as saving file and closing document.
        /// Return JSON with <c>"error"</c> property containing numeric error code.
        /// </summary>
        /// <param name="id">Internal document identifier for a file being edited.</param>
        /// <param name="token">Access token.</param>
        /// <param name="data">Callback parameters determined by OnlyOffice document server.</param>
        /// <param name="cancellationToken">Token to cancel async task.</param>
        /// <returns>JSON with <c>"error"</c> property containing numeric error code.</returns>
        // POST api/v1/onlyoffice/files/{id}/callback/?token=...
        [HttpPost("files/{id:guid}/callback"), OnlyOfficeJwtAuthorization]
        [Consumes(MediaTypeNames.Application.Json), Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<JsonResult> Callback(
            [FromRoute] Guid id,
            [FromQuery, OnlyOfficeJwtToken] string token,
            [FromBody] IDictionary<string, object?> data,
            CancellationToken cancellationToken = default)
        {
            var jwtToken = NotNullOrThrow(this.HttpContext.TryGetJwtToken());

            if (logger.IsTraceEnabled)
            {
                logger.Trace($"Callback id={id}, data={JsonConvert.SerializeObject(data)}, users={(data.TryGetValue("users", out object? value) ? value : "null")}" +
                    $", actions={(data.TryGetValue("actions", out object? value1) ? value1 : "null")},\ntoken={token}\n-------------------------------------");
            }

            await this.onlyOfficeService.CallbackAsync(id, jwtToken, data, cancellationToken);

            return this.Json(new { error = 0 });
        }


        /// <summary>
        /// Delete file from cache which is used with OnlyOffice document server.
        /// Call this method when editing has been finished.
        /// </summary>
        /// <param name="id">Internal document identifier for a file being edited.</param>
        /// <param name="cancellationToken">Token to cancel async task.</param>
        /// <returns>204 (No Content).</returns>
        // DELETE api/v1/onlyoffice/files/{id}
        [HttpDelete("files/{id:guid}"), SessionMethod]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Delete(
            [FromRoute] Guid id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await this.onlyOfficeService.DeleteAsync(id, this.session.User.ID, this.session.User.IsAdministrator(), cancellationToken);
                return this.NoContent();
            }
            catch (InvalidOperationException)
            {
                return this.StatusCode(StatusCodes.Status403Forbidden, string.Empty);
            }
        }

        /// <summary>
        /// Delete file from cache which is used with OnlyOffice document server.
        /// Call this method when editing has been finished.
        /// </summary>
        /// <remarks>
        /// This is POST version of the request to delete file. Its used in conjunction with <c>Navigator.sendBeacon</c> browser feature,
        /// which is supporting only POST request. It allows to execute a request when closing the page (browser's tab or window).
        /// In other cases please use <see cref="Delete"/> route: <c>DELETE files/{id}</c>.
        /// </remarks>
        /// <param name="id">Internal document identifier for a file being edited.</param>
        /// <param name="cancellationToken">Token to cancel async task.</param>
        /// <returns>204 (No Content).</returns>
        // POST api/v1/onlyoffice/files/{id}/delete
        [HttpPost("files/{id:guid}/delete"), SessionMethod]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public Task<IActionResult> PostDelete(
            [FromRoute] Guid id,
            CancellationToken cancellationToken = default) =>
            this.Delete(id, cancellationToken);


        /// <summary>
        /// Initiate cooperative editing session.
        /// Returns JSON object with attributes:
        /// <c>"id"</c> - internal document identifier;
        /// <c>"coeditKey"</c> - a key to identify cooperative editing session;
        /// <c>"isNew"</c> - flag whether session is new, otherwise user has joined existing session;
        /// <c>"accessToken"</c> - access token to authenticate requests.
        /// </summary>
        /// <param name="versionId">Identifier of file version to edit in the card.</param>
        /// <param name="name">File name including extension.</param>
        /// <param name="cancellationToken">Token to cancel async task.</param>
        /// <returns>Resulting JSON object.</returns>
        // GET api/v1/onlyoffice/files/coedit
        [HttpGet("files/coedit"), SessionMethod]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<JsonResult> InitiateCoedit(
            [FromQuery] Guid versionId,
            [FromQuery] string name,
            CancellationToken cancellationToken = default)
        {
            (IOnlyOfficeFileCacheInfo fileInfo, bool isNew, string? accessToken) =
                await this.onlyOfficeService.InitiateCoeditAsync(versionId, name, this.session.User.ID, cancellationToken);

            return this.Json(new { id = fileInfo.ID, coeditKey = fileInfo.CoeditKey, isNew, accessToken });
        }

        /// <summary>
        /// Get status of cooperative editing session.
        /// Returns JSON object with attributes:
        /// <c>"lastVersionId"</c> - identifier of last version of the document being edited;
        /// <c>"names"</c> - name of all users who are currently editing the document;
        /// <c>"date"</c> - date and time of last access to the document.
        /// </summary>
        /// <param name="versionId">Identifier of file version which is being edited, present in the card.</param>
        /// <param name="cancellationToken">Token to cancel async task.</param>
        /// <returns>Resulting JSON object.</returns>
        // GET api/v1/onlyoffice/files/coedit-status
        [HttpGet("files/coedit-status"), SessionMethod]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<JsonResult> GetCurrentCoeditStatus(
            [FromQuery] Guid versionId,
            CancellationToken cancellationToken = default)
        {
            (Guid? lastVersionId, string? names, DateTime? date) =
                await this.onlyOfficeService.TryGetCurrentCoeditWithLastVersionAsync(versionId, cancellationToken);

            return this.Json(new { lastVersionId, names, date });
        }

        /// <summary>
        /// <para>State that OnlyOffice editor (and editing session) is forcefully closed.</para>
        /// <para>Usually OnlyOffice sends callback that its editor is closed, but in cases when it didn't,
        /// this request is performed after client timeout has passed.
        /// It is required for cooperative editing sessions to work correctly.</para>
        /// </summary>
        /// <param name="id">Internal document identifier for a file being edited.</param>
        /// <param name="cancellationToken">Token to cancel async task.</param>
        /// <returns>204 (No Content).</returns>
        // POST api/v1/onlyoffice/files/{id}/close
        [HttpPost("files/{id:guid}/close"), SessionMethod]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> CloseEditor(
            [FromRoute] Guid id,
            CancellationToken cancellationToken = default)
        {
            await this.onlyOfficeService.CloseEditorAsync(id, this.session.User.ID, this.session.User.IsAdministrator(), cancellationToken);
            return this.NoContent();
        }

        /// <summary>
        /// Create JWT for OnlyOffice/R7 usage
        /// Return JSON with <c>"token"</c> property containing jwt token or nothing if there is not enabled jwt setting.
        /// </summary>
        /// <param name="id">Internal document identifier for a file being edited.</param>
        /// <param name="token">Access token.</param>
        /// <param name="config">Config to be used to initiat OnlyOffice operation.</param>
        /// <param name="cancellationToken">Token to cancel async task.</param>
        /// <returns>JSON with <c>"token"</c> property containing jwt token or nothing if there is not enabled jwt setting.</returns>
        // POST api/v1/onlyoffice/files/{id}/get_jwt/?token=...
        [HttpPost("files/{id:guid}/get_jwt"), OnlyOfficeJwtAuthorization]
        [Consumes(MediaTypeNames.Application.Json), Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<JsonResult> GetR7JWT(
            [FromRoute] Guid id,
            [FromQuery, OnlyOfficeJwtToken] string token,
            [FromBody] IDictionary<string, object?> config,
            CancellationToken cancellationToken = default)
        {
            var configJson = config.TryGet<string>("config");
            ThrowIfNullOrWhiteSpace(configJson);

            return this.Json(new { token = await onlyOfficeService.GetR7JWTAsync(id, configJson, cancellationToken) });
        }

        #endregion
    }
}
