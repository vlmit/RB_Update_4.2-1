using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Platform.ConsoleApps;
using Unity;

namespace Tessa.Extensions.Default.Console.Scripts
{
    [ConsoleScript]
    public sealed class NotifyInstallationFinished : ClientConsoleScriptBase
    {
        protected override async ValueTask ExecuteCoreAsync(CancellationToken cancellationToken)
        {
            await this.Logger.InfoAsync($"Sending request {nameof(CardRequestTypes.NotifyInstallationFinished)}...");

            var cardRepository = this.Container.Resolve<ICardRepository>();
            var response = await cardRepository.RequestAsync(new() { RequestType = CardRequestTypes.NotifyInstallationFinished }, cancellationToken);
            var result = response.ValidationResult.Build();

            if (result.IsSuccessful)
            {
                await this.Logger.InfoAsync("Request is processed successfully.");
            }
            else
            {
                await this.Logger.ErrorAsync("Failed to process the request.");
                this.Result = -1;
            }

            await this.Logger.LogResultAsync(result);
        }

        protected override async ValueTask ShowHelpCoreAsync(CancellationToken cancellationToken)
        {
            await this.Logger.WriteLineAsync("Notifies system that installation process is finished.");
            await this.Logger.WriteLineAsync("Usually, this script is called at the end of the installation scripts \"setup\" and \"upgrade\".");
            await this.Logger.WriteLineAsync("Script requires to login as user with system administrative privileges.");
            await this.Logger.WriteLineAsync();
            await this.Logger.WriteLineAsync("Example:");
            await this.Logger.WriteLineAsync(
                $"{Assembly.GetEntryAssembly()?.GetName().Name} Script {nameof(NotifyInstallationFinished)} -a:https://localhost -u:admin -p:admin");
        }
    }
}
