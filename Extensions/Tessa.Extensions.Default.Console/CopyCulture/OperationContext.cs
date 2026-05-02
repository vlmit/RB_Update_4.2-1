using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Tessa.Extensions.Default.Console.CopyCulture
{
    public class OperationContext
    {
        [DisallowNull]
        public string? Source { get; set; }

        public string? Target { get; set; }

        [DisallowNull]
        public CultureInfo? FromCulture { get; set; }

        [DisallowNull]
        public CultureInfo? ToCulture { get; set; }

        public bool ForceDetached { get; set; }

        public bool EmptyOnly { get; set; }
    }
}
