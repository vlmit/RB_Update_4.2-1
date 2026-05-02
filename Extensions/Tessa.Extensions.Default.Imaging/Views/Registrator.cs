using Tessa.Views;
using Unity;

namespace Tessa.Extensions.Default.Imaging.Views
{
    [Registrator]
    public sealed class Registrator : RegistratorBase
    {
        public override void RegisterUnity() =>
            this.UnityContainer
                .RegisterSingleton<IExtraViewListProvider, DocLoadBehaviorsViewProvider>(nameof(DocLoadBehaviorsViewProvider))
            ;
    }
}
