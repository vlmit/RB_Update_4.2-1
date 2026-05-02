using System;
using System.Collections.Generic;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Files;
using Tessa.Platform.Conditions;

namespace Tessa.Extensions.Default.Server.Files.VirtualFiles
{
    public sealed class KrVirtualFile : IKrVirtualFile
    {
        #region Constructors

        public KrVirtualFile()
        {
        }

        #endregion

        #region IKrVirtualFile Implementation

        /// <inheritdoc />
        public Guid ID { get; set; }

        /// <inheritdoc />
        public string Name { get; set; }

        /// <inheritdoc />
        public FileCategory FileCategory { get; set; }

        /// <inheritdoc />
        public List<IKrVirtualFileVersion> Versions { get; } = new();

        /// <inheritdoc />
        public IEnumerable<ConditionSettings> Conditions { get; set; }

        /// <inheritdoc />
        public string InitializationScenario { get; set; }

        /// <inheritdoc />
        public HashSet<KrState> DocumentStates { get; } = new();

        /// <inheritdoc />
        public HashSet<Guid> Types { get; } = new();

        /// <inheritdoc />
        public HashSet<Guid> Roles { get; } = new();

        #endregion
    }
}
