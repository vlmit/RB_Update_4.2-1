using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using NLog;
using Tessa.Cards;
using Tessa.Cards.Caching;
using Tessa.Extensions.Default.Server.Web.DeskiMobile;
using Tessa.Extensions.Default.Server.Web.DeskiMobile.Models;
using Tessa.Platform;
using Tessa.Platform.Licensing;
using Tessa.Platform.Operations;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.Web;
using Tessa.Web.Helpers;
using Tessa.Web.Serialization;
using FileSignatureResponse = Tessa.Extensions.Default.Server.Web.DeskiMobile.Models.FileSignatureResponse;

namespace Tessa.Extensions.Default.Server.Web.Controllers
{
    /// <summary>
    /// Working with DeskiMobile app used to sign files, verify signatures and preview files contents.
    /// </summary>
    [Route("api/v1/mobile"), AllowAnonymous, ApiController]
    [ProducesErrorResponseType(typeof(PlainValidationResult))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public sealed class DeskiMobileController : Controller
    {
        #region Constructors

        /// <summary>
        /// Конструктор запрашивает зависимости от контейнера ASP.NET Core, у которого они отличаются от зависимостей в UnityContainer.
        /// </summary>
        /// <param name="deskiMobileManager">Класс для управления операцией по взаимодействию с мобильным приложением TESSA Assistant.</param>
        /// <param name="cardCache">Кэш карточек настроек.</param>
        /// <param name="linkGenerator">Определяет контракт для создания абсолютных и связанных URI на основе маршрутизации конечных точек.</param>
        /// <param name="licenseManager">Объект, управляющий лицензиями.</param>
        /// <param name="deskiMobileTokenManager">Manager, управляющий созданием и проверкой Jwt токенов для DeskiMobile.</param>
        public DeskiMobileController(
            IDeskiMobileManager deskiMobileManager,
            ICardCache cardCache,
            LinkGenerator linkGenerator,
            ILicenseManager licenseManager,
            IDeskiMobileTokenManager deskiMobileTokenManager)
        {
            this.deskiMobileManager = NotNullOrThrow(deskiMobileManager);
            this.cardCache = NotNullOrThrow(cardCache);
            this.linkGenerator = NotNullOrThrow(linkGenerator);
            this.licenseManager = NotNullOrThrow(licenseManager);
            this.deskiMobileTokenManager = NotNullOrThrow(deskiMobileTokenManager);
        }

        #endregion

        #region Fields

        private readonly IDeskiMobileManager deskiMobileManager;

        private readonly IDeskiMobileTokenManager deskiMobileTokenManager;

        private readonly ICardCache cardCache;

        private readonly LinkGenerator linkGenerator;

        private readonly ILicenseManager licenseManager;

        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        #endregion

        #region Constants

        private const string LicenseNotFoundMessage = "License for \"Assistant application for mobile devices\" module isn't found.";

        #endregion

        #region Base Overrides

        /// <inheritdoc />
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!(await this.IsDeskiMobileEnabledAsync(context.HttpContext.RequestAborted)))
            {
                logger.Warn("The ability to interact with the Tessa Assistant interaction is disabled in the server settings.");
                context.Result = new NotFoundResult();
                return;
            }

            if (!(await this.HasDeskiMobileLicenseAsync(context.HttpContext.RequestAborted)))
            {
                logger.Warn(LicenseNotFoundMessage);
                throw new InvalidOperationException(LicenseNotFoundMessage);
            }

            await next();
        }

        #endregion

        #region Controller Methods

        /// <summary>
        /// <para>Create operation to work with TESSA Assistant. Ensures that there is a TESSA Assistant app on a user's device.
        /// Used by web client, requires authenticated user.</para>
        /// <para>Returns link to communicate with TESSA Assistant app.</para>
        /// </summary>
        /// <param name="operation">Type of operation to create.</param>
        /// <param name="request">Request with operation parameters.</param>
        /// <param name="cancellationToken">Token to cancel async task.</param>
        /// <returns>link to communicate with TESSA Assistant app.</returns>
        // POST api/v1/mobile/init-operation
        [HttpPost("init-operation"), SessionMethod, TypedJsonBody(ConvertPascalCasing = true)]
        [Consumes(MediaTypeNames.Application.Json), Produces(MediaTypeNames.Text.Plain)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<string>> PostInitOperation(
            [FromQuery] string operation,
            [FromBody] InitOperationRequest request,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(request);

            var url = await this.GetLinkAsync(DeskiMobileTokenPermissionFlags.None, cancellationToken);
            ThrowIfNull(url);

            return await this.deskiMobileManager.GenerateLinkAsync(request.Files.ToArray(), operation, url, cancellationToken);
        }

        /// <summary>
        /// Delete operation. Used by web client, requires authenticated user with JWT Token.
        /// Returns <c>204 (No Content)</c> on success, otherwise status code is unsuccessful.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel async task.</param>
        /// <returns><c>204 (No Content)</c> on success, otherwise status code is unsuccessful.</returns>
        // DELETE api/v1/mobile/created-operation
        [HttpDelete("created-operation")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult> DeleteCreatedOperation(CancellationToken cancellationToken = default)
        {
            var tokenInfo = this.deskiMobileTokenManager.GetToken(this.Request);
            await this.deskiMobileManager.DeleteOperationAsync(tokenInfo, cancellationToken);
            return this.NoContent();
        }

        /// <summary>
        /// Get state of operation. Used by web client, requires authenticated user with JWT Token.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel async task.</param>
        /// <returns>Operation state.</returns>
        // GET api/v1/mobile/operation-status
        [HttpGet("operation-status")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<OperationState>> GetOperationStatus(CancellationToken cancellationToken = default)
        {
            var tokenInfo = this.deskiMobileTokenManager.GetToken(this.Request);
            var state = await this.deskiMobileManager.GetOperationStateAsync(tokenInfo, cancellationToken);
            return this.Json(state.ToString());
        }

        /// <summary>
        /// Get completed operation results if present and delete it.
        /// Used by web client, requires authenticated user user with JWT Token.
        /// </summary>
        /// <param name="forceDelete">Flag for force delete operation.</param>
        /// <param name="cancellationToken">Token to cancel async task.</param>
        /// <returns>JSON object with operation results, and flag whether it was deleted.</returns>
        // POST api/v1/mobile/try-get-response-and-delete?forceDelete=false
        [HttpPost("try-get-response-and-delete")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<DeleteOperationResponse>> TryGetCompletedResponseAndDelete(
            [FromQuery] bool forceDelete = false,
            CancellationToken cancellationToken = default)
        {
            var tokenInfo = this.deskiMobileTokenManager.GetToken(this.Request);
            var state = await this.deskiMobileManager.GetOperationStateAsync(tokenInfo, cancellationToken);
            if (state != OperationState.Completed && !forceDelete)
            {
                return new DeleteOperationResponse { Deleted = false, Response = null };
            }

            var response = await this.deskiMobileManager.TryGetOperationResponseAsync(tokenInfo, cancellationToken);
            await this.deskiMobileManager.DeleteOperationAsync(tokenInfo, cancellationToken);
            return new DeleteOperationResponse { Deleted = true, Response = response };
        }

        /// <summary>
        /// <para>Start operation by changing its state to <c>InProgress</c> (observed by web client). Used by TESSA Assistant app.</para>
        /// <para>Returns JSON object with info on how to communicate with API for working on operation,
        /// for instance how to download file, get its signatures, etc.</para>
        /// </summary>
        /// <param name="cancellationToken">Token to cancel async task.</param>
        /// <returns>JSON object containing info on how to communicate with API for working on operation.</returns>
        // POST api/v1/mobile/start-operation
        [HttpPost("start-operation")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<StartOperationResponse>> PostStartOperation(CancellationToken cancellationToken = default)
        {
            var tokenInfo = this.deskiMobileTokenManager.GetToken(this.Request);

            var flags = tokenInfo.Access;

            var filesInfo = await this.deskiMobileManager.GetOperationFilesInfoAsync(tokenInfo, cancellationToken);

            string? getContentUrl = null;
            if (flags.HasFlag(DeskiMobileTokenPermissionFlags.GetContent))
            {
                getContentUrl = await this.GetLinkAsync(DeskiMobileTokenPermissionFlags.GetContent, cancellationToken);
            }

            string? getSignaturesUrl = null;
            if (flags.HasFlag(DeskiMobileTokenPermissionFlags.GetSignatures))
            {
                getSignaturesUrl = await this.GetLinkAsync(DeskiMobileTokenPermissionFlags.GetSignatures, cancellationToken);
            }

            string? postEnhanceUrl = null;
            if (flags.HasFlag(DeskiMobileTokenPermissionFlags.Enhance))
            {
                postEnhanceUrl = await this.GetLinkAsync(DeskiMobileTokenPermissionFlags.Enhance, cancellationToken);
            }

            string? postVerifyUrl = null;
            if (flags.HasFlag(DeskiMobileTokenPermissionFlags.Verify))
            {
                postVerifyUrl = await this.GetLinkAsync(DeskiMobileTokenPermissionFlags.Verify, cancellationToken);
            }

            string? postCancelUrl = null;
            if (flags.HasFlag(DeskiMobileTokenPermissionFlags.CancelOperation))
            {
                postCancelUrl = await this.GetLinkAsync(DeskiMobileTokenPermissionFlags.CancelOperation, cancellationToken);
            }

            await this.deskiMobileManager.StartOperationAsync(tokenInfo, cancellationToken);

            return new StartOperationResponse
            {
                GetContent = getContentUrl,
                PostCancel = postCancelUrl,
                GetSignatures = getSignaturesUrl,
                PostVerify = postVerifyUrl,
                PostEnhance = postEnhanceUrl,
                PostHiddenVerifyWebDialog = null,
                FilesInfo = filesInfo
            };
        }

        /// <summary>
        /// Get operation's file contents as binary stream. Used by TESSA Assistant app.
        /// </summary>
        /// <param name="cacheID">Cache ID in operation.</param>
        /// <param name="cancellationToken">Token to cancel async task.</param>
        /// <returns>Stream with file contents.</returns>
        // GET api/v1/mobile/get-file-content?cacheID={cacheID}
        [HttpGet("get-file-content")]
        [Produces(MediaTypeNames.Application.Octet)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<Stream>> GetFileContent(
            [FromQuery] string cacheID,
            CancellationToken cancellationToken = default)
        {
            var tokenInfo = this.deskiMobileTokenManager.GetToken(this.Request);
            var fileName = await this.deskiMobileManager.GetFileNameAsync(tokenInfo, cacheID, cancellationToken);
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = "file";
            }

            this.Response.Headers.ContentDisposition = FileContentHelper.GetAttachmentContentDisposition(fileName);
           
            var fileStream = await this.deskiMobileManager.GetFileContentAsync(tokenInfo, cacheID, cancellationToken);

            return new FileStreamResult(fileStream, MediaTypeNames.Application.Octet);
        }

        /// <summary>
        /// Get info on signatures for operation's file. Used by TESSA Assistant app.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel async task.</param>
        /// <returns>List of object with info on file signatures.</returns>
        // GET api/v1/mobile/get-file-signatures
        [HttpGet("get-file-signatures")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<Dictionary<string, List<FileSignatureResponse>>>> GetFileSignatures(CancellationToken cancellationToken = default)
        {
            var tokenInfo = this.deskiMobileTokenManager.GetToken(this.Request);
            return await this.deskiMobileManager.GetSignaturesAsync(tokenInfo, cancellationToken);
        }

        /// <summary>
        /// Enhance the signature for operation by its identifier. Used by TESSA Assistant app.
        /// Returns <c>204 (No Content)</c> on success, otherwise status code is unsuccessful.
        /// </summary>
        /// <param name="parameters">Request with signature as base64 string.</param>
        /// <param name="cancellationToken">Token to cancel async task.</param>
        /// <returns><c>204 (No Content)</c> on success, otherwise status code is unsuccessful.</returns>
        // POST api/v1/mobile/enhance
        [HttpPost("enhance"), TypedJsonBody]
        [Consumes(MediaTypeNames.Application.Json), Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult> PostEnhance(
            [FromBody] DeskiMobileEnhanceRequest parameters,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(parameters);

            var tokenInfo = this.deskiMobileTokenManager.GetToken(this.Request);
            var response = await this.deskiMobileManager.GetOperationResponseForEnhanceAsync(tokenInfo, parameters, cancellationToken);
            await this.deskiMobileManager.CompleteOperationAsync(tokenInfo, response, cancellationToken);
            return this.NoContent();
        }

        /// <summary>
        /// Verify the signatures for operation by its identifier. Used by TESSA Assistant app.
        /// Returns results of verification for each signature identifier.
        /// </summary>
        /// <param name="parameters">Request with signatures identifiers to verify.</param>
        /// <param name="cancellationToken">Token to cancel async task.</param>
        /// <returns>Results of verification for each signature identifier.</returns>
        // POST api/v1/mobile/verify
        [HttpPost("verify"), TypedJsonBody]
        [Consumes(MediaTypeNames.Application.Json), Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<OperationResponse>> PostVerify(
            [FromBody] DeskiMobileVerifyRequest parameters,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(parameters);

            var tokenInfo = this.deskiMobileTokenManager.GetToken(this.Request);
            var response = await this.deskiMobileManager.GetOperationResponseForVerifyAsync(tokenInfo, parameters, cancellationToken);
            await this.deskiMobileManager.CompleteOperationAsync(tokenInfo, response, cancellationToken);
            return response;
        }

        /// <summary>
        /// Cancel operation. Used by TESSA Assistant app.
        /// Returns <c>204 (No Content)</c> on success, otherwise status code is unsuccessful.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel async task.</param>
        /// <returns><c>204 (No Content)</c> on success, otherwise status code is unsuccessful.</returns>
        // POST api/v1/mobile/cancel
        [HttpPost("cancel")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult> PostCancel(CancellationToken cancellationToken = default)
        {
            var tokenInfo = this.deskiMobileTokenManager.GetToken(this.Request);
            var response = this.deskiMobileManager.GetOperationResponseForCancel(tokenInfo);
            await this.deskiMobileManager.CompleteOperationAsync(tokenInfo, response, cancellationToken);
            return this.NoContent();
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Генерация ссылки для выполнения запроса из TESSA Assistant.
        /// </summary>
        /// <param name="flag">Разрешение на операцию с TESSA Assistant.</param>
        /// <param name="cancellationToken">Token to cancel async task.</param>
        /// <returns>Ссылка для выполнения запроса из TESSA Assistant.</returns>
        private async ValueTask<string?> GetLinkAsync(DeskiMobileTokenPermissionFlags flag, CancellationToken cancellationToken = default)
        {
            var webAddress = await this.TryGetWebAddressAsync(cancellationToken);
            webAddress = LinkHelper.NormalizeWebAddress(webAddress);
            switch (flag)
            {
                case DeskiMobileTokenPermissionFlags.GetContent:
                    if (string.IsNullOrEmpty(webAddress))
                    {
                        return this.linkGenerator.GetUriByAction(this.HttpContext, nameof(this.GetFileContent), null, null);
                    }

                    return $"{webAddress}api/v1/mobile/get-file-content";
                case DeskiMobileTokenPermissionFlags.GetSignatures:
                    if (string.IsNullOrEmpty(webAddress))
                    {
                        return this.linkGenerator.GetUriByAction(this.HttpContext, nameof(this.GetFileSignatures), null, null);
                    }

                    return $"{webAddress}api/v1/mobile/get-file-signatures";
                case DeskiMobileTokenPermissionFlags.Enhance:
                    if (string.IsNullOrEmpty(webAddress))
                    {
                        return this.linkGenerator.GetUriByAction(this.HttpContext, nameof(this.PostEnhance), null, null);
                    }

                    return $"{webAddress}api/v1/mobile/enhance";
                case DeskiMobileTokenPermissionFlags.Verify:
                    if (string.IsNullOrEmpty(webAddress))
                    {
                        return this.linkGenerator.GetUriByAction(this.HttpContext, nameof(this.PostVerify), null, null);
                    }

                    return $"{webAddress}api/v1/mobile/verify";
                case DeskiMobileTokenPermissionFlags.CancelOperation:
                    if (string.IsNullOrEmpty(webAddress))
                    {
                        return this.linkGenerator.GetUriByAction(this.HttpContext, nameof(this.PostCancel), null, null);
                    }

                    return $"{webAddress}api/v1/mobile/cancel";
                default:
                    if (string.IsNullOrEmpty(webAddress))
                    {
                        return this.linkGenerator.GetUriByAction(this.HttpContext, nameof(this.PostStartOperation), null, null);
                    }

                    return $"{webAddress}api/v1/mobile/start-operation";
            }
        }

        /// <summary>
        /// Получение всех полей из карточки с настройками сервера.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel async task.</param>
        /// <returns>Настройки сервера.</returns>
        private async ValueTask<Dictionary<string, object?>> GetServerInstancesFieldsAsync(CancellationToken cancellationToken = default)
        {
            var card = await this.cardCache.Cards.GetAsync(CardHelper.ServerInstanceTypeName, cancellationToken);
            return card.GetValue().Sections["ServerInstances"].RawFields;
        }

        /// <summary>
        /// Получение базового адреса web-клиента из карточки с настройками сервера.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel async task.</param>
        /// <returns>Базовый адрес web-клиента.</returns>
        private async ValueTask<string?> TryGetWebAddressAsync(CancellationToken cancellationToken = default)
        {
            var fields = await this.GetServerInstancesFieldsAsync(cancellationToken);
            return fields.Get<string?>("WebAddress");
        }

        /// <summary>
        /// Получение флага доступности мобильного приложения из карточки с настройками сервера.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel async task.</param>
        /// <returns>true - если флаг для работы с мобильным приложением установлен, иначе - false.</returns>
        private async ValueTask<bool> IsDeskiMobileEnabledAsync(CancellationToken cancellationToken = default)
        {
            var fields = await this.GetServerInstancesFieldsAsync(cancellationToken);
            return fields.Get<bool>("DeskiMobileEnabled");
        }

        /// <summary>
        /// Проверка наличия модуля TESSA Assistant в лицензии
        /// </summary>
        /// <param name="cancellationToken">Token to cancel async task.</param>
        /// <returns>true - если модуль присутствует в лицензии, иначе - false.</returns>
        private async ValueTask<bool> HasDeskiMobileLicenseAsync(CancellationToken cancellationToken = default) =>
            (await this.licenseManager.GetLicenseAsync(cancellationToken)).Modules.Contains(LicenseModules.MobileAssistantID);

        #endregion
    }
}
