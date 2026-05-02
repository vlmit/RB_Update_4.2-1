#nullable enable

using Tessa.Platform.Configuration;
using Unity;

namespace Tessa.Extensions.Default.Shared.Configuration
{
    [Registrator(Tag = RegistratorTag.GroupForConfiguration)]
    public sealed class Registrator : RegistratorBase
    {
        public override void RegisterUnity()
        {
            this.UnityContainer
                .RegisterSingleton<IConfigurationStorageTransformer, RedisConfigurationStorageTransformer>(
                    nameof(RedisConfigurationStorageTransformer))
                .RegisterSingleton<IConfigurationStorageTransformer, VaultConnectionStringTransformer>(
                    nameof(VaultConnectionStringTransformer))
                .RegisterSingleton<IConfigurationItemSourceLoader, RedisConfigurationLoader>(
                    RedisConfigurationLoader.Key)
                .RegisterSingleton<IConfigurationContextFinalizer, RedisConfigurationContextFinalizer>(
                    nameof(RedisConfigurationContextFinalizer))
                .RegisterSingleton<IConfigurationContextFinalizer, HttpClientConfigurationContextFinalizer>(
                    nameof(HttpClientConfigurationContextFinalizer))
                .RegisterSingleton<IConfigurationItemSourceLoader, EncryptedConfigurationLoader>(
                    EncryptedConfigurationLoader.Key)
                ;
        }
    }
}
