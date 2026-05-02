#nullable enable
using System.Collections.Generic;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Serialization
{
    public static class KrStageRowExtensions
    {
        public static IExtensionContainer RegisterKrStageRowExtensionTypes(
            this IExtensionContainer extensionContainer) =>
            NotNullOrThrow(extensionContainer)
                .RegisterType<IKrStageRowExtension>(x => x
                        .MethodAsync<IKrStageRowExtensionContext>(y => y.BeforeSerialization)
                        .MethodAsync<IKrStageRowExtensionContext>(y => y.DeserializationBeforeRepair)
                        .MethodAsync<IKrStageRowExtensionContext>(y => y.DeserializationAfterRepair),
                    x => x.Register(KrStageRowExtensionFilterPolicy.Instance));

        public static IExtensionPolicyContainer WhenRouteCardTypes(
            this IExtensionPolicyContainer policyContainer,
            IReadOnlyCollection<RouteCardType> routeCardTypes) =>
            NotNullOrThrow(policyContainer)
                .Register(new RouteCardTypeExtensionPolicy(routeCardTypes));

        public static IExtensionPolicyContainer WhenRouteCardTypes(
            this IExtensionPolicyContainer policyContainer,
            params RouteCardType[] routeCardTypes) =>
            NotNullOrThrow(policyContainer)
                .Register(new RouteCardTypeExtensionPolicy(routeCardTypes));
    }
}
