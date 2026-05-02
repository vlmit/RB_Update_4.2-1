#nullable enable

using System;
using Tessa.Extensions.Default.Server.Plugins.FileConverters;
using Tessa.Extensions.Default.Server.Plugins.Notices;
using Tessa.Extensions.Default.Server.Plugins.OnlyOffice;
using Tessa.Extensions.Default.Server.Plugins.RefGroups;
using Tessa.Extensions.Default.Server.Plugins.SignatureArchive;
using Tessa.Extensions.Default.Server.Plugins.Workflow;
using Tessa.Extensions.Default.Server.Workflow;
using Tessa.Platform;
using Tessa.Platform.Plugins;
using Unity;
using Unity.Lifetime;

namespace Tessa.Extensions.Default.Server.Plugins
{
    [Registrator]
    public sealed class Registrator : RegistratorBase
    {
        #region Base Overrides

        /// <inheritdoc/>
        public override void RegisterUnity()
        {
            this.UnityContainer
                .RegisterSingleton<PasswordNotificationsPluginHandler>()
                .RegisterSingleton<TasksNotificationsPluginHandler>()
                .RegisterSingleton<TokenNotificationsPluginHandler>()
                .RegisterSingleton<MailSenderPluginHandler>()
                .RegisterSingleton<FileConverterRemoveCachePluginHandler>()
                .RegisterSingleton<OnlyOfficeRemoveFileCacheInfoPluginHandler>()
                .RegisterSingleton<RefGroupsRecalculatePluginHandler>()
                .RegisterSingleton<KrAutoApprovePluginHandler>()
                .RegisterSingleton<MobileApprovalPluginHandler>()
                .RegisterSingleton<ReturnTasksFromPostponedPluginHandler>()
                .RegisterSingleton<SignatureArchivePluginHandler>()

                .RegisterSingleton<MailSenderConfig>()
                .RegisterSingleton<MobileApprovalConfig>()

                .RegisterType<IMailFileLoaderService, MailFileLoaderService>(new PerResolveLifetimeManager())
                .RegisterType<IMailSentNotificationService, MailSentNotificationService>(new PerResolveLifetimeManager())

                // resolution factories
                .RegisterFactory<Func<string?, IMailFileLoaderService>>(
                    c => new Func<string?, IMailFileLoaderService>(_ => c.Resolve<IMailFileLoaderService>()),
                    new ContainerControlledLifetimeManager())
                .RegisterFactory<Func<string?, IMailSentNotificationService>>(
                    c => new Func<string?, IMailSentNotificationService>(_ => c.Resolve<IMailSentNotificationService>()),
                    new ContainerControlledLifetimeManager())

                .RegisterType<ExchangeSender>(new PerResolveLifetimeManager())
                .RegisterType<SmtpSender>(new PerResolveLifetimeManager())
                .RegisterType<IOutboxManager, OutboxManager>(new PerResolveLifetimeManager())

                .RegisterType<IMailReceiver, ExchangeMailReceiver>(MailReceiverNames.ExchangeMailReceiver, new PerResolveLifetimeManager())
                .RegisterType<IMailReceiver, Pop3MailReceiver>(MailReceiverNames.Pop3MailReceiver, new PerResolveLifetimeManager())
                .RegisterType<IMailReceiver, ImapMailReceiver>(MailReceiverNames.ImapMailReceiver, new PerResolveLifetimeManager())
                ;
        }

        public override void FinalizeRegistration()
        {
            this.UnityContainer
                .TryResolve<IPluginHandlerResolver>()?
                .Register<PasswordNotificationsPluginHandler>(DefaultPluginNames.PasswordNotificationsPlugin)
                .Register<TasksNotificationsPluginHandler>(DefaultPluginNames.TasksNotificationsPlugin)
                .Register<TokenNotificationsPluginHandler>(DefaultPluginNames.TokenNotificationsPlugin)
                .Register<MailSenderPluginHandler>(DefaultPluginNames.MailSenderPlugin)
                .Register<FileConverterRemoveCachePluginHandler>(DefaultPluginNames.FileConverterRemoveCachePlugin)
                .Register<OnlyOfficeRemoveFileCacheInfoPluginHandler>(DefaultPluginNames.OnlyOfficeRemoveFileCacheInfoPlugin)
                .Register<RefGroupsRecalculatePluginHandler>(DefaultPluginNames.RefGroupsRecalculatePlugin)
                .Register<KrAutoApprovePluginHandler>(DefaultPluginNames.KrAutoApprovePlugin)
                .Register<MobileApprovalPluginHandler>(DefaultPluginNames.MobileApprovalPlugin)
                .Register<ReturnTasksFromPostponedPluginHandler>(DefaultPluginNames.ReturnTasksFromPostponedPlugin)
                .Register<SignatureArchivePluginHandler>(DefaultPluginNames.SignatureArchivePlugin)
                ;
        }

        #endregion
    }
}
