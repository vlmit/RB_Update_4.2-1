using System.Diagnostics.CodeAnalysis;

namespace Tessa.Extensions.Default.Console.ConvertConfiguration
{
    public class OperationContext
    {
        [DisallowNull]
        public string? Source { get; set; }

        [DisallowNull]
        public string? Target { get; set; }

        public string? SchemePath { get; set; }

        public bool DoNotDelete { get; set; }

        public ConversionMode ConversionMode { get; set; }
    }
}
