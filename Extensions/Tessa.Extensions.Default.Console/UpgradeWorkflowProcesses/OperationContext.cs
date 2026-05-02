using System;
using System.Collections.Generic;

namespace Tessa.Extensions.Default.Console.UpgradeWorkflowProcesses
{
    public class OperationContext
    {
        public IList<Guid>? Identifiers { get; init; }

        public Mode Mode { get; init; }
    }
}
