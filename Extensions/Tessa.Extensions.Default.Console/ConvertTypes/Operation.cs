using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Platform.ConsoleApps;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Console.ConvertTypes
{
    public sealed class Operation(
        IConsoleSessionManager sessionManager,
        IConsoleLogger logger,
        ICardTypeClientRepository cardTypeClientRepository)
        : ConsoleOperation<OperationContext>(logger, sessionManager)
    {
        private readonly ICardTypeClientRepository cardTypeClientRepository = NotNullOrThrow(cardTypeClientRepository);

        /// <inheritdoc />
        public override async Task<int> ExecuteAsync(OperationContext context, CancellationToken cancellationToken = default)
        {
            if (!this.SessionManager.IsOpened)
            {
                return -1;
            }

            var convertedCount = 0;
            var skippedCount = 0;
            try
            {
                await this.Logger.InfoAsync("Converting types from: \"{0}\"", context.Source);

                foreach (var typeFile in DefaultConsoleHelper.GetSourceFiles(context.Source, "*.jtype", throwIfNotFound: false))
                {
                    await this.Logger.InfoAsync("Reading and converting type from: \"{0}\"", typeFile);

                    var text = await File.ReadAllTextAsync(typeFile, cancellationToken);
                    var cardType = NotNullOrThrow(await CardSerializableObject.DeserializeFromJsonAsync<CardType>(text, null, cancellationToken));

                    var newStorage = await this.cardTypeClientRepository.SerializeTypeAsync(cardType, cancellationToken);
                    var newText = StorageHelper.SerializeToTypedJson(newStorage, indented: true);

                    if (text != newText)
                    {
                        await this.Logger.InfoAsync("Saving converted type to: \"{0}\"", typeFile);
                        await File.WriteAllTextAsync(typeFile, newText, Encoding.UTF8, cancellationToken);
                        convertedCount++;
                    }
                    else
                    {
                        skippedCount++;
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await this.Logger.LogExceptionAsync("Error converting types", ex);
                return -1;
            }

            await this.Logger.InfoAsync("Converted types ({0}), skipped ({1})", convertedCount, skippedCount);
            return 0;
        }
    }
}
