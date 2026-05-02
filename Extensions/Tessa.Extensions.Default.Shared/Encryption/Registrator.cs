#nullable enable

using Tessa.Platform.Encryption;
using Unity;

namespace Tessa.Extensions.Default.Shared.Encryption
{
    [Registrator(Tag = RegistratorTag.GroupForConfiguration)]
    public class Registrator : RegistratorBase
    {
        public override void RegisterUnity()
        {
            this.UnityContainer
                .RegisterSingleton<IEncryptionService, AesEncryptionService>()
                .RegisterSingleton<IEncryptionCertificateLoader, EncryptionCertificateLoader>()
                ;
        }
    }
}
