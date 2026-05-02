namespace Tessa.Extensions.Default.Console.PrintLocks
{
    public class OperationContext
    {
        public required string[] LockGroups { get; init; }

        public string? KeyPath { get; init; }

        public string? KeyPassword { get; init; }
    }
}
