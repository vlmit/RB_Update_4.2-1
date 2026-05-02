#nullable enable

using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Roles;
using Tessa.Roles.NestedRoles;
using Tessa.Roles.SmartRoles;
using Tessa.Views;
using Unity;

namespace Tessa.Extensions.Default.Server.Roles
{
    [Registrator]
    public sealed class Registrator : RegistratorBase
    {
        /// <inheritdoc/>
        public override void RegisterUnity()
        {
            this.UnityContainer
                .RegisterSingleton<INestedRoleContextSelector, NestedRoleContextSelector>()
                .RegisterSingleton<IViewInterceptor, TaskAssignedRoleUsersInterceptor>(nameof(TaskAssignedRoleUsersInterceptor))
                .RegisterSingleton<ISmartRoleGeneratorDataFactory, KrSmartRoleGeneratorDataFactory>()
                .RegisterSingleton<ISmartRoleGeneratorCacheObjectFactory, KrSmartRoleGeneratorCacheObjectFactory>();
        }


        /// <inheritdoc/>
        public override void RegisterExtensions(IExtensionContainer extensionContainer)
        {
            extensionContainer
                .RegisterExtension<ICardNewExtension, AdSyncRoleCardNewExtension>(x => x
                    .WithOrder(ExtensionStage.BeforePlatform, 1)
                    .WithSingleton()
                    .WhenCardTypes(RoleHelper.PersonalRoleTypeID, RoleHelper.DepartmentRoleTypeID, RoleHelper.StaticRoleTypeID)
                    .WhenMethod(CardNewMethod.Template));
        }
    }
}
