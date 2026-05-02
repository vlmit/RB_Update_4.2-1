#nullable enable
using System;

namespace Tessa.Extensions.Default.Server.RefGroups
{
    /// <summary>
    /// Методы и константы для работы с группами ссылок.
    /// </summary>
    public class KrRefGroupsHelper
    {
        #region RefGroup Types 

        /// <summary>
        /// Идентификатор типа группы ссылок "Состояние документа". {EEA01655-354B-41BF-87A8-F0D4D5B85506}
        /// </summary>
        public static readonly Guid KrStateRefGroupTypeID =
            new(0xeea01655, 0x354b, 0x41bf, 0x87, 0xa8, 0xf0, 0xd4, 0xd5, 0xb8, 0x55, 0x06); // EEA01655-354B-41BF-87A8-F0D4D5B85506

        #endregion
    }
}
