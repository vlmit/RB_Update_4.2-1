#nullable enable
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NLog.Web;
using Tessa.Platform.Configuration;
using Tessa.Test.Default.Shared.Web;
using Tessa.Web;
using Tessa.Web.Client;
using Tessa.Web.Services;
using Unity;

namespace Tessa.Test.Default.Client.Web
{
    /// <summary>
    /// Предоставляет методы для создания тестового сервера предназначенного для тестирования web-приложения TESSA.
    /// </summary>
    public class TessaWebApplicationFactory :
        WebApplicationFactoryBase
    {
        #region Nested Types

        private sealed class TestHttpMaxRequestBodySizeFeature :
            IHttpMaxRequestBodySizeFeature
        {
            public bool IsReadOnly => false;

            public long? MaxRequestBodySize
            {
                get => null;
                set
                {
                    // ignored
                }
            }

            public static readonly IHttpMaxRequestBodySizeFeature Instance = new TestHttpMaxRequestBodySizeFeature();
        }

        private sealed class TestSetFeaturesMiddleware(RequestDelegate next)
        {
            public Task Invoke(HttpContext context)
            {
                // prevent warnings about absence of IHttpMaxRequestBodySizeFeature
                context.Features.Set(TestHttpMaxRequestBodySizeFeature.Instance);
                return next(context);
            }
        }

        #endregion

        #region Fields

        private readonly Func<WebContainerCreationOptions, object?, IWebContextAccessor, ValueTask<IUnityContainer>> createContainerFunc;

        private readonly IConfigurationManager? configurationManagerOverride;

        #endregion

        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="TessaWebApplicationFactory"/>.
        /// </summary>
        /// <param name="createContainerFunc">Метод создающий серверный контейнер для тестов.</param>
        /// <param name="configurationManagerOverride">Объект, управляющий конфигурацией приложений Tessa переопределяющий используемый по умолчанию.
        /// Если задано значение по умолчанию для типа, то используется менеджер по умолчанию.</param>
        public TessaWebApplicationFactory(
            Func<WebContainerCreationOptions, object?, IWebContextAccessor, ValueTask<IUnityContainer>> createContainerFunc,
            IConfigurationManager? configurationManagerOverride = null)
        {
            ThrowIfNull(createContainerFunc);

            this.createContainerFunc = createContainerFunc;
            this.configurationManagerOverride = configurationManagerOverride;

            // Необходимо для предотвращения копирования ответа содержащего поток SuperStream не поддерживающий получение длины потока (Stream.Length).
            this.ClientOptions.AllowAutoRedirect = false;
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder
                .UseNLog()
                .UseTessaConfiguration()
                .ConfigureServices(services =>
                {
                    services
                        .AddTessaServices()
                        .AddTessaClientServices()
                        .AddTessaResponseCompression();

                    services
                        .AddControllersWithViews()
                        .ConfigureTessaClientMvc();

                    if (this.configurationManagerOverride is not null)
                    {
                        services
                            .RemoveAll<IConfigurationManager>()
                            .AddSingleton(this.configurationManagerOverride);
                    }

                    services
                        .RemoveAll<IWebUnityFactory>()
                        .AddSingleton<IWebBackgroundServiceQueue, WebBackgroundServiceQueue>()
                        .AddSingleton<IWebPeriodicService, WebPeriodicService>()
                        .AddSingleton<IWebUnityFactory, TestWebUnityFactory>(s =>
                            new TestWebUnityFactory(this.createContainerFunc, s.GetRequiredService<IWebUnityFactoryDependencies>()))
                        .AddTessaResponseCompression();

                    services
                        .AddOptions()
                        .AddTessaHealthChecks()
                        .Configure<MvcOptions>(x => x.AddTessaFormatters())
                        .Configure<FormOptions>(x => x.SetupTessaFormOptions())
                        .ConfigureWebOptions()
                        .ConfigureWebClientOptions()
                        .ConfigureAuthWithoutSaml();
                })
                .Configure(app =>
                {
                    var services = app.ApplicationServices;

                    var forwardedHeaders = new ForwardedHeadersOptions
                    {
                        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
                        ForwardLimit = 10,
                    };

                    forwardedHeaders.KnownIPNetworks.Clear();
                    forwardedHeaders.KnownProxies.Clear();

                    var serverOptions = services.GetRequiredService<IOptions<WebServerOptions>>();
                    var options = services.GetRequiredService<IOptions<WebOptions>>();

                    app
                        .UseMiddleware<TestSetFeaturesMiddleware>()
                        .UseForwardedHeaders(forwardedHeaders)
                        .UseTessaHttpsRedirection(serverOptions.Value, environmentIsDevelopment: true)
                        .UsePathBaseIfSpecified(options.Value.PathBase)
                        .UseResponseCompression();

                    app
                        .UseTessaClientApplication()
                        .UseRouting();

                    this.ConfigureAuthentication(app);

                    app
                        .UseEndpoints(endpoints =>
                        {
                            endpoints.MapHealthChecks("/hcheck");
                            endpoints.MapControllers();
                        });

                    var applicationLifetime = services.GetRequiredService<IHostApplicationLifetime>();
                    applicationLifetime.RegisterUnityLifetime(app);

                    app.Run(context => context.HandleNotFoundAsync());
                });
        }

        /// <inheritdoc/>
        protected override async ValueTask OnBeforeHostStartedAsync(IHost host)
        {
            await base.OnBeforeHostStartedAsync(host);
            await host.Services.PrepareWebServerWithUnityAsync(initializeLocalization: false);
        }

        /// <inheritdoc/>
        protected override void ConfigureTestServer(TestServer testServer) =>
            // Аналог IISServerOptions.AllowSynchronousIO = true.
            testServer.AllowSynchronousIO = true;

        #endregion

        #region Protected Methods

        /// <summary>
        /// Выполняет настройку аутентификации и авторизации в конвейере веб-приложения.
        /// </summary>
        /// <param name="app">Объект для настройки конвейера запросов приложения.</param>
        protected virtual void ConfigureAuthentication(IApplicationBuilder app) =>
            app.UseAuthorization(); // необходимо в ASP.NET Core 3+, нельзя выключать

        #endregion
    }
}
