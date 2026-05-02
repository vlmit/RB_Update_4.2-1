using System;
using System.Collections.Generic;

namespace Tessa.Extensions.Default.Console.SendCommand
{
    public class OperationContext
    {
        public required string KeyPath { get; init; }
        public required string KeyPassword { get; init; }
        public required string Command { get; init; }
        public string[]? Targets { get; init; }
        public string[]? Scopes { get; init; }
        public IReadOnlyDictionary<string, string?>? Arguments { get; init; }
        public bool IsClient { get; init; }
        public bool Nowait { get; init; }
        public string? UserName { get; init; }
        public string? Password { get; init; }
        public TimeSpan Timeout { get; init; }
    }
}
