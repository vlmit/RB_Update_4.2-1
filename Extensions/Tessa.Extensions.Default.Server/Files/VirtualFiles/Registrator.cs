using System;
using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Compilation;
using Tessa.Extensions.Default.Server.Files.VirtualFiles.Compilation;
using Tessa.Platform;
using Unity;
using Unity.Lifetime;

namespace Tessa.Extensions.Default.Server.Files.VirtualFiles
{
    [Registrator]
    public sealed class Registrator : RegistratorBase
    {
        public override void RegisterUnity()
        {
            this.UnityContainer
                .RegisterType<IKrVirtualFileManager, KrVirtualFileManager>(new ContainerControlledLifetimeManager())
                .RegisterType<IKrVirtualFileCache, KrVirtualFileCache>(new ContainerControlledLifetimeManager())

                .RegisterType<IKrVirtualFileCompiler, KrVirtualFileCompiler>(new ContainerControlledLifetimeManager())
                .RegisterType<IKrVirtualFileCompilationCache, KrVirtualFileCompilationCache>(new ContainerControlledLifetimeManager())

                .RegisterType<VirtualFilesServerInstanceStoreExtension>(new ContainerControlledLifetimeManager())
                ;
        }

        public override void RegisterExtensions(IExtensionContainer extensionContainer)
        {
            extensionContainer.RegisterExtension<ICardStoreExtension, VirtualFilesServerInstanceStoreExtension>(x => x
                .WithOrder(ExtensionStage.AfterPlatform)
                .WithUnity(this.UnityContainer)
                .WhenCardTypes(CardHelper.ServerInstanceTypeID)
                .WhenAnyStoreMethod());
        }

        public override void FinalizeRegistration()
        {
            this.UnityContainer
                .TryResolve<ICompilationCacheContainer>()
                ?
                .Register<IKrVirtualFileCompilationCache, IKrVirtualFileCompilationContext, Guid, IKrVirtualFileScript>();
        }
    }
}
