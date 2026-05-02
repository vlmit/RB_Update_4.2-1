using System.Threading;
using System.Threading.Tasks;
using Chronos.Plugins;
using NLog;
using Tessa.Extensions.Server.Plugins;
using Tessa.Platform;
using Tessa.Platform.Plugins;
using Unity;

namespace Tessa.Extensions.Chronos.Plugins
{
    /// <summary>
    /// Пример плагина, который может работать через серверное API.
    /// </summary>
    [Plugin(
        Name = "Example plugin",
        Description = "Plugin is used as an example of how Chronos plugins should be created"
            + " to communicate with database as server.",
        Version = 1,
        JsonName = ExamplePluginHandler.PluginName)]
    public sealed class ExamplePlugin :
        Plugin
    {
        #region Fields

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        #endregion

        #region Base Overrides

        public override async Task EntryPointAsync(CancellationToken cancellationToken = default)
        {
            logger.Trace("Starting plugin");

            await using var companion = new UnityContainerCompanion { UseConfiguration = true };
            await companion.ProcessAsync(
                static async (c, ct) =>
                {
                    // конфигурируем контейнер Unity для использования стандартных серверных API (в т.ч. API карточек)
                    // а также для получения прямого доступа к базе данных через IDbScope по строке подключения из app.json;
                    // предполагаем, что все действия, совершаемые плагином, будут выполняться от имени пользователя System

                    await c.Container
                        // дополнительные регистрации в контейнере Unity могут быть здесь:
                        // .RegisterSingleton<MyService>()
                        .RegisterServerForPluginAsync(cancellationToken: ct);
                },
                async (c, ct) =>
                {
                    var pluginSettingsProvider = c.Container.Resolve<IPluginSettingsProvider>();
                    var pluginHandlerResolver = c.Container.Resolve<IPluginHandlerResolver>();

                    // Пытаемся получить обработчик плагина из зависимостей контейнера.
                    if (pluginHandlerResolver.TryResolve(ExamplePluginHandler.PluginName) is not { } handler)
                    {
                        logger.Error($"No handler found for plugin {ExamplePluginHandler.PluginName}.");
                        return;
                    }

                    // Загружаем настройки плагина из app.json или получаем настройки по умолчанию из обработчика, если в app.json не заданы настройки нашего плагина.
                    var settings = pluginSettingsProvider.TryGetPluginSettings(ExamplePluginHandler.PluginName)
                        ?? handler.ResolveSettings();

                    var context = new PluginExecutingContext(settings)
                    {
                        StopRequestedToken = this.StopRequestedToken,
                        CancellationToken = ct
                    };

                    await handler.ExecuteAsync(context);
                },
                cancellationToken);

            logger.Trace("Shutting down");
        }

        #endregion
    }
}
