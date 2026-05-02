using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Tessa.Extensions.Default.Console.ImportDiffCulture
{
    public class OperationContext
    {
        [DisallowNull]
        public string? Source { get; set; }

        [DisallowNull]
        public string? Output { get; set; }

        [DisallowNull]
        public CultureInfo? TargetCulture { get; set; }
    }
}
