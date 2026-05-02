#nullable enable
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Platform.Server.Cards;
using Tessa.Platform.Collections;

namespace Tessa.Extensions.Default.Server.AbTest
{
    /// <summary>
    /// Пример расширения, которое переносит файлы из карточки - файлового сателлита в карточки задач.
    /// </summary>
    public sealed class AbTaskFilesExampleGetExtension(ICardRepository cardRepository) : CardGetExtension
    {
        #region Fields

        private readonly ICardRepository cardRepository = NotNullOrThrow(cardRepository);

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async Task AfterRequest(ICardGetExtensionContext context)
        {
            // Получаем карточку из ответа.
            var card = context.Response!.Card;

            // Если в карточке нет задач, выходим из метода.
            if (card.Tasks.Count == 0)
            {
                return;
            }

            // Извлекаем файловый сателлит для главной карточки.
            var fileSatellite = await FileSatelliteHelper.GetFileSatelliteAsync(this.cardRepository, card.ID, context.ValidationResult);

            // Если мы не можем получить файловый сателлит, то выходим из метода.
            if (fileSatellite is null)
            {
                return;
            }

            // Получаем и затем перебираем список файлов, что относятся к задачам.
            var taskFiles = fileSatellite.Files;

            foreach (var taskFile in taskFiles)
            {
                if (taskFile.TaskID is null
                    || !card.Tasks.TryFirst(x => x.RowID == taskFile.TaskID, out var task))
                {
                    continue;
                }

                // Добавляем в файловый контейнер файл, который уже в UI-расширении будет перенесен в файловый контейнер задачи.
                var addedTaskFile = task.Card.Files.Add(taskFile);

                // В информации о файле указываем в качестве внешнего источника файловый сателлит.
                addedTaskFile.ExternalSource = new CardFileContentSource
                {
                    CardID = fileSatellite.ID,
                    FileID = addedTaskFile.RowID,
                    VersionRowID = addedTaskFile.VersionRowID,
                    Source = addedTaskFile.StoreSource,
                    CardTypeID = CardHelper.FileSatelliteTypeID
                };

                // Для карточки задачи, к которой прикреплен файл, выставим Version > 0.
                // Это необходимо для того, чтобы изменить способ сохранения карточки с CardStoreMode.Insert на CardStoreMode.Update,
                // и таким образом обеспечить работу логики, извлекающей по запросу список версий для прикрепленных файлов.
                if (task.Card.Version == 0)
                {
                    task.Card.Version = 1;
                }
            }
        }

        #endregion
    }
}
