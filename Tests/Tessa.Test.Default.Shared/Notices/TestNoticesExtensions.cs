#nullable enable

using Tessa.Notices;
using Unity;
using Unity.Lifetime;
using Unity.Resolution;

namespace Tessa.Test.Default.Shared.Notices
{
    public static class TestNoticesExtensions
    {
        #region IUnityContainer Extensions

        public static IUnityContainer FinalizeTestNoticesOnServer(this IUnityContainer unityContainer)
        {
            var defaultInstance = unityContainer.Resolve<INotificationManager>();
            var withoutTransactionInstance = unityContainer.Resolve<INotificationManager>(NotificationManagerNames.WithoutTransaction);
            var deferredWithoutTransactionInstance = unityContainer.Resolve<INotificationManager>(NotificationManagerNames.DeferredWithoutTransaction);

            return unityContainer
                .RegisterSingleton<TestNotificationList>()
                .RegisterFactory<INotificationManager>(
                    c => c.Resolve<TestNotificationManagerDecorator>(new DependencyOverride<INotificationManager>(defaultInstance)),
                    new ContainerControlledLifetimeManager())
                .RegisterFactory<INotificationManager>(
                    NotificationManagerNames.WithoutTransaction,
                    c => c.Resolve<TestNotificationManagerDecorator>(new DependencyOverride<INotificationManager>(withoutTransactionInstance)),
                    new ContainerControlledLifetimeManager())
                .RegisterFactory<INotificationManager>(
                    NotificationManagerNames.DeferredWithoutTransaction,
                    c => c.Resolve<TestNotificationManagerDecorator>(new DependencyOverride<INotificationManager>(deferredWithoutTransactionInstance)),
                    new ContainerControlledLifetimeManager());
        }

        #endregion
    }
}
