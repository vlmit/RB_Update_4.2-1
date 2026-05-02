using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Tessa.Extensions.Default.Console.ExportDiffCulture
{
    public class OperationContext
    {
        public List<string>? Sources { get; set; }

        [DisallowNull]
        public string? Output { get; set; }

        [DisallowNull]
        public CultureInfo? TargetCulture { get; set; }

        [DisallowNull]
        public CultureInfo? BaseCulture { get; set; }
    }
}
