#nullable enable
using System.Collections.Generic;
using System.Linq;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Serialization
{
    public sealed class RouteCardTypeExtensionPolicy : IRouteCardTypeExtensionPolicy
    {
        private readonly IReadOnlyCollection<RouteCardType> allowedRouteCardTypes;

        public RouteCardTypeExtensionPolicy(
            IReadOnlyCollection<RouteCardType> allowedRouteCardTypes) =>
            this.allowedRouteCardTypes = NotNullOrThrow(allowedRouteCardTypes);


        /// <inheritdoc />
        public bool IsAllowed(RouteCardType routeCardType) =>
            this.allowedRouteCardTypes.Contains(routeCardType);
    }
}
