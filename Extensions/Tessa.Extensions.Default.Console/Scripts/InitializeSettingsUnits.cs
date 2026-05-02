using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Platform.ConsoleApps;
using Unity;

namespace Tessa.Extensions.Default.Console.Scripts
{
    [ConsoleScript]
    public sealed class InitializeSettingsUnits : ClientConsoleScriptBase
    {
        /// <inheritdoc/>
        protected override async ValueTask ExecuteCoreAsync(CancellationToken cancellationToken)
        {
            await this.Logger.InfoAsync("Initializing settings units...");

            var cardRepository = this.Container.Resolve<ICardRepository>();
            var response = await cardRepository.RequestAsync(new() { RequestType = CardRequestTypes.InitializeSettingsUnits }, cancellationToken);
            var result = response.ValidationResult.Build();

            if (result.IsSuccessful)
            {
                await this.Logger.InfoAsync("Settings units are initialized successfully.");
            }
            else
            {
                await this.Logger.ErrorAsync("Failed to initialize settings units.");
                this.Result = -1;
            }

            await this.Logger.LogResultAsync(result);
        }

        /// <inheritdoc/>
        protected override async ValueTask ShowHelpCoreAsync(CancellationToken cancellationToken)
        {
            await this.Logger.WriteLineAsync("Initializes settings units with updating their payload and system metadata.");
            await this.Logger.WriteLineAsync("Script requires to login as user with system administrative privileges.");
            await this.Logger.WriteLineAsync();
            await this.Logger.WriteLineAsync("Notice: settings units initialization is performed during \"web\" service startup.");
            await this.Logger.WriteLineAsync("Use this script if system data for settings units were changed in the database with direct SQL queries,");
            await this.Logger.WriteLineAsync("and settings units initialization should be enforced.");
            await this.Logger.WriteLineAsync();
            await this.Logger.WriteLineAsync("Example:");
            await this.Logger.WriteLineAsync(
                $"{Assembly.GetEntryAssembly()?.GetName().Name} Script {nameof(InitializeSettingsUnits)} -a:https://localhost -u:admin -p:admin");
        }
    }
}
