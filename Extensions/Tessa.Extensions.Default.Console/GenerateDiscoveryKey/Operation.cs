using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Discovery;
using Tessa.Discovery.Senders;
using Tessa.Platform;
using Tessa.Platform.ConsoleApps;
using Tessa.Platform.Json;
using Tessa.Platform.Redis;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Console.GenerateDiscoveryKey
{
    public sealed class Operation : ConsoleOperation<OperationContext>
    {
        private readonly IDiscoveryKeySerializer discoveryKeySerializer;
        private readonly IJwtTokenSerializer jwtTokenSerializer;
        private readonly IDiscoveryCommandStrategy discoveryCommandStrategy;
        private readonly IRedisConnectionProvider redisConnectionProvider;
        private readonly IRedisConnectionStringCleaner redisConnectionStringCleaner;
        private readonly IDiscoveryComponentStrategy discoveryComponentStrategy;
        private readonly ITessaServerSettings tessaServerSettings;

        public Operation(
            IConsoleLogger logger,
            IConsoleSessionManager sessionManager,
            IDiscoveryKeySerializer discoveryKeySerializer,
            IJwtTokenSerializer jwtTokenSerializer,
            IDiscoveryCommandStrategy discoveryCommandStrategy,
            IRedisConnectionProvider redisConnectionProvider,
            IRedisConnectionStringCleaner redisConnectionStringCleaner,
            IDiscoveryComponentStrategy discoveryComponentStrategy,
            ITessaServerSettings tessaServerSettings,
            bool extendedInitialization = false)
            : base(logger, sessionManager, extendedInitialization)
        {
            this.discoveryKeySerializer = NotNullOrThrow(discoveryKeySerializer);
            this.jwtTokenSerializer = NotNullOrThrow(jwtTokenSerializer);
            this.discoveryCommandStrategy = NotNullOrThrow(discoveryCommandStrategy);
            this.redisConnectionProvider = NotNullOrThrow(redisConnectionProvider);
            this.redisConnectionStringCleaner = NotNullOrThrow(redisConnectionStringCleaner);
            this.discoveryComponentStrategy = NotNullOrThrow(discoveryComponentStrategy);
            this.tessaServerSettings = NotNullOrThrow(tessaServerSettings);
        }

        public override async Task<int> ExecuteAsync(OperationContext context, CancellationToken cancellationToken = default)
        {
            if (context.Mode == Mode.Generate)
            {
                await this.Logger.InfoAsync($"Running in \"{nameof(Mode.Generate)}\" mode. Keys will only be saved on disk.");
            }
            else
            {
                if (context.Mode == Mode.Register)
                {
                    await this.Logger.InfoAsync($"Running in {nameof(Mode.Register)} mode. Keys will be saved on disk and in Redis.");
                }
                else
                {
                    await this.Logger.InfoAsync(
                        $"Running in {nameof(Mode.Publish)} mode. Keys will be saved on disk and in Redis. " +
                        $"Additionally, a command will be sent to register key in components.");
                }

                // Для режимов Publish и Register нам нужен redis. Проверяем его доступность.
                var redisConnectionString = this.tessaServerSettings.RedisConnectionString;

                await this.Logger.InfoAsync(
                    "Redis connection: {0}",
                    await this.redisConnectionStringCleaner.CleanConnectionSafeAsync(redisConnectionString, cancellationToken));

                await this.Logger.InfoAsync("Connecting to Redis...");

                // checking only that connection is available (and cached in DI)
                var connection = await this.redisConnectionProvider.GetOpenedConnectionAsync(cancellationToken);
                connection.GetDatabase();

                await this.Logger.InfoAsync("Connected to Redis");
            }

            var (publicKey, privateKey) = GetKeys();

            var currentDate = DateTime.UtcNow;
            var discoveryKey = new DiscoveryKey
            {
                KeyID = SHA512.HashData(publicKey),
                Scopes = context.Scopes,
                Subject = context.Subject,
                PrivateKey = privateKey,
                PublicKey = publicKey,
                Algorithm = DiscoveryKeyHelper.Ecdsa,
                IssuedAt = currentDate,
                ExpiredAt = currentDate.AddMonths(context.ExpirationMonths)
            };
            var signingKeyPayload = discoveryKey;

            if (context.SelfSigned)
            {
                discoveryKey.Issuer = discoveryKey.Subject;
                discoveryKey.IssuerKeyID = discoveryKey.KeyID;
            }
            else
            {
                ThrowIfNull(context.Parent);
                ThrowIfNull(context.ParentPassword);

                signingKeyPayload = await DiscoverySenderHelper.LoadKeyAsync(this.discoveryKeySerializer, context.Parent, context.ParentPassword, cancellationToken);
                if (signingKeyPayload.ExpiredAt < DateTime.UtcNow)
                {
                    throw new InvalidOperationException("Unable to sign with an already expired key");
                }

                if (signingKeyPayload.Scopes?.Contains(DiscoveryScopes.PublishKey, StringComparer.OrdinalIgnoreCase) != true)
                {
                    throw new InvalidOperationException($"Signing key doesn't have \"{DiscoveryScopes.PublishKey}\" scope");
                }

                discoveryKey.Issuer = NotNullOrThrow(signingKeyPayload.Subject);
                discoveryKey.IssuerKeyID = NotNullOrThrow(signingKeyPayload.KeyID);
            }

            var serializedJwtToken = this.discoveryKeySerializer.Serialize(discoveryKey, context.Password);

            var publishedKey = DiscoveryPublishedKey.FromPrivate(discoveryKey);

            var serializedPublishedKey = StorageHelper.SerializeToJson(publishedKey, TessaSerializer.Json);
            var serializedPublishedKeyJwtToken = this.jwtTokenSerializer.Serialize(serializedPublishedKey, discoveryKey);

            var output = context.Output;
            await File.WriteAllTextAsync(output ?? "private.key", serializedJwtToken, cancellationToken);
            var publicKeyPath = output is null ? "public.key" : $"{Path.GetFileNameWithoutExtension(output)}_public.key";
            await File.WriteAllTextAsync(publicKeyPath, serializedPublishedKeyJwtToken, cancellationToken);

            // Если это не генерация, то либо регистрация, либо публикация
            if (context.Mode != Mode.Generate)
            {
                // Публикация всегда подразумевает регистрацию
                await this.RegisterKeyAsync(discoveryKey, serializedPublishedKeyJwtToken);

                if (context.Mode != Mode.Register)
                {
                    return await this.PublishKeyAsync(context.StdOut, serializedPublishedKeyJwtToken, signingKeyPayload);
                }
            }

            return 0;
        }

        private static (byte[] PublicKey, byte[] PrivateKey) GetKeys()
        {
            // TODO: посмотреть начальные значения
            using var dsa = ECDsa.Create();

            byte[] publicKey = dsa.ExportSubjectPublicKeyInfo();
            byte[] privateKey = dsa.ExportPkcs8PrivateKey();
            return (publicKey, privateKey);
        }

        private async Task RegisterKeyAsync(DiscoveryKey discoveryKey, string serializedPublishedKeyJwtToken)
        {
            var connection = await this.redisConnectionProvider.GetOpenedConnectionAsync();
            var db = connection.GetDatabase();
            await db.HashSetAsync(RedisHelper.GetCommandsKeysKey(), Convert.ToBase64String(NotNullOrThrow(discoveryKey.KeyID)), serializedPublishedKeyJwtToken);
        }

        private async Task<int> PublishKeyAsync(TextWriter stdOut, string serializedPublicKey, DiscoveryKey signingKey)
        {
            var commandCreationDate = DateTime.UtcNow;
            var discoveryCommandRequest = new DiscoveryCommandRequest
            {
                Type = "PublishKey",
                Created = commandCreationDate,
                ExpireAt = commandCreationDate.AddMinutes(3),
                Arguments =
                {
                    ["Key"] = serializedPublicKey
                },
                Scopes = new[] { DiscoveryScopes.PublishKey },
                Targets = new[] { "all" }
            };

            var components = await this.discoveryComponentStrategy.GetComponentsAsync();
            if (components.Count == 0)
            {
                await this.Logger.InfoAsync("No components are found");
                return 1;
            }

            var componentsTextPositions = new Dictionary<string, int>(StringComparer.Ordinal);
            var componentPosition = components.Count;
            foreach (var component in components)
            {
                ThrowIfNull(component.Name);
                componentsTextPositions.Add(component.Name, componentPosition);
                componentPosition--;
                await stdOut.WriteLineAsync($"WAITING {component.Name}");
            }

            var responseQueue = new ConcurrentQueue<DiscoveryCommandResponse>();

            using var cts = new CancellationTokenSource();
            var task = Task.Run(async () =>
            {
                // ReSharper disable once AccessToDisposedClosure
                var token = cts.Token;

                var counter = 0;
                var finalCycle = false;
                while (true)
                {
                    if (responseQueue.TryDequeue(out var queuedResponse))
                    {
                        ThrowIfNull(queuedResponse.Cid);
                        ThrowIfNull(queuedResponse.Result);

                        var currentCursorPosition = (System.Console.CursorLeft, System.Console.CursorTop);
                        // Теоретически мы можем получить ответ от компонента, от которого мы не ожидали
                        // В таком случае мы его просто игнорируем
                        if (componentsTextPositions.TryGetValue(queuedResponse.Cid, out var componentPositionDelta))
                        {
                            System.Console.SetCursorPosition(0, System.Console.CursorTop - componentPositionDelta);
                            var resultColor = GetColorForState(queuedResponse.Result);
                            OutputWithColor(stdOut, resultColor, queuedResponse.Result);
                            await stdOut.WriteAsync($" {queuedResponse.Cid}: {queuedResponse.Text}");
                            System.Console.SetCursorPosition(currentCursorPosition.Item1, currentCursorPosition.Item2);
                        }

                        continue;
                    }

                    // В очереди пусто и это последний цикл, выходим
                    if (finalCycle)
                    {
                        return;
                    }

                    // Взвели отмену - делаем ещё один цикл (чтобы обработать всё, что осталось в очереди)
                    if (token.IsCancellationRequested)
                    {
                        finalCycle = true;
                        continue;
                    }

                    counter++;
                    switch (counter % 4)
                    {
                        case 0:
                            await stdOut.WriteAsync("/");
                            break;
                        case 1:
                            await stdOut.WriteAsync("-");
                            break;
                        case 2:
                            await stdOut.WriteAsync("\\");
                            break;
                        case 3:
                            await stdOut.WriteAsync("|");
                            break;
                    }

                    System.Console.SetCursorPosition(System.Console.CursorLeft - 1, System.Console.CursorTop);
                    await Task.Delay(50, CancellationToken.None);
                }
            }, cts.Token);

            try
            {
                await this.discoveryCommandStrategy.SendAsync(discoveryCommandRequest, signingKey, (response, ct) =>
                {
                    responseQueue.Enqueue(response);
                    return Task.CompletedTask;
                }, cancellationToken: CancellationToken.None);
            }
            finally
            {
                await cts.CancelAsync();
                await task;
            }

            return 0;
        }

        private static void OutputWithColor(TextWriter stdOut, ConsoleColor color, string text)
        {
            var previousColor = System.Console.ForegroundColor;
            System.Console.ForegroundColor = color;
            stdOut.Write(text);
            System.Console.ForegroundColor = previousColor;
        }

        private static ConsoleColor GetColorForState(string? state)
        {
            return state?.ToUpperInvariant() switch
            {
                DiscoveryHelper.CommandResponseStateOK => ConsoleColor.Green,
                DiscoveryHelper.CommandResponseStateWarning => ConsoleColor.Yellow,
                DiscoveryHelper.CommandResponseStateError => ConsoleColor.Red,
                _ => ConsoleColor.Gray
            };
        }
    }
}
