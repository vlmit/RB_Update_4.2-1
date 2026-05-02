using Unity;

namespace Tessa.Extensions.Default.Console.TextRecognition
{
    [Registrator(Tag = RegistratorTag.ClientConsole)]
    public sealed class ClientOperationRegistrator : RegistratorBase
    {
        public override void RegisterUnity()
        {
            this.UnityContainer
                .RegisterSingleton<Sync.Operation>()
                .RegisterSingleton<Async.Operation>();
        }
    }
}
