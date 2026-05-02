#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Caching;
using Tessa.Cards.Extensions;
using Tessa.Platform.Initialization;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.Cards.UserPalettesSettings
{
    /// <summary>
    /// Расширение, которое синхронизирует системные палитры с пользовательскими в карточке пользователя.
    /// Работает в паре с <see cref="UserPalettesSettingsGetExtension"/>.
    /// </summary>
    public class UserPalettesSettingsStoreExtension : CardStoreExtension
    {
        #region Fields

        private readonly ICardCache cardCache;

        #endregion

        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="UserPalettesSettingsStoreExtension"/>.
        /// </summary>
        /// <param name="cardCache"><inheritdoc cref="ICardCache" path="/summary"/></param>
        public UserPalettesSettingsStoreExtension(ICardCache cardCache)
        {
            this.cardCache = NotNullOrThrow(cardCache);
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc />
        public override async Task BeforeRequest(ICardStoreExtensionContext context)
        {
            if (context.Request!.TryGetCard() is not { } card)
            {
                return;
            }

            var insertedPalettes = card.Info
                .TryGet<IList>(UserPalettesSettingsExtensionHelper.InsertedCustomPalettesKey)?
                .Cast<Dictionary<string, object?>>()
                .ToList();
            var updatedPalettes = card.Info
                .TryGet<IList>(UserPalettesSettingsExtensionHelper.UpdatedCustomPalettesKey)?
                .Cast<Dictionary<string, object?>>()
                .ToList();
            var deletedPaletteIds = card.Info
                .TryGet<IList>(UserPalettesSettingsExtensionHelper.DeletedCustomPalettesKey)?
                .Cast<Guid>()
                .ToList();

            if (insertedPalettes is null && updatedPalettes is null && deletedPaletteIds is null)
            {
                return;
            }

            var serverInstance = await this.cardCache.Cards.GetAsync(CardHelper.ServerInstanceTypeName, context.CancellationToken);
            if (!serverInstance.IsSuccess ||
                !serverInstance.GetValue().Sections.TryGetValue("Palettes", out var section) ||
                (section is not { Rows.Count: > 0 }))
            {
                return;
            }

            var defaultPalettes = new List<PaletteInfo>();
            foreach (var row in section.Rows)
            {
                var palettes = new List<PaletteInfo>();
                var alias = row.Get<string>("Alias")!;
                var caption = row.Get<string>("Caption")!;
                var rowId = row.RowID;
                defaultPalettes.Add(new PaletteInfo(alias, caption, rowId));
            }

            // Получаем из инфо-секции данные о палитрах, которые сформировало расширение <see cref="UserPalettesSettingsGetExtension"/>.
            // 1) Для новых палитр нужно поставить статус <see cref="CardRowState.Inserted"/>.
            // 2) Для измененных палитр нужно прописать недостающие значения полей и поставить статус <see cref="CardRowState.Modified"/>, если требуется.
            // 3) Для удаленных палитр добавим строку со статусом = <see cref="CardRowState.Deleted"/>.

            var userPalettesVirtual = card.Sections.GetOrAddTable("UserPalettesVirtual");
            var rows = userPalettesVirtual.Rows;

            if (insertedPalettes is { Count: > 0 })
            {
                foreach (var insertedPalette in insertedPalettes)
                {
                    var rowId = insertedPalette.Get<Guid>("RowID");
                    var paletteRowId = insertedPalette.Get<Guid>("PaletteRowID");
                    var defaultPalette = defaultPalettes.FirstOrDefault(x => x.RowId == paletteRowId);
                    if (defaultPalette is null)
                    {
                        continue;
                    }
                    var insertedRow = rows.FirstOrDefault(x => x.RowID == rowId);
                    if (insertedRow is null)
                    {
                        insertedRow = rows.Add();
                        insertedRow.RowID = rowId;
                    }
                    insertedRow.State = CardRowState.Inserted;
                    insertedRow["PaletteAlias"] = defaultPalette.Alias;
                    insertedRow["PaletteCaption"] = defaultPalette.Caption;
                    insertedRow["PaletteRowID"] = paletteRowId;
                    for (int i = 1; i <= PaletteHelper.CustomColorsCount; i++)
                    {
                        insertedRow.TryAdd(PaletteHelper.GetColorFieldAlias(i), null);
                    }
                }
            }

            if (updatedPalettes is { Count: > 0 })
            {
                foreach (var updatedPalette in updatedPalettes)
                {
                    var rowId = updatedPalette.Get<Guid>("RowID");
                    var paletteRowId = updatedPalette.Get<Guid>("PaletteRowID");
                    var defaultPalette = defaultPalettes.FirstOrDefault(x => x.RowId == paletteRowId);
                    if (defaultPalette is null)
                    {
                        continue;
                    }
                    var updatedRow = rows.FirstOrDefault(x => x.RowID == rowId);
                    if (updatedRow is null)
                    {
                        updatedRow = rows.Add();
                        updatedRow.RowID = rowId;
                    }
                    updatedRow.State = CardRowState.Modified;
                    updatedRow["PaletteAlias"] = defaultPalette.Alias;
                    updatedRow["PaletteCaption"] = defaultPalette.Caption;
                    updatedRow["PaletteRowID"] = paletteRowId;
                }
            }

            if (deletedPaletteIds is { Count: > 0 })
            {
                foreach (var deletedPaletteId in deletedPaletteIds)
                {
                    var deletedRow = rows.Add();
                    deletedRow.RowID = deletedPaletteId;
                    deletedRow.State = CardRowState.Deleted;
                }
            }
        }

        #endregion
    }
}
