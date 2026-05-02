#nullable enable

using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Platform.Server.Cards;

namespace Tessa.Extensions.Default.Server.AbTest
{
    /// <summary>
    /// Пример расширения, которое сохраняет файлы из задач карточки в карточку - файловый сателлит.
    /// </summary>
    public sealed class AbTaskFilesExampleStoreExtension(
        ICardRepository cardRepository,
        ICardStreamServerRepository cardStreamServerRepository)
        : CardStoreExtension
    {
        #region Fields

        private readonly ICardRepository cardRepository = NotNullOrThrow(cardRepository);

        private readonly ICardStreamServerRepository cardStreamServerRepository = NotNullOrThrow(cardStreamServerRepository);

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async Task BeforeRequestWhenTypeResolved(ICardStoreExtensionContext context)
        {
            // Получаем карточку из запроса.
            var card = context.Request.Card;
            if (card.TypeID == CardHelper.FileSatelliteTypeID)
            {
                return;
            }

            Card? fileSatellite = null;

            // Перебираем файлы в файловом контейнере для того, чтобы сохранить те, что относятся к задачам, в файловый сателлит.
            for (var i = card.Files.Count - 1; i >= 0; i--)
            {
                var file = card.Files[i];
                // Если у файла не определен TaskID, переходим к следующему.
                if (!file.TaskID.HasValue)
                {
                    continue;
                }

                // Если до сих пор не извлечен файловый сателлит для главной карточки, извлекаем его.
                if (fileSatellite is null)
                {
                    fileSatellite = await FileSatelliteHelper.GetFileSatelliteAsync(
                        context,
                        this.cardRepository);

                    // Если мы не можем получить файловый сателлит, то выходим из метода.
                    if (fileSatellite is null)
                    {
                        return;
                    }

                    context.Request.ForceTransaction = true;
                }

                // Добавляем файл в файловый сателлит.
                fileSatellite.Files.Add(file);

                // Настраиваем маппинг сохранения контента файла в карточку файлового сателлита
                if (file.State != CardFileState.Deleted)
                {
                    var fileMapping = context.Request.FileMapping.Add();
                    fileMapping.CardID = fileSatellite.ID;
                    fileMapping.CardTypeID = fileSatellite.TypeID;
                    fileMapping.FileID = file.RowID;
                    fileMapping.VersionRowID = file.VersionRowID;
                    fileMapping.SourceFileID = file.RowID;
                    fileMapping.StoreSource = file.StoreSource;
                }

                // Из главной карточки удаляем информацию по файлу.
                card.Files.RemoveAt(i);
            }
        }

        /// <inheritdoc/>
        public override Task BeforeCommitTransaction(ICardStoreExtensionContext context) =>
            context.ValidationResult.IsSuccessful()
                ? FileSatelliteHelper.StoreFileSatelliteAsync(context, this.cardStreamServerRepository)
                : Task.CompletedTask;

        #endregion
    }
}
