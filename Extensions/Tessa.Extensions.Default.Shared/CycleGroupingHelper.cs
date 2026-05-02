#nullable enable

using System;
using System.Collections.Generic;
using Tessa.Cards;
using Tessa.Files;

namespace Tessa.Extensions.Default.Shared
{
    /// <summary>
    /// Предоставляет методы и константы, используемые при распределении файлов по циклам согласования.
    /// </summary>
    public static class CycleGroupingHelper
    {
        #region Constants And Static Fields

        /// <summary>
        /// Ключ, по которому в <see cref="IFileObject.Info"/>, содержится максимальный номер цикла согласования. Тип значения: <see cref="int"/>.
        /// </summary>
        public const string MaxCycleNumberKey = "KrMaxCycleNumber";

        /// <summary>
        /// Ключ, по которому в <see cref="CardInfoStorageObject.Info"/>, содержится распределение копий файлов по циклам согласования. Тип значения: <see cref="Dictionary{TKey, TValue}"/>, где TKey - <see cref="string"/> - строковое представление идентификатора файла, TValue - <see cref="object"/> - номер цикла согласования (<see cref="int"/>).
        /// </summary>
        public const string FilesByCyclesKey = "KrFilesByCycles";

        /// <summary>
        /// Ключ, по которому в <see cref="CardInfoStorageObject.Info"/>, содержится распределение версий файлов по циклам согласования. Тип значения: список хранилищ объектов <see cref="CycleGroupingFileInfo"/>: <see cref="List{T}"/>, где T - <see cref="Dictionary{TKey, TValue}"/>, где TKey - <see cref="string"/>, TValue - <see cref="object"/>.
        /// </summary>
        public const string FilesModifiedByCyclesKey = "KrFilesModifiedByCycles";

        /// <summary>
        /// Ключ, по которому в <see cref="IFileObject.Info"/>, содержится номер цикла согласования. Тип значения: <see cref="int"/>.
        /// </summary>
        public const string CycleIDKey = "KrCycleID";

        /// <summary>
        /// Ключ, по которому в <see cref="IFileObject.Info"/>, содержится порядок сортировки. Тип значения: <see cref="int"/>.
        /// </summary>
        public const string CycleOrderKey = "KrCycleorder";

        /// <summary>
        /// Ключ, по которому в <see cref="IFileObject.Info"/>, содержится имя пользователя, создавшего версию (изменившего файл). Тип значения: <see cref="string"/>.
        /// </summary>
        public const string CreatedByNameKey = "KrCreatedByName";

        /// <summary>
        /// Ключ, по которому в <see cref="IFileObject.Info"/>, содержится дата и время создания версии файла. Тип значения: <see cref="DateTime"/>.
        /// </summary>
        public const string CreatedKey = "KrCreated";

        /// <summary>
        /// Ключ, по которому содержится режим отображения группировки по циклам согласования. Тип значения: <see cref="Nullable{T}"/>, где T - <see cref="T:Tessa.Extensions.Default.Client.Files.CycleFilesMode"/>.
        /// </summary>
        /// <remarks>
        /// Ключ, в <see cref="T:Tessa.UI.Files.IFileControl"/>/<see cref="T:Tessa.UI.IUIContext.Info"/>/<see cref="T:Tessa.UI.Cards.CardModel.Info"/> для корректного сохранения выбранного режима отображения файлов в группировке по циклам.<br/>
        /// В <see cref="T:Tessa.UI.Files.IFileControl"/> и <see cref="T:Tessa.UI.Cards.CardModel.Info"/> - это режим - значение из справочника <see cref="T:Tessa.Extensions.Default.Client.Files.CycleFilesMode"/>.<br/>
        /// В <see cref="T:Tessa.UI.IUIContext.Info"/> - это Dictionary&lt;<see cref="string"/>, <see cref="T:Tessa.Extensions.Default.Client.Files.CycleGroupingMode"/>&gt;, где по алиасу контрола можно найти его режим, если у контрола есть алиас.
        /// </remarks>
        public const string CycleGroupingModeKey = "CycleGroupingMode";

        #endregion
    }
}
