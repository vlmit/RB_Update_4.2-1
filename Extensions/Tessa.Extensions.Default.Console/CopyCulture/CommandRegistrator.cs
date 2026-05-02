using Tessa.Platform.CommandLine;
using Tessa.Platform.ConsoleApps;

namespace Tessa.Extensions.Default.Console.CopyCulture
{
    [ConsoleRegistrator]
    public sealed class CommandRegistrator :
        ConsoleRegistratorBase
    {
        public override void RegisterCommands() => this.CommandContext!.AddCommand(Command.CopyCulture);
    }
}
