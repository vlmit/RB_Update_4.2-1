using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Discovery;
using Tessa.Discovery.Senders;
using Tessa.Platform;
using Tessa.Platform.ConsoleApps;

namespace Tessa.Extensions.Default.Console.SendCommand
{
    public sealed class Operation :
        ConsoleOperation<OperationContext>
    {
        #region Fields

        private readonly IDiscoveryKeySerializer keySerializer;
        private readonly IDiscoveryCommandStrategy commandStrategy;
        private readonly IDiscoverySenderCommandHandler[] handlers;

        #endregion
        
        public Operation(
            IConsoleLogger logger,
            IConsoleSessionManager sessionManager,
            IDiscoveryKeySerializer keySerializer,
            IDiscoveryCommandStrategy commandStrategy,
            Func<IEnumerable<IDiscoverySenderCommandHandler>> handlersGetter,
            bool extendedInitialization = false)
            : base(logger, sessionManager, extendedInitialization)
        {
            this.keySerializer = NotNullOrThrow(keySerializer);
            this.commandStrategy = NotNullOrThrow(commandStrategy);
            ThrowIfNull(handlersGetter);
            this.handlers = handlersGetter()
                .OrderByAttributeAndType()
                .ToArray();
        }

        #region Base Overrides

        public override async Task<int> ExecuteAsync(OperationContext context, CancellationToken cancellationToken = default)
        {
            var key = await DiscoverySenderHelper.LoadKeyAsync(this.keySerializer, context.KeyPath, context.KeyPassword, cancellationToken);
            ThrowIfNull(key);
            
            var commandContext = new DiscoverySendCommandContext(context.Command, context.Arguments, key)
            {
                InitialScopes = context.Scopes,
                InitialTargets = context.Targets,
                InitialTimeout = context.Timeout,
                IsClient = context.IsClient,
            };
            if (context.IsClient)
            {
                commandContext.LoginFunc = async ct =>
                {
                    if (!await this.LoginAsync(context.UserName, context.Password, ct))
                    {
                        throw new InvalidOperationException("Can't login to system.");
                    }
                };
            }
            
            // preparation
            var commandHandlers = new List<IDiscoverySenderCommandHandler>(this.handlers.Length);
            foreach (IDiscoverySenderCommandHandler handler in this.handlers)
            {
                if (!handler.IsApplicable(commandContext.Command))
                {
                    continue;
                }
                commandHandlers.Add(handler);
                try
                {
                    await handler.PrepareRequestAsync(commandContext, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    await this.Logger.LogExceptionAsync("Can't prepare command request.", e);
                    return -2;
                }
            }
            
            // sending
            var responses = await this.commandStrategy.SendAsync(commandContext.Request, commandContext.SigningKey,
                async (resp, ct) =>
                {
                    foreach (var handler in commandHandlers)
                    {
                        try
                        {
                            await handler.HandleResponseAsync(resp, ct);
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
                        }
                        catch (Exception e)
                        {
                            await this.Logger.LogExceptionAsync("Can't handle command response.", e);
                        }
                    }
                }, context.Nowait, cancellationToken);
            
            // analyzing results
            var result = DiscoverySenderHelper.HandleCompletedCommand(responses);
            if (!result.IsSuccessful)
            {
                await this.Logger.LogResultAsync(result);
                return -3;
            }

            foreach (var response in responses)
            {
                System.Console.WriteLine($"{response.Cid} {response.Result}: {response.Text}");
            }

            return 0;
        }

        #endregion
    }
}
