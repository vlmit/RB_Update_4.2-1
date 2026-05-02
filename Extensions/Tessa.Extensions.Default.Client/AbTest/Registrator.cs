using System;
using Tessa.Cards;
using Tessa.Extensions.Default.Client.Views;
using Tessa.Extensions.Default.Shared.AbTest;
using Tessa.Platform;
using Tessa.UI.Cards;
using Tessa.UI.Files;
using Tessa.UI.Tiles.Extensions;
using Tessa.UI.Views.Extensions;
using Tessa.Views;
using Unity;

namespace Tessa.Extensions.Default.Client.AbTest
{
    [Registrator]
    public sealed class Registrator : RegistratorBase
    {
        public override void RegisterUnity()
        {
            this.UnityContainer
                .RegisterSingleton<AbCarUIExtension>()
                .RegisterSingleton<AbCustomFilesViewCardControlInitializationStrategy>()
                .RegisterSingleton<AbExternalFilesFileControlExtension>()
                .RegisterSingleton<AbExternalSystemRequestTileExtension>()
                .RegisterSingleton<AbSettingsTileExtension>()
                .RegisterSingleton<AbTestProcessTileExtension>()
                .RegisterSingleton<ITessaView, AbClientProgramView>(nameof(AbClientProgramView))
                ;
        }

        public override void RegisterExtensions(IExtensionContainer extensionContainer)
        {
            extensionContainer
                .RegisterExtension<ICardUIExtension, AbCarUIExtension>(x => x
                    .WithOrder(ExtensionStage.AfterPlatform, 1)
                    .WithUnity(this.UnityContainer)
                    .WhenCardTypes(AbCardTypes.AbCarTypeID))
                .RegisterExtension<IFileControlExtension, AbExternalFilesFileControlExtension>(x => x
                    .WithOrder(ExtensionStage.BeforePlatform, 1)
                    .WithUnity(this.UnityContainer))
                .RegisterExtension<IFileExtension, AbExternalFileExtension>(x => x
                    .WithOrder(ExtensionStage.AfterPlatform, 1)
                    .WithSingleton())
                .RegisterExtension<ITileGlobalExtension, AbExternalSystemRequestTileExtension>(x => x
                    .WithOrder(ExtensionStage.AfterPlatform, 4)
                    .WithUnity(this.UnityContainer))
                .RegisterExtension<ITileGlobalExtension, AbSettingsTileExtension>(x => x
                    .WithOrder(ExtensionStage.AfterPlatform, 6)
                    .WithUnity(this.UnityContainer))
                .RegisterExtension<ITileGlobalExtension, AbTestProcessTileExtension>(x => x
                    .WithOrder(ExtensionStage.AfterPlatform, 7)
                    .WithUnity(this.UnityContainer))
                ;
        }

        public override void FinalizeRegistration()
        {
            this.UnityContainer.TryResolve<IWorkplaceExtensionRegistry>()?
                .Register(typeof(AbCustomFolderExtension))
                .RegisterConfiguratorType(
                    typeof(AbCustomFolderExtension),
                    type => this.UnityContainer.Resolve<AbCustomFolderExtensionConfigurator>())
                //
                .Register(typeof(AbGetDataWithDelayExtension))
                .RegisterConfiguratorType(
                    typeof(AbGetDataWithDelayExtension),
                    type => this.UnityContainer.Resolve<AbGetDataWithDelayExtensionConfigurator>())
                //
                .Register(typeof(AbTreeViewItemExtension))
                .RegisterConfiguratorType(
                    typeof(AbTreeViewItemExtension),
                    type => this.UnityContainer.Resolve<AbTreeViewItemExtensionConfigurator>());

            this.UnityContainer.TryResolve<IFilterViewDialogDescriptorRegistry>()?
                .Register(
                    // идентификатор узла дерева в рабочем месте:
                    new Guid(0x193496fb, 0xe9a1, 0x49a1, 0xb9, 0xf4, 0x8d, 0xbf, 0x73, 0xd5, 0x6b, 0xd4), // 193496fb-e9a1-49a1-b9f4-8dbf73d56bd4
                    AbFilterViewDialogDescriptors.Cars);
        }
    }
}
