using Microsoft.Extensions.DependencyInjection;
using Tessa.Extensions.Default.Shared.Encryption;
using Tessa.Platform.Encryption;
using Tessa.Web.Registrations;

namespace Tessa.Extensions.Default.Server.Web.Encryption
{
    [WebRegistrator]
    public sealed class WebRegistrator : WebRegistratorBase
    {
        public override void RegisterServices()
        {
            this.Services
                .AddSingleton<IEncryptionService, AesEncryptionService>()
                .AddSingleton<IEncryptionCertificateLoader, EncryptionCertificateLoader>();
        }
    }
}
