using System;
using System.Collections.Generic;
using Tessa.Cards.Repair;

namespace Tessa.Extensions.Default.Console.RepairTypes
{
    public class OperationContext
    {
        public IList<Guid>? CardTypeIDs { get; init; }

        public TypeRepairLevel RepairLevel { get; init; }
    }
}
