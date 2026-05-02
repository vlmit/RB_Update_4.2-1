using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Discovery;
using Tessa.Platform.Json;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.Platform.Web;
using Tessa.Webbi;
using Unity;

namespace Tessa.Extensions.Default.Console.SendCommand
{
    public sealed class ClientDiscoveryCommandStrategy : IDiscoveryCommandStrategy
    {
        #region Fields

        private readonly IJwtTokenSerializer jwtTokenSerializer;
        private readonly IWebProxyFactory webbiProxyFactory;

        #endregion

        #region Constructor

        public ClientDiscoveryCommandStrategy(IJwtTokenSerializer jwtTokenSerializer,
            [Dependency(nameof(WebbiWebProxyFactory))] IWebProxyFactory webbiProxyFactory)
        {
            this.jwtTokenSerializer = NotNullOrThrow(jwtTokenSerializer);
            this.webbiProxyFactory = NotNullOrThrow(webbiProxyFactory);
        }

        #endregion
        
        #region IDiscoveryCommandStrategy Implementation

        public async Task<IList<DiscoveryCommandResponse>> SendAsync(
            DiscoveryCommandRequest request,
            DiscoveryKey signingKey,
            Func<DiscoveryCommandResponse, CancellationToken, Task>? progressFunc = null,
            bool nowait = false,
            CancellationToken cancellationToken = default)
        {
            var commandJson = StorageHelper.SerializeToJson(request, TessaSerializer.Json);
            var commandJwt = this.jwtTokenSerializer.Serialize(commandJson, signingKey);

            await using var proxy = await this.webbiProxyFactory.UseProxyAsync<WebbiWebProxy>(cancellationToken: cancellationToken);
            ISet<string>? components;
            try
            {
                components = await proxy.SendCommandAsync(commandJwt, cancellationToken);
            }
            catch (ValidationException e)
            {
                throw new InvalidOperationException("Invalid command request.", e);
            }
            
            if (components.Count == 0 || nowait)
            {
                return Array.Empty<DiscoveryCommandResponse>();
            }

            var commandStateJson = StorageHelper.SerializeToJson(new WebbiWebProxy.CommandStatusRequest()
            {
                ID = request.ID,
            }, TessaSerializer.Json);
            var commandStateJwt = this.jwtTokenSerializer.Serialize(commandStateJson, signingKey);

            var responses = new Dictionary<string, DiscoveryCommandResponse>(components.Count);
            foreach (string name in components)
            {
                responses[name] = new DiscoveryCommandResponse() { Cid = name, Result = "No response" };
            }
            var responsesToReceive = new HashSet<string>(components);
            while (DateTime.UtcNow < request.ExpireAt)
            {
                await Task.Delay(1000, cancellationToken);
                IReadOnlyList<DiscoveryCommandResponse>? commandResponses;
                try
                {
                    commandResponses = await proxy.GetCommandStatusAsync(commandStateJwt, cancellationToken);
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException("Invalid command status request.", e);
                }

                foreach (var response in commandResponses)
                {
                    if (response.Cid is null)
                    {
                        // Ответ от неизвестного компонента, игнорируем
                        continue;
                    }
                    responsesToReceive.Remove(response.Cid);
                    responses[response.Cid] = response;
                    if (progressFunc is not null)
                    {
                        await progressFunc(response, cancellationToken);
                    }
                }
                if (responsesToReceive.Count == 0)
                {
                    return responses.Values.ToList();
                }
            }
            return responses.Values.ToList();
        }

        #endregion
    }
}
