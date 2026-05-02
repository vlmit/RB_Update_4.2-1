using System;

namespace Tessa.Extensions.Default.Console.RecalcAcl
{
    public class OperationContext
    {
        public Guid[]? Ids { get; set; }

        public bool All { get; set; }

        public Guid[]? Cards { get; set; }
    }
}
