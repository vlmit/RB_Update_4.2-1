using System;
using Tessa.Platform.RefGroups;

namespace Tessa.Extensions.Default.Console.RecalcGroups
{
    public class OperationContext
    {
        public RefGroupsRecalcMode Mode { get; set; }

        public Guid[]? Ids { get; set; }
    }
}
