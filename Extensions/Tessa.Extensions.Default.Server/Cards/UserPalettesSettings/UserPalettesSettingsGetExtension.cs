#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Caching;
using Tessa.Cards.Extensions;

namespace Tessa.Extensions.Default.Server.Cards.UserPalettesSettings
{
    /// <summary>
    /// Расширение, которое синхронизирует системные палитры с пользовательскими в карточке пользователя.
    /// Работает в паре с <see cref="UserPalettesSettingsStoreExtension"/>.
    /// </summary>
    public class UserPalettesSettingsGetExtension : CardGetExtension
    {
        #region Private Fields

        private readonly ICardCache cardCache;

        #endregion

        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="UserPalettesSettingsGetExtension"/>.
        /// </summary>
        /// <param name="cardCache"><inheritdoc cref="ICardCache" path="/summary"/></param>
        public UserPalettesSettingsGetExtension(ICardCache cardCache)
        {
            this.cardCache = NotNullOrThrow(cardCache);
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc />
        public override async Task AfterRequest(ICardGetExtensionContext context)
        {
            if (context.RequestIsSuccessful && context.Response!.TryGetCard() is { } card)
            {
                // Получаем набор системных палитр из карточки настроек сервера, этот набор будет использован в алгоритме сверки.
                var serverSettingsValue = await this.cardCache.Cards.GetAsync(CardHelper.ServerInstanceTypeName, context.CancellationToken);
                if (!serverSettingsValue.IsSuccess)
                {
                    return;
                }

                var serverSettings = serverSettingsValue.GetValue();

                if (!serverSettings.Sections.TryGetValue("Palettes", out var section) ||
                    section.Rows is not { Count: > 0 } palettes)
                {
                    return;
                }

                var sourcePalettes = palettes
                    .Select(
                        x => new PaletteInfo(
                            x.Get<string>("Alias")!,
                            x.Get<string>("Caption")!,
                            x.Get<Guid>("RowID")
                        )
                    ).ToList();

                // Получаем пользовательские палитры.
                var userPalettes = card.Sections.GetOrAddTable("UserPalettesVirtual");

                IList<Guid>? deletedPalettes = null;
                IList<Dictionary<string, object?>>? updatedPalettes = null;

                // Набор палитр пользователя должен соответствовать системному набору. 
                for (int i = userPalettes.Rows.Count - 1; i >= 0; i--)
                {
                    var row = userPalettes.Rows[i];

                    var paletteRowId = row.Get<Guid>("PaletteRowID");
                    var paletteFromSource = sourcePalettes.FirstOrDefault(x => x.RowId == paletteRowId);

                    // Если в системном наборе палитра не найдена, удаляем ее.
                    if (paletteFromSource is null)
                    {
                        deletedPalettes ??= new List<Guid>();
                        deletedPalettes.Add(row.RowID);
                        userPalettes.Rows.RemoveAt(i);
                        continue;
                    }

                    // Если палитра найдена, но имеет другое имя или другой алиас, то исправим значения на дефолтные.
                    var caption = row.Get<string>("PaletteCaption");
                    var alias = row.Get<string>("PaletteAlias");
                    if (!paletteFromSource.Caption.Equals(caption, StringComparison.Ordinal) ||
                        !paletteFromSource.Alias.Equals(alias, StringComparison.Ordinal))
                    {
                        row["PaletteCaption"] = paletteFromSource.Caption;
                        row["PaletteAlias"] = paletteFromSource.Alias;
                        updatedPalettes ??= new List<Dictionary<string, object?>>();
                        updatedPalettes.Add(new Dictionary<string, object?>
                        {
                            { "RowID", row.RowID },
                            { "PaletteRowID", paletteFromSource.RowId }
                        });
                    }

                    // Удаляем из исходного набора найденную в пользовательских настройках палитру.
                    // Если в одной из следующих итераций попадется палитра с тем же алиасом (задвоенная),
                    // то она не будет найдена в исходном наборе и будет удалена.

                    sourcePalettes.Remove(paletteFromSource);
                }

                // Если каких-то палитр не было у пользователя, добавляем их.
                // Для этого обходим оставшиеся после сверки палитры из исходного списка.

                IList<Dictionary<string, object?>>? insertedPalettes = null;

                foreach (var sourcePalette in sourcePalettes)
                {
                    var newRow = userPalettes.Rows.Add();
                    newRow.RowID = Guid.NewGuid();
                    newRow["PaletteAlias"] = sourcePalette.Alias;
                    newRow["PaletteCaption"] = sourcePalette.Caption;
                    newRow["PaletteRowID"] = sourcePalette.RowId;
                    newRow["Color1"] = null;
                    newRow["Color2"] = null;
                    newRow["Color3"] = null;
                    newRow["Color4"] = null;
                    insertedPalettes ??= new List<Dictionary<string, object?>>();
                    insertedPalettes.Add(new Dictionary<string, object?>
                    {
                        { "RowID", newRow.RowID },
                        { "PaletteRowID", sourcePalette.RowId }
                    });
                }

                // Сохраняем список добавленных, удаленных и измененных строк в Info карточки
                // для дальнейшней обработки в <see cref="UserPalettesSettingsStoreExtension"/>.
                if (insertedPalettes is not null)
                {
                    card.Info[UserPalettesSettingsExtensionHelper.InsertedCustomPalettesKey] = insertedPalettes;
                }

                if (deletedPalettes is not null)
                {
                    card.Info[UserPalettesSettingsExtensionHelper.DeletedCustomPalettesKey] = deletedPalettes;
                }

                if (updatedPalettes is not null)
                {
                    card.Info[UserPalettesSettingsExtensionHelper.UpdatedCustomPalettesKey] = updatedPalettes;
                }
            }
        }

        #endregion
    }
}
