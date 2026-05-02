using Microsoft.Extensions.DependencyInjection;
using Tessa.Extensions.Default.Shared.Configuration;
using Tessa.Platform.Configuration;
using Tessa.Web.Registrations;

namespace Tessa.Extensions.Default.Server.Web.Configuration
{
    [WebRegistrator]
    public sealed class WebRegistrator : WebRegistratorBase
    {
        public override void RegisterServices()
        {
            this.Services
                .AddSingleton<IConfigurationStorageTransformer, RedisConfigurationStorageTransformer>()
                .AddSingleton<IConfigurationStorageTransformer, VaultConnectionStringTransformer>()
                .AddKeyedSingleton<IConfigurationItemSourceLoader, RedisConfigurationLoader>(
                    RedisConfigurationLoader.Key)
                .AddSingleton<IConfigurationContextFinalizer, RedisConfigurationContextFinalizer>()
                .AddSingleton<IConfigurationContextFinalizer, HttpClientConfigurationContextFinalizer>()
                .AddKeyedSingleton<IConfigurationItemSourceLoader, EncryptedConfigurationLoader>(
                    EncryptedConfigurationLoader.Key)
                ;
        }
    }
}
