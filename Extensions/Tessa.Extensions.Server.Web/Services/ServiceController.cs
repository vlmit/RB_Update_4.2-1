using System;
using System.Net;
using System.Net.Mime;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Tessa.Cards;
using Tessa.Extensions.Shared.Services;
using Tessa.Platform;
using Tessa.Platform.Data;
using Tessa.Platform.Operations;
using Tessa.Platform.Runtime;
using Tessa.Platform.Validation;
using Tessa.Web;
using Tessa.Web.Client;
using Tessa.Web.Serialization;
using Tessa.Web.Services;
using Unity;

namespace Tessa.Extensions.Server.Web.Services
{
    /// <summary>
    /// Контроллер, для которого задан базовый путь "service". Является примером реализации сервисов в рамках приложения TESSA.
    /// </summary>
    /// <remarks>
    /// ВАЖНО: Это просто пример того, как можно написать контроллер с какой-то кастомной логикой.
    /// <para>Вы можете его удалить, и добавить сколько угодно своих классов-контроллеров, наследуемых от <see cref="Controller"/>
    /// или <see cref="Controller"/>, имеющих разные маршруты (адреса) в атрибуте <see cref="RouteAttribute"/>, разные зависимости в конструкторе.</para>
    /// <para>Ваши контроллеры можно расположить в разных сборках, помимо <c>Tessa.Extensions.Server.Web</c>, путь к сборкам указывается
    /// в <c>app.json</c> веб-сервиса по ключу <c>WebControllers</c>.</para>
    /// <para>Конструктор запрашивает зависимости из контейнера .NET DI, а при отсутствии в нём регистрации
    /// или при указании на параметре атрибута <see cref="DependencyAttribute"/> и <see cref="OptionalDependencyAttribute"/> - из контейнера <see cref="IUnityContainer"/>.</para>
    /// </remarks>
    /// <param name="serverSettings">Настройки TESSA на сервере, которые выносятся в конфигурационный файл.</param>
    /// <param name="sessionServer">Объект, обеспечивающий взаимодействие с сессиями на сервере.</param>
    /// <param name="sessionService">Сервис, управляющий открытыми сессиями.</param>
    /// <param name="dbScope">Объект для взаимодействия с базой данных.</param>
    /// <param name="cardRepository">Репозиторий для управления карточками.</param>
    /// <param name="unityContainer">Контейнер Unity.</param>
    /// <param name="hostEnvironment">Информация по среде выполнения ASP.NET Core. Запрошена для примера, обычно не требуется.</param>
    /// <param name="options">Настройки веб-сервиса.</param>
    [Route("service"), AllowAnonymous, ApiController]
    [ProducesErrorResponseType(typeof(PlainValidationResult))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public sealed class ServiceController(
        ITessaServerSettings serverSettings,
        ISessionServer sessionServer,
        ISessionService sessionService,
        IDbScope dbScope,
        [Dependency(CardRepositoryNames.Extended)] ICardRepository cardRepository,
        IUnityContainer unityContainer,
        IWebHostEnvironment hostEnvironment,
        IOptions<WebOptions> options)
        : Controller
    {
        #region Fields

        private readonly ITessaServerSettings serverSettings = NotNullOrThrow(serverSettings);

        private readonly ISessionServer sessionServer = NotNullOrThrow(sessionServer);

        private readonly ISessionService sessionService = NotNullOrThrow(sessionService);

        private readonly IDbScope dbScope = NotNullOrThrow(dbScope);

        private readonly ICardRepository cardRepository = NotNullOrThrow(cardRepository);

        private readonly IUnityContainer unityContainer = NotNullOrThrow(unityContainer);

        private readonly IWebHostEnvironment hostEnvironment = NotNullOrThrow(hostEnvironment);

        private readonly IOptions<WebOptions> options = NotNullOrThrow(options);

        #endregion

        #region Private Methods

        private string GetLoadedExtensions() => string.Join(Environment.NewLine, this.unityContainer.ResolveAssemblyInfo().ServerExtensions);

        #endregion

        #region Controller Methods

        /*
         * Метод доступен по базовому адресу контроллера, не требует авторизации и не обращается к сессии.
         * Для проверки функционирования сервиса перейдите по URL вида: https://localhost/tessa/web/service
         *
         * Указываем, что метод может вернуть Json, если возникнет исключение - оно будет упаковано в PlainValidationResult.
         */
        /// <summary>
        /// Возвращает текстовое описание для конфигурации веб-сервиса, если в конфигурации
        /// установлена настройка <c>HealthCheckIsEnabled</c> равной <c>true</c>.
        /// </summary>
        /// <returns>Текстовое описание для конфигурации веб-сервиса.</returns>
        // GET service
        [HttpGet, Produces(MediaTypeNames.Text.Plain, MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public string Get() =>
            this.options.Value.HealthCheckIsEnabled
                ? $"Syntellect TESSA, build {BuildInfo.Version} of {FormatDate(BuildInfo.Date, convertToLocal: false)}{Environment.NewLine}" +
                $"Environment: \"{this.hostEnvironment.EnvironmentName}\".{Environment.NewLine}{Environment.NewLine}" +
                $"Running on {EnvironmentHelper.OSQualifiedFriendlyName ?? "Unknown OS"}{Environment.NewLine}" +
                $"{RuntimeInformation.FrameworkDescription}{Environment.NewLine}{Environment.NewLine}" +
                $"Server extensions:{Environment.NewLine}{this.GetLoadedExtensions()}"
                : "Health check is disabled in configuration";

        /*
         * Метод для входа по паре логин/пароль. Здесь используется клиентская информация по умолчанию и идентификатор неизвестного приложения;
         * обычно вместо этого задают конкретное приложение и передают параметры.
         *
         * В методе не поддерживается двухфакторная аутентификация, так как она требует обратного взаимодействия от клиента. Учтите это,
         * при настройке параметров аутентификации для платформы, и для конкретного сотрудника в частности.
         *
         * Метод возвращает строку с токеном, которую надо передавать в следующие методы
         * или же использоваться API TESSA для проброса токена в HTTP-заголовок "Tessa-Session".
         *
         * Указываем TypedJsonBody, чтобы можно было получать как типизированный, так и нетипизированный json в качестве параметра.
         * ConvertPascalCasing конвертирует в названиях свойств объектов первую строчную букву в прописную. Например: userName => UserName.
         *
         * Рекомендуется использовать метод открытия сессии из REST API, если это возможно: /api/v2/sessions/open
         */
        /// <summary>
        /// Открывает сессию для входа пользователя по паре логин/пароль. Возвращает строку, содержащую токен сессии,
        /// который должен передаваться во все другие запросы к веб-сервисам.
        /// </summary>
        /// <param name="parameters">Параметры входа.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Строка, содержащая токен сессии.</returns>
        // POST service/login
        [HttpPost("login"), TypedJsonBody(ConvertPascalCasing = true)]
        [Consumes(MediaTypeNames.Application.Json), Produces(MediaTypeNames.Text.Xml, MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<string>> PostLogin(
            [FromBody] IntegrationLoginParameters parameters,
            CancellationToken cancellationToken = default)
        {
            var result = await this.sessionServer
                .OpenSessionAsync(
                    NotEmptyOrThrow(parameters.Login),
                    parameters.Password,
                    applicationID: ApplicationIdentifiers.Other,
                    consumeClientLicense: true,
                    twoFactorAuthSupport: false,
                    parameters: SessionClientParameters.CreateCurrent(),
                    loginMethod: "$ActionHistory_Sessions_LoginMethod_LoginAndPassword",
                    cancellationToken: cancellationToken);

            var authToken = result.Token.SerializeToXml(SessionSerializationOptions.Auth);

            // сервер сообщает версию и токен только после того, как сессия была успешно открыта, т.е. это пользователь системы, а не случайный человек
            this.Response.Headers[SessionHttpRequestHeader.Version] = BuildInfo.VersionObjectString;
            this.Response.Headers[SessionHttpRequestHeader.Session] = authToken;

            return this.Ok(authToken);
        }

        /*
         * Метод для входа, используя windows-аутентификацию. Здесь используется клиентская информация по умолчанию и идентификатор неизвестного приложения;
         * обычно вместо этого задают конкретное приложение и передают параметры.
         *
         * Метод возвращает строку с токеном, которую надо передавать в следующие методы
         * или же использоваться API TESSA для проброса токена в HTTP-заголовок "Tessa-Session".
         *
         * Рекомендуется использовать метод открытия сессии из REST API, если это возможно: /api/v2/sessions/open
         */
        /// <summary>
        /// Открывает сессию для входа пользователя, используя windows-аутентификацию. Возвращает строку, содержащую токен сессии,
        /// который должен передаваться во все другие запросы к веб-сервисам.
        /// </summary>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Строка, содержащая токен сессии.</returns>
        // POST service/winlogin
        [HttpPost("winlogin"), Produces(MediaTypeNames.Text.Xml, MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<string>> PostWinLogin(CancellationToken cancellationToken = default)
        {
            IIdentity? identity = this.HttpContext.User.Identity;
            if (identity is null)
            {
                if (this.options.Value.KerberosIsEnabled)
                {
                    this.Response.Headers.Append("WWW-Authenticate", new[] { "Negotiate" });
                }

                throw new SessionException(
                    SessionExceptionCode.WindowsAuthFailed,
                    "Windows authentication failed to link with user account.");
            }

            string? accountName;
            if (!identity.IsAuthenticated || string.IsNullOrEmpty(accountName = identity.Name))
            {
                if (this.options.Value.KerberosIsEnabled)
                {
                    this.Response.Headers.Append("WWW-Authenticate", new[] { "Negotiate" });
                }

                throw new SessionException(
                    SessionExceptionCode.ExpectedWindowsAuth,
                    "Use Windows authentication instead of anonymous.");
            }

            var result = await this.sessionServer
                .OpenSessionAsync(
                    accountName,
                    applicationID: ApplicationIdentifiers.Other,
                    consumeClientLicense: true,
                    twoFactorAuthSupport: false,
                    parameters: WebClientHelper.GetSessionClientParameters(this.HttpContext),
                    expectedLoginTypes: [UserLoginTypes.Windows],
                    loginMethod: "$ActionHistory_Sessions_LoginMethod_KerberosNtlm",
                    info: new() { [RuntimeHelper.SkipWindowsLoginValidationKey] = BooleanBoxes.True },
                    cancellationToken: cancellationToken);

            var authToken = result.Token.SerializeToXml(SessionSerializationOptions.Auth);

            // сервер сообщает версию и токен только после того, как сессия была успешно открыта, т.е. это пользователь системы, а не случайный человек
            this.Response.Headers[SessionHttpRequestHeader.Version] = BuildInfo.VersionObjectString;
            this.Response.Headers[SessionHttpRequestHeader.Session] = authToken;

            return this.Ok(authToken);
        }

        /*
         * Метод для закрытия сессии. Токен может содержаться как в HTTP-заголовке "Tessa-Session",
         * так и в параметре адресной строки <c>?token=...</c>.
         */
        /// <summary>
        /// Закрывает сессию с указанием строки с токеном сессии. Токен возвращается методом открытия сессии <see cref="PostLogin"/>.
        /// Методу не требуется наличие информации по сессии в HTTP-заголовке, если указан токен <paramref name="token"/>.
        /// </summary>
        /// <param name="token">Токен закрываемой сессии.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Асинхронная задача.</returns>
        // POST service/logout?token=...
        [HttpPost("logout"), SessionMethod(UserAccessLevel.Administrator)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> PostLogout(
            [FromQuery, SessionToken] string? token = null,
            CancellationToken cancellationToken = default)
        {
            await this.sessionService.CloseSessionWithTokenAsync(token, cancellationToken);
            return this.NoContent();
        }

        /*
         * Атрибут [SessionMethod] нужен для того, чтобы выполнять действия в пределах сессии.
         * Если в атрибуте указать UserAccessLevel.Administrator, то метод сможет вызвать только администратор Tessa.
         *
         * Если убрать атрибут, то любая внешняя система может вызвать метод от любого пользователя,
         * а также не будет доступна информация по текущей сессии. См. метод GetDataWithoutCheckingToken.
         *
         * В необязательном параметре "token" содержится строка с сериализованным токеном сессии.
         * Это задаётся атрибутом [SessionToken]. Если строка пустая, то используется HTTP-заголовок "Tessa-Session".
         *
         * Здесь и ниже указываем необходимость сессии администратора UserAccessLevel.Administrator,
         * чтобы обычные пользователи не могли получить к нему доступ. В реальных сценариях к серверу могут
         * подключаться различные клиенты, которые могут быть не объявлены администраторами системы.
         */
        /// <summary>
        /// Выполняет некоторый запрос для заданного параметра и возвращает результат.
        /// Требует наличия токена сессии в HTTP-заголовке <c>Tessa-Session</c> или в параметре <paramref name="token"/>.
        /// Это метод для тестирования возможностей REST веб-сервиса.
        /// </summary>
        /// <param name="parameter">Параметр запроса.</param>
        /// <param name="token">Токен текущей сессии.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Результат запроса.</returns>
        /// <remarks>
        /// Информация по HTTP-заголовкам, используемым платформой, доступна в методе <see cref="SessionHttpRequestHeader"/>.
        /// </remarks>
        // GET service/data?p=...&token=...
        [HttpGet("data"), SessionMethod(UserAccessLevel.Administrator), Produces(MediaTypeNames.Text.Plain, MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<string>> GetData(
            [FromQuery(Name = "p")] string parameter,
            [FromQuery, SessionToken] string? token = null,
            CancellationToken cancellationToken = default)
        {
            // максимум первые десять символов
            string data = string.IsNullOrEmpty(parameter)
                ? parameter
                : parameter[..Math.Min(10, parameter.Length)];

            await using (this.dbScope.Create())
            {
                DbManager db = this.dbScope.Db;

                // по умолчанию возвращается this.Ok(...)
                return await db
                    .SetCommand(
                        // запрос "SELECT @Data"
                        this.dbScope.BuilderFactory
                            .Select().P("Data")
                            .Build(),
                        db.Parameter("Data", data))
                    .LogCommand()
                    .ExecuteAsync<string>(cancellationToken) ?? string.Empty;
            }
        }

        /*
         * Атрибут [ApiAccessTokenMethod] нужен для того, чтобы выполнять действия с использованием токена доступа к API.
         * Если в атрибуте указать области действия, то метод будет доступен только для тех токенов, которые поддерживают эти области действия.
         *
         * Если убрать атрибут, то любая внешняя система может вызвать метод от любого пользователя,
         * а также не будет доступна информация по текущей сессии. См. метод GetDataWithoutCheckingToken.
         *
         * В необязательном параметре "token" содержится строка с токеном доступа к API.
         * Это задаётся атрибутом [ApiAccessToken]. Если строка пустая, то используется HTTP-заголовок "Authorization".
         *
         * Здесь и ниже указываем необходимость области действия "api-read service/api-data",
         * чтобы с помощью токенов с другим скоупом невозможно было получить доступ к методу.
         */
        /// <summary>
        /// Выполняет некоторый запрос для заданного параметра и возвращает результат.
        /// Требует наличия токена доступа к API в HTTP-заголовке <c>Authorization</c> или в параметре <paramref name="token"/>.
        /// Это метод для тестирования возможностей REST веб-сервиса.
        /// </summary>
        /// <param name="parameter">Параметр запроса.</param>
        /// <param name="token">Токен доступа к API.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Результат запроса.</returns>
        // GET service/data-with-api-token?p=...&token=...
        [HttpGet("data-with-api-token"), ApiAccessTokenMethod(additionalScopes: "service/api-data")]
        [Produces(MediaTypeNames.Text.Plain, MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public Task<ActionResult<string>> GetDataWithApiToken(
            [FromQuery(Name = "p")] string parameter,
            [FromQuery, ApiAccessToken] string? token = null,
            CancellationToken cancellationToken = default) =>
            this.GetData(parameter, cancellationToken: cancellationToken);
        /*
         * Метод не использует сессию через атрибут [SessionMethod], а также не использует токены доступа к API через атрибут [ApiAccessTokenMethod].
         * Все действия в мтоде выполняются от пользователя "System".
         *
         * Для таких методов крайне рекомендуется реализовать другой механизм аутентификации или ограничить сетевую доступность метода.
         */
        /// <summary>
        /// Выполняет некоторый запрос для заданного параметра и возвращает результат.
        /// Это метод для тестирования возможностей REST веб-сервиса. Метод не требует наличия сессии и токена доступа к API.
        /// </summary>
        /// <param name="parameter">Параметр запроса.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Результат запроса.</returns>
        // GET service/data-without-login
        [HttpGet("data-without-login"), Produces(MediaTypeNames.Text.Plain, MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<string>> GetDataWithoutCheckingToken(
            [FromQuery(Name = "p")] string parameter,
            CancellationToken cancellationToken = default)
        {
            await using (SessionContext.Create(Session.CreateSystemToken(this.serverSettings)))
            {
                // код внутри using будет выполняться от имени пользователя System, например:
                //return await this.GetData(parameter, null, cancellationToken);

                // но по умолчанию вернём захардкоженную строку, чтобы нельзя было за-DoS-ить сервер запросами,
                // каждый из которых обращается в базу
                return "Not supported";
            }
        }

        /*
         * Метод для загрузки карточки, используя сериализацию типизированного JSON.
         * Метод нельзя сделать GET, поскольку он содержит тело запроса, поэтому это POST.
         *
         * ConvertPascalCasing не используется, поскольку в request.Info могут быть ключи,
         * первые буквы которых должны остаться строчными.
         *
         * И сериализация, и передача сессии обычно выполняются средствами API.
         */
        /// <summary>
        /// Загружает карточку по заданному запросу для клиента.
        /// Метод идентичен типовому методу загрузки карточки в контроллере <c>CardsController</c>.
        /// Это метод для тестирования возможностей REST веб-сервиса. Метод требует наличия сессии.
        /// </summary>
        /// <param name="request">Запрос на загрузку карточки.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Ответ на запрос на загрузку карточки.</returns>
        // POST service/cards/get
        [HttpPost("cards/get"), SessionMethod(UserAccessLevel.Administrator), TypedJsonBody]
        [Consumes(MediaTypeNames.Application.Json), Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<CardGetResponse>> PostGetCard(
            [FromBody] CardGetRequest request,
            CancellationToken cancellationToken = default)
        {
            // если не установить ServiceType, то запрос выполнится с пропуском ряда проверок на валидность запроса и прав пользователя
            request.ServiceType = CardServiceType.Client;

            var response = await this.cardRepository.GetAsync(request, cancellationToken);

            // для того, чтобы ответ на запрос содержал информацию по типам данных, надо вызвать метод-расширение TypedJsonAsync
            return await this.TypedJsonAsync(response, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Открывает карточку и возвращает JSON с типизированной структурой объекта <see cref="CardGetResponse"/>.
        /// Токен сессии передаётся в HTTP-заголовке "Tessa-Session".
        /// </summary>
        /// <param name="id">Идентификатор карточки <see cref="Guid"/>. Передаётся в адресной строке.</param>
        /// <param name="type">Алиас типа карточки. Передаётся как параметр в адресной строке. Необязательный параметр.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Ответ на запрос на загрузку карточки, сериализованный как типизированный JSON.</returns>
        // GET service/cards/{id}?type=...
        [HttpGet("cards/{id:guid}"), SessionMethod(UserAccessLevel.Administrator), Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<CardGetResponse>> GetCard(
            [FromRoute] Guid id,
            [FromQuery] string? type = null,
            CancellationToken cancellationToken = default)
        {
            // если не установить ServiceType, то запрос выполнится с пропуском ряда проверок на валидность запроса и прав пользователя
            var request = new CardGetRequest { CardID = id, CardTypeName = type, ServiceType = CardServiceType.Client };

            var response = await this.cardRepository.GetAsync(request, cancellationToken);

            // для того, чтобы ответ на запрос содержал информацию по типам данных, надо вызвать метод-расширение TypedJsonAsync
            return await this.TypedJsonAsync(response, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Возвращает <c>ValidationResult</c> с <c>ValidationKeys.OperationIsUnavailable</c>
        /// и кодом <c>HttpStatusCode.Forbidden</c>  - 403.
        /// </summary>
        /// <returns>Ошибка.</returns>
        // GET service/validation-result-error
        [HttpGet("validation-result-error")]
        public void GetValidationResultError()
        {
            ValidationResult result = ValidationSequence.Begin()
                .SetObjectName(this)
                .Error(ValidationKeys.OperationIsUnavailable, OperationTypes.Other)
                .End().Build();
            throw new ValidationException(result) { StatusCode = HttpStatusCode.Forbidden };
        }

        #endregion
    }
}
