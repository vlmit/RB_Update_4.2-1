using System.Collections.Generic;

namespace Tessa.Extensions.Default.Console.ExportSettings
{
    public sealed record class OperationContext(
        IReadOnlyCollection<string>? UnitNames,
        string? OutputFolder,
        bool ClearOutputFolder);
}
