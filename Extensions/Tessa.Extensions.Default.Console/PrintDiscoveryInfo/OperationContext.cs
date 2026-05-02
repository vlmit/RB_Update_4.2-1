using System.IO;

namespace Tessa.Extensions.Default.Console.PrintDiscoveryInfo
{
    public sealed class OperationContext
    {
        #region Constructors

        public OperationContext(TextWriter stdOut) =>
            this.StdOut = NotNullOrThrow(stdOut);

        #endregion

        #region Properties

        public TextWriter StdOut { get; }

        public bool Export { get; init; }

        public bool PrintInfo { get; init; }

        #endregion
    }
}
