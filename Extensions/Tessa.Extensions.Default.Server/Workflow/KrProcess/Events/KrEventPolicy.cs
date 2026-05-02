#nullable enable

using System.Collections.Generic;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Events
{
    public sealed class KrEventPolicy : IKrEventPolicy
    {
        private readonly IReadOnlyCollection<string> eventTypes;

        public KrEventPolicy(IReadOnlyCollection<string> eventTypes)
        {
            this.eventTypes = NotNullOrThrow(eventTypes);

            foreach (string eventType in eventTypes)
            {
                ThrowIfNullOrWhiteSpace(eventType);
            }
        }

        /// <inheritdoc />
        public bool IsAllowed(string? eventType)
        {
            if (this.eventTypes.Count == 0)
            {
                return true;
            }

            foreach (string allowedEventType in this.eventTypes)
            {
                if (allowedEventType == eventType)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
