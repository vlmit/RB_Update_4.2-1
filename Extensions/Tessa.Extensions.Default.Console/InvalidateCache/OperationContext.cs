using System.Diagnostics.CodeAnalysis;

namespace Tessa.Extensions.Default.Console.InvalidateCache
{
    public class OperationContext
    {
        /// <summary>
        /// Не должно быть равно <c>null</c>. Пустой массив приводит к сбросу всех кэшей.
        /// </summary>
        [DisallowNull]
        public string[]? CacheNames { get; set; }
    }
}