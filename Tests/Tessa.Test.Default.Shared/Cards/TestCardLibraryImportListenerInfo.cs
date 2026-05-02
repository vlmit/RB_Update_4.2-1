#nullable enable
using System;
using System.Collections.Generic;

namespace Tessa.Test.Default.Shared.Cards
{
    public class TestCardLibraryImportListenerInfo
    {
        public int Skipped { get; set; }

        public int NotModified { get; set; }

        public List<string> ImportedNames { get; } = [];

        public int Errors { get; set; }

        public bool FilesForImportNotFound { get; set; }

        public List<Guid> ImportSequence { get; } = [];
    }
}
