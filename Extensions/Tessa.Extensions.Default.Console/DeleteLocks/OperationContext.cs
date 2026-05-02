namespace Tessa.Extensions.Default.Console.DeleteLocks
{
    public class OperationContext
    {
        public string[]? LockGroups { get; init; }

        public string[]? LockIDs { get; init; }

        public string? KeyPath { get; init; }

        public string? KeyPassword { get; init; }
    }
}
