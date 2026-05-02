using System.Collections.Generic;

namespace Tessa.Extensions.Default.Console.PackageMobileClientApp
{
    public sealed class MobileClientApplication 
    {
        #region Properties

        public string? Version { get; set; }

        public string? FileName { get; set; }

        public List<MobileClientApplicationFile> Files { get; } = new();

        #endregion
    }
}
