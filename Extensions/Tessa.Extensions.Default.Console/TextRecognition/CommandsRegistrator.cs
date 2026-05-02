using Tessa.Platform.CommandLine;
using Tessa.Platform.ConsoleApps;

namespace Tessa.Extensions.Default.Console.TextRecognition
{
    [ConsoleRegistrator]
    public sealed class CommandsRegistrator : ConsoleRegistratorBase
    {
        public override void RegisterCommands() => this.CommandContext!.AddCommand(Sync.Command.OcrSync).AddCommand(Async.Command.OcrAsync);
    }
}
