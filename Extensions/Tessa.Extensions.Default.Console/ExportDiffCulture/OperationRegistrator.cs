using Unity;

namespace Tessa.Extensions.Default.Console.ExportDiffCulture
{
    [Registrator(Tag = RegistratorTag.ClientConsole)]
    public sealed class OperationRegistrator :
        RegistratorBase
    {
        public override void RegisterUnity()
        {
            this.UnityContainer
                .RegisterSingleton<Operation>()
                ;
        }
    }
}
