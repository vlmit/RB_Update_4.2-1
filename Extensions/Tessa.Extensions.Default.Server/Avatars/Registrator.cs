#nullable enable

using Tessa.Content.Avatars;
using Unity;

namespace Tessa.Extensions.Default.Server.Avatars
{
    [Registrator]
    public sealed class Registrator : RegistratorBase
    {
        public override void RegisterUnity()
        {
            this.UnityContainer
                .RegisterSingleton<IAvatarContentPermissionsManager, KrAvatarContentPermissionsManager>()
                ;
        }
    }
}
