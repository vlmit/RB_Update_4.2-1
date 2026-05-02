#nullable enable

using System;

namespace Tessa.Extensions.Default.Shared
{
    /// <summary>
    /// Предоставляет информацию о типах документов, задействованных в типовом решении.
    /// </summary>
    public static class DefaultDocTypes
    {
        #region Constants And Static Fields

        /// <summary>
        /// Идентификатор типа документа "Исходящий".
        /// </summary>
        public static readonly Guid OutgoingDocTypeID = new(0x13597b5f, 0xd25d, 0x4385, 0x9e, 0x36, 0x43, 0xf1, 0x52, 0x91, 0x47, 0x19);

        /// <summary>
        /// Название типа документа "Исходящий".
        /// </summary>
        public const string OutgoingDocTypeTitle = "$KrTypes_DocTypes_Outgoing";

        /// <summary>
        /// Идентификатор типа документа "Входящий".
        /// </summary>
        public static readonly Guid IncomingDocTypeID = new(0x93806539, 0x4931, 0x47ad, 0x9d, 0x68, 0x73, 0x87, 0x15, 0x6c, 0x41, 0x7f);

        /// <summary>
        /// Название типа документа "Входящий".
        /// </summary>
        public const string IncomingDocTypeTitle = "$KrTypes_DocTypes_Incoming";

        #endregion
    }
}
