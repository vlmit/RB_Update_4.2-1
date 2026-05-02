using System.Collections.Generic;

namespace Tessa.Extensions.Default.Console.ResetSettings
{
    public sealed record class OperationContext(IEnumerable<string>? Names, IEnumerable<string>? Categories);
}
