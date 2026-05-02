using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Platform.ConsoleApps;
using Unity;

namespace Tessa.Extensions.Default.Console.Scripts
{
    [ConsoleScript]
    public sealed class InitializeGroups : ClientConsoleScriptBase
    {
        protected override async ValueTask ExecuteCoreAsync(CancellationToken cancellationToken)
        {
            await this.Logger.InfoAsync("Initializing groups...");

            var cardRepository = this.Container.Resolve<ICardRepository>();
            var response = await cardRepository.RequestAsync(new() { RequestType = CardRequestTypes.InitializeGroups }, cancellationToken);
            var result = response.ValidationResult.Build();

            if (result.IsSuccessful)
            {
                await this.Logger.InfoAsync("Groups are initialized successfully.");
            }
            else
            {
                await this.Logger.ErrorAsync("Failed to initialize groups.");
                this.Result = -1;
            }

            await this.Logger.LogResultAsync(result);
        }

        protected override async ValueTask ShowHelpCoreAsync(CancellationToken cancellationToken)
        {
            await this.Logger.WriteLineAsync("Initializes groups with updating their settings, including system members and admins.");
            await this.Logger.WriteLineAsync("Script requires to login as user with system administrative privileges.");
            await this.Logger.WriteLineAsync();
            await this.Logger.WriteLineAsync("Notice: groups initialization is performed during \"web\" service startup.");
            await this.Logger.WriteLineAsync("Use this script if system settings for groups were changed in the database with direct SQL queries,");
            await this.Logger.WriteLineAsync("and groups initialization should be enforced.");
            await this.Logger.WriteLineAsync();
            await this.Logger.WriteLineAsync("Example:");
            await this.Logger.WriteLineAsync(
                $"{Assembly.GetEntryAssembly()?.GetName().Name} Script {nameof(InitializeGroups)} -a:https://localhost -u:admin -p:admin");
        }
    }
}
