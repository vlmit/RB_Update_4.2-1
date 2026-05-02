using Tessa.Imaging.DocLoad;
using Tessa.Platform;

namespace Tessa.Extensions.Default.Imaging.Ai
{
    [Registrator]
    public class Registrator : RegistratorBase
    {
        public override void RegisterUnity()
        {
            this.UnityContainer
                // Register DocLoad Behaviors
                .TryResolve<IDocLoadBehaviorResolver>()?
                    .Register<DocLoadAiIncomingFilesBehavior>(nameof(DocLoadAiIncomingFilesBehavior))
                ;
        }
    }
}
