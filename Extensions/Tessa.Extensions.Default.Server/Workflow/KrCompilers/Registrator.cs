#nullable enable

using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Compilation;
using Tessa.Extensions.Default.Server.Workflow.KrCompilers.UserAPI;
using Tessa.Extensions.Default.Shared;
using Tessa.Platform;
using Unity;
using Unity.Lifetime;

namespace Tessa.Extensions.Default.Server.Workflow.KrCompilers
{
    [Registrator]
    public sealed class Registrator :
        RegistratorBase
    {
        /// <inheritdoc/>
        public override void RegisterUnity()
        {
            this.UnityContainer
                .RegisterType<IExtraSourceSerializer, ExtraSourceStorageSerializer>(new ContainerControlledLifetimeManager())
                .RegisterType<IKrPreprocessorProvider, KrPreprocessorProvider>(new ContainerControlledLifetimeManager())
                .RegisterType<IKrCompiler, KrCompiler>(new ContainerControlledLifetimeManager())
                .RegisterType<IKrProcessCache, KrProcessCache>(new ContainerControlledLifetimeManager())
                .RegisterType<IKrStageTemplateCompilationCache, KrStageTemplateCompilationCache>(new ContainerControlledLifetimeManager())
                .RegisterType<IKrStageGroupCompilationCache, KrStageGroupCompilationCache>(new ContainerControlledLifetimeManager())
                .RegisterType<IKrSecondaryProcessCompilationCache, KrSecondaryProcessCompilationCache>(new ContainerControlledLifetimeManager())
                .RegisterType<IKrCommonMethodCompilationCache, KrCommonMethodCompilationCache>(new ContainerControlledLifetimeManager())

                .RegisterType<IKrExecutor, KrStageExecutor>(KrExecutorNames.StageExecutor, new ContainerControlledLifetimeManager())
                .RegisterType<IKrExecutor, KrGroupExecutor>(KrExecutorNames.GroupExecutor, new ContainerControlledLifetimeManager())
                .RegisterFactory<IKrExecutor>(
                    static c => c.Resolve<IKrExecutor>(KrExecutorNames.GroupExecutor),
                    new ContainerControlledLifetimeManager())
                .RegisterType<IKrStageTemplateLockStrategy, KrStageTemplatesLockStrategy>(new ContainerControlledLifetimeManager())
                .RegisterType<KrStageCardTemplateStoreExtension>(new ContainerControlledLifetimeManager())
                .RegisterType<KrStageCardTemplateDeleteExtension>(new ContainerControlledLifetimeManager())
            ;
        }

        /// <inheritdoc/>
        public override void RegisterExtensions(IExtensionContainer extensionContainer)
        {
            extensionContainer
                .RegisterExtension<ICardStoreExtension, KrStageCardTemplateStoreExtension>(x => x
                    .WithOrder(ExtensionStage.Finalize, 1)
                    .WithUnity(this.UnityContainer)
                    .WhenAnyStoreMethod()
                    .WhenCardTypes(DefaultCardTypes.KrStageTemplateTypeID, DefaultCardTypes.KrStageGroupTypeID))

                .RegisterExtension<ICardDeleteExtension, KrStageCardTemplateDeleteExtension>(x => x
                    .WithOrder(ExtensionStage.Finalize, 2)
                    .WithUnity(this.UnityContainer)
                    .WhenAnyDeleteMethod()
                    .WhenCardTypes(DefaultCardTypes.KrStageTemplateTypeID, DefaultCardTypes.KrStageGroupTypeID))
                    ;
        }
        /// <inheritdoc/>
        public override void FinalizeRegistration()
        {
            this.UnityContainer
                .TryResolve<ICompilationCacheContainer>()
                ?
                .Register<IKrStageTemplateCompilationCache, IKrCompilationContext, string, IKrScript>()
                .Register<IKrStageGroupCompilationCache, IKrCompilationContext, string, IKrScript>()
                .Register<IKrSecondaryProcessCompilationCache, IKrCompilationContext, string, IKrScript>()
                .Register<IKrCommonMethodCompilationCache, IKrCompilationContext, string, IKrScript>()
                ;
        }
    }
}
