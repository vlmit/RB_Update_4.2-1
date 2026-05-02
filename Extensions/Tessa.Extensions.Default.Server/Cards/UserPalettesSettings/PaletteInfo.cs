using System;

namespace Tessa.Extensions.Default.Server.Cards.UserPalettesSettings
{
    /// <summary>
    /// Информация о палитре.
    /// </summary>
    internal sealed class PaletteInfo
    {
        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="PaletteInfo"/>.
        /// </summary>
        /// <param name="alias"><inheritdoc cref="Alias" path="/summary"/></param>
        /// <param name="caption"><inheritdoc cref="Caption" path="/summary"/></param>
        /// <param name="rowId"><inheritdoc cref="RowId" path="/summary"/></param>
        public PaletteInfo(string alias, string caption, Guid rowId)
        {
            Alias = alias;
            Caption = caption;
            RowId = rowId;
        }

        /// <summary>
        /// Алиас палитры.
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// Заголовок палитры.
        /// </summary>
        public string Caption { get; set; }

        /// <summary>
        /// Идентификатор строки палитры в карточке настроек сервера.
        /// </summary>
        public Guid RowId { get; set; }

    }
}
