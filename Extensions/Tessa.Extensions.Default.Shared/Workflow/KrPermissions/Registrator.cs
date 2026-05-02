using Tessa.Cards;
using Unity;
using Unity.Lifetime;

namespace Tessa.Extensions.Default.Shared.Workflow.KrPermissions
{
    [Registrator]
    public sealed class Registrator : RegistratorBase
    {
        public override void RegisterUnity()
        {
            this.UnityContainer
                // Переопределяем менеджер проверки принадлежности типа карточки к кастомной подсистеме прав доступа
                .RegisterType<ICardTypePermissionsManager, KrCardTypePermissionsManager>(new ContainerControlledLifetimeManager())
                ;
        }
    }
}
