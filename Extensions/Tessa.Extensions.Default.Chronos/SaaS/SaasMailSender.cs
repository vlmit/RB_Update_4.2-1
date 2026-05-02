using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Chronos.Plugins;
using NLog;
using Tessa.Discovery;
using Tessa.Discovery.Senders;
using Tessa.Extensions.Default.Server.Plugins.Notices;
using Tessa.Extensions.Default.Server.SaaS;
using Tessa.Platform;
using Tessa.Platform.Configuration;
using Tessa.Platform.Data;
using Tessa.Platform.Plugins;
using Tessa.Platform.Runtime;
using Tessa.Platform.Storage;
using Tessa.Platform.Web;
using Tessa.SaaS;
using Unity;
using Unity.Injection;
using Unity.Lifetime;
using Unity.Resolution;

namespace Tessa.Extensions.Default.Chronos.SaaS
{
    [Plugin(
        Name = "Plugin for sending notice emails in SaaS cluster.",
        Description = "Sending notice emails in SaaS cluster.",
        Version = 1,
        JsonName = PluginName)]
    public sealed class SaasMailSender :
        Plugin
    {
        #region Constants

        private const string PluginName = nameof(SaasMailSender);

        #endregion

        #region Fields

        private readonly AsyncLock asyncLock = new();

        private readonly ISessionToken?[] tokenClosure = [null];

        private volatile PluginExtensionContext? context;

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async Task EntryPointAsync(CancellationToken cancellationToken = default)
        {
            logger.Log(LogLevel.Trace, "{0}: started", PluginName);

            try
            {
                await using var companion = new UnityContainerCompanion { UseConfiguration = true };
                await companion.ProcessAsync(
                    (c, ct) => this.ConfigureContainerAsync(c.Container, ct),
                    async (c, ct) =>
                    {
                        PluginExtensionContext? context;
                        using (await this.asyncLock.EnterAsync(ct))
                        {
                            if (this.StopRequested)
                            {
                                return;
                            }

                            context = this.CreateInstanceExtensionContext(c.Container, ct);
                            this.context = context;
                        }

                        if (this.StopRequested)
                        {
                            return;
                        }

                        try
                        {
                            var handlerResolver = context.Resolve<IPluginHandlerResolver>();
                            if (handlerResolver.TryResolve(PluginName) is not { } handler)
                            {
                                logger.Error($"No handler found for {PluginName}.");
                                return;
                            }

                            var settingsProvider = context.Resolve<IPluginSettingsProvider>();
                            var settings = settingsProvider.TryGetPluginSettings(PluginName);
                            if (settings is null)
                            {
                                logger.Error($"No handler settings found for {PluginName}.");
                                return;
                            }

                            if (settings.Enabled)
                            {
                                await handler.ExecuteAsync(
                                    new PluginExecutingContext(settings, context.ValidationResult)
                                    {
                                        CancellationToken = context.CancellationToken,
                                    });
                            }
                        }
                        catch (ThreadAbortException)
                        {
                            // завершение потока было инициировано методом Stop() плагина или хостом из другого потока;
                            // если это был хост, а метод Stop() уже начал выполняться и использовал token.WaitUntilEntryPointFinishedAsync(),
                            // то событие окончания метода EntryPointAsync() следует инициировать, чтобы метод StopAsync() вышел из состояния ожидания
                        }
                        catch (OperationCanceledException)
                        {
                            // выбросим наружу, там будет трейс о том, что плагин прерван, но всё нормально
                            throw;
                        }
                        catch (Exception ex)
                        {
                            // все прочие исключения логируются как обычно
                            logger.LogException(ex);
                        }
                    },
                    cancellationToken);
            }
            finally
            {
                logger.Trace(
                    this.context is { StopRequested: true }
                        ? "{0}: interrupted"
                        : "{0}: completed",
                    PluginName);
            }
        }

        /// <inheritdoc/>
        public override async Task StopAsync(IPluginStopToken token)
        {
            try
            {
                using (await this.asyncLock.EnterAsync())
                {
                    this.StopRequested = true;

                    PluginExtensionContext? context = this.context;
                    if (context is not null)
                    {
                        context.StopRequested = true;
                    }

                    await token.CancellationTokenSource.CancelAsync();
                }

                await token.WaitUntilEntryPointFinishedAsync(token.CancellationTokenSource.Token);
            }
            finally
            {
                this.asyncLock.Dispose();
            }
        }

        #endregion

        #region Private Methods

        private async ValueTask ConfigureContainerAsync(
            IUnityContainer container,
            CancellationToken cancellationToken = default)
        {
            container
                .RegisterPlatformSharedDependencies()
                .RegisterSaasSettings(server: false)
                .RegisterSaasServices()
                .RegisterDbManager(static c =>
                    c.Resolve<IConfigurationManager>().Configuration.CreateDbManager(
                        NotWhiteSpaceOrThrow(c.Resolve<ISaasSettings>().MailDb)))
                .RegisterDbScope()
                .RegisterType<IOutboxManager, SaasOutboxManager>(
                    new PerResolveLifetimeManager())
                .RegisterDiscoveryCommon()
                .RegisterWeb()
                .RegisterWebDefaultHandlers()
                .RegisterApplicationServerSettingsFromConfig()
                .RegisterFactory<ISession>(
                    c => new Session(
                        SessionType.Server,
                        () => this.tokenClosure[0] ??= Session.CreateSystemToken(c.Resolve<ITessaServerSettings>()),
                        x => c.Resolve<ITessaServerSettings>().ServerCode),
                    new ContainerControlledLifetimeManager())
                // mail sender plugin handler and it's deps
                .RegisterSingleton<MailSenderPluginHandler>()
                .RegisterSingleton<MailSenderConfig>()
                .RegisterType<ExchangeSender>(new PerResolveLifetimeManager())
                .RegisterType<SmtpSender>(new PerResolveLifetimeManager())
                .RegisterSingleton<IPluginSettingsProvider, PluginSettingsProvider>()
                .RegisterSingleton<IPluginHandlerResolver, PluginHandlerResolver>()
                ;

            container
                .Resolve<IPluginHandlerResolver>()
                .Register<MailSenderPluginHandler>(PluginName);

            var key = await LoadDiscoveryKeyAsync(container, cancellationToken);
            var instances = await container.Resolve<IClusterInformationService>().GetInstancesInfoAsync(cancellationToken);

            container
                // fix the instances for stable access to named registrations
                .RegisterFactory<IClusterInformationService>(
                    _ => new FakeClusterInformationService(instances),
                    new ContainerControlledLifetimeManager())
                .RegisterType<IDiscoveryKeyProvider, SaasDiscoveryKeyProvider>(
                    new ContainerControlledLifetimeManager(),
                    new InjectionConstructor(new InjectionParameter(key)))
                .RegisterFactory<ISaasMailFromAddressProvider>(
                    _ => new SaasMailFromAddressProvider(instances),
                    new ContainerControlledLifetimeManager())
                // resolution factories
                .RegisterFactory<Func<string?, IMailFileLoaderService>>(
                    c => new Func<string?, IMailFileLoaderService>(
                        name => c.Resolve<IMailFileLoaderService>(name)),
                    new ContainerControlledLifetimeManager())
                .RegisterFactory<Func<string?, IMailSentNotificationService>>(
                    c => new Func<string?, IMailSentNotificationService>(
                        name => c.Resolve<IMailSentNotificationService>(name)),
                    new ContainerControlledLifetimeManager())
                ;

            // named registrations
            foreach (var info in instances)
            {
                var instanceName = info.Instance;
                container
                    .RegisterFactory<IConnectionSettings>(
                        instanceName,
                        c => ConnectionSettings.ParseFromConfigurationSettings(
                            c.Resolve<IConfigurationManager>().Configuration.Settings,
                            info.Endpoint),
                        new ContainerControlledLifetimeManager())
                    .RegisterFactory<IWebProxyFactory>(
                        instanceName,
                        static (c, _, name) =>
                            c.Resolve<WebProxyFactory>(
                                new DependencyOverride<IConnectionSettings>(c.Resolve<IConnectionSettings>(name))),
                        new ContainerControlledLifetimeManager())
                    .RegisterFactory<IMailFileLoaderService>(
                        instanceName,
                        static (c, t, name) =>
                            c.Resolve<SaasMailFileLoaderService>(
                                new DependencyOverride<IWebProxyFactory>(c.Resolve<IWebProxyFactory>(name))),
                        new ContainerControlledLifetimeManager())
                    .RegisterFactory<IMailSentNotificationService>(
                        instanceName,
                        static (c, t, name) =>
                            c.Resolve<SaasMailSentNotificationService>(
                                new DependencyOverride<IWebProxyFactory>(c.Resolve<IWebProxyFactory>(name))),
                        new ContainerControlledLifetimeManager())
                    ;
            }
        }

        private static async Task<DiscoveryKey> LoadDiscoveryKeyAsync(IUnityContainer container, CancellationToken cancellationToken)
        {
            var configurationManager = container.Resolve<IConfigurationManager>();
            var saasSection = NotNullOrThrow(SaasHelper.TryGetSaasSection(configurationManager.Configuration.Settings));
            var keyPath = NotEmptyOrThrow(saasSection.TryGet<string>(SaasHelper.KeyPathAttributeName)?.Trim());
            var keyPassword = NotEmptyOrThrow(saasSection.TryGet<string>(SaasHelper.KeyPasswordAttributeName)?.Trim());

            var keySerializer = container.Resolve<IDiscoveryKeySerializer>();
            var key = await DiscoverySenderHelper.LoadKeyAsync(keySerializer, keyPath, keyPassword, cancellationToken);
            if (key.ExpiredAt < DateTime.UtcNow)
            {
                throw new InvalidOperationException("Expired key");
            }

            // check needed scopes
            if (key.Scopes?.Contains(DiscoveryScopes.SaasFiles, StringComparer.OrdinalIgnoreCase) is not true ||
                key.Scopes.Contains(DiscoveryScopes.SaasMail, StringComparer.OrdinalIgnoreCase) is not true)
            {
                throw new InvalidOperationException("Invalid key");
            }

            return key;
        }

        private PluginExtensionContext CreateInstanceExtensionContext(
            IUnityContainer container,
            CancellationToken cancellationToken)
        {
            Action<ISessionToken> setTokenAction = token => this.tokenClosure[0] = token;
            return new PluginExtensionContext(SaasHelper.SaaS, container, setTokenAction, cancellationToken);
        }

        #endregion
    }
}
