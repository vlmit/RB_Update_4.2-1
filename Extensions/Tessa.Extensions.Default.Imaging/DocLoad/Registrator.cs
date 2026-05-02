using Tessa.Imaging.DocLoad;
using Unity;

namespace Tessa.Extensions.Default.Imaging.DocLoad
{
    [Registrator]
    public class Registrator : RegistratorBase
    {
        public override void RegisterUnity()
        {
            this.UnityContainer
                .RegisterSingleton<IBarcodeConverter, BarcodeConverter>()
                ;
        }
    }
}
