using System.Collections.Generic;
using System.Threading.Tasks;
using LinqToDB;
using NLog;
using Tessa.Platform.Data;
using Tessa.Platform.Plugins;
using Tessa.Scheme;

namespace Tessa.Extensions.Server.Plugins
{
    public sealed class ExamplePluginHandler : IPluginHandler
    {
        #region Fields

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public const string PluginName = "ExamplePlugin";

        private readonly IDbScope dbScope;

        #endregion

        #region Constructors

        /// <summary>
        /// Создаёт экземпляр класса с указанием его зависимостей.
        /// </summary>
        /// <param name="dbScope"><inheritdoc cref="IDbScope" path="/summary"/></param>
        public ExamplePluginHandler(IDbScope dbScope)
        {
            this.dbScope = NotNullOrThrow(dbScope);
        }

        #endregion

        #region IPluginHandler Implementation

        /// <inheritdoc/>
        public async ValueTask ExecuteAsync(IPluginExecutingContext context)
        {
            // любая полезная работа плагина может быть здесь
            logger.Trace("Doing useful stuff here");

            ThrowIfTypeIsNot<ExamplePluginSettings>(context.Settings);
            var exampleSettings = (ExamplePluginSettings) context.Settings;

            await using (this.dbScope.Create())
            {
                // работа в пределах одного SQL-соединения, транзакция при этом явно не создаётся

                if (context.StopRequested)
                {
                    // была запрошена асинхронная остановка, можно периодически проверять значение этого свойства,
                    // и консистентно завершать выполнение (закрыть транзакцию, если была открыта, и др.)
                    return;
                }

                var db = this.dbScope.Db;
                var builder = this.dbScope.BuilderFactory;

                var cardExists = await db
                    .SetCommand(
                        builder
                            .Select().V(true)
                            .From(Names.Instances).NoLock()
                            .Where().C(Names.Instances_ID).Equals().P(Names.Instances_ID)
                            .Build(),
                        db.Parameter(Names.Instances_ID, exampleSettings.CardID, DataType.Guid))
                    .LogCommand()
                    .ExecuteAsync<bool>(context.CancellationToken);

                logger.Trace(cardExists
                    ? $"Card with ID {exampleSettings.CardID} exists in db"
                    : $"Card with ID {exampleSettings.CardID} doesn't exist in db");
            }
        }

        /// <inheritdoc/>
        public IPluginSettings ResolveSettings(Dictionary<string, object?>? info = null)
        {
            var settings = new ExamplePluginSettings(PluginName);
            if (info is not null)
            {
                settings.Deserialize(info);
            }

            return settings;
        }

        #endregion
    }
}
