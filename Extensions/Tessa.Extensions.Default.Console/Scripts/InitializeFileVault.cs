using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Platform.ConsoleApps;
using Unity;

namespace Tessa.Extensions.Default.Console.Scripts
{
    [ConsoleScript]
    public sealed class InitializeFileVault: ClientConsoleScriptBase
    {
        protected override async ValueTask ExecuteCoreAsync(CancellationToken cancellationToken)
        {
            await this.Logger.InfoAsync("Initializing file vault...");

            var cardRepository = this.Container.Resolve<ICardRepository>();
            var response = await cardRepository.RequestAsync(new() { RequestType = CardRequestTypes.InitializeFileVault }, cancellationToken);
            var result = response.ValidationResult.Build();

            if (result.IsSuccessful)
            {
                await this.Logger.InfoAsync("File vault is initialized successfully.");
            }
            else
            {
                await this.Logger.ErrorAsync("Failed to initialize file vault.");
                this.Result = -1;
            }

            await this.Logger.LogResultAsync(result);
        }

        protected override async ValueTask ShowHelpCoreAsync(CancellationToken cancellationToken)
        {
            await this.Logger.WriteLineAsync("Initializes file vault.");
            await this.Logger.WriteLineAsync("Script requires to login as user with system administrative privileges.");
            await this.Logger.WriteLineAsync();
            await this.Logger.WriteLineAsync("Notice: file vault initialization is performed during \"web\" service startup.");
            await this.Logger.WriteLineAsync("Use this script if system folders were removed in the database with direct SQL queries.");
            await this.Logger.WriteLineAsync();
            await this.Logger.WriteLineAsync("Example:");
            await this.Logger.WriteLineAsync(
                $"{Assembly.GetEntryAssembly()?.GetName().Name} Script {nameof(InitializeFileVault)} -a:https://localhost -u:admin -p:admin");
        }
    }
}
