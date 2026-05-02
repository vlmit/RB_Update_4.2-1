#nullable enable
using System;

namespace Tessa.Extensions.Default.Server.Normalization
{
    /// <summary>
    /// Справочники нормализации в типовом решении.
    /// </summary>
    /// <remarks>
    /// Имена и идентификаторы всех справочников также должны быть перечислены в системной таблице <c>NormalizationSources</c>.
    /// </remarks>
    public static class DefaultNormalizationSources
    {
        #region Static Fields

        /// <summary>
        /// Валюты.
        /// </summary>
        public static readonly Guid Currencies = new(0xc9f65066, 0xbf62, 0x4693, 0xb6, 0x12, 0x6b, 0x97, 0x56, 0x4d, 0xeb, 0x07); // C9F65066-BF62-4693-B612-6B97564DEB07

        /// <summary>
        /// Категории документов (протоколов, внутренних документов).
        /// </summary>
        public static readonly Guid DocumentCategories = new(0x7eef3af0, 0xfd2c, 0x4402, 0xb9, 0x00, 0x33, 0x94, 0x2e, 0xb3, 0xd5, 0xef); // 7EEF3AF0-FD2C-4402-B900-33942EB3D5EF

        /// <summary>
        /// Состояния документов (Проект, На согласовании, ...)
        /// </summary>
        public static readonly Guid KrDocStates = new(0xa9ed1258, 0xd9e3, 0x49e2, 0x8f, 0x5a, 0x54, 0xbe, 0x0a, 0x4a, 0xe3, 0xdd); // A9ED1258-D9E3-49E2-8F5A-54BE0A4AE3DD

        /// <summary>
        /// Типы карточек, включённых в типовое решение и не использующих типы документов (Контрагент, Протокол), а также типы документов (Служебная записка, Приказ),
        /// а также все прочие типы карточек.
        /// </summary>
        public static readonly Guid KrTypes = new(0x8f4430ab, 0x0530, 0x42e3, 0x90, 0x45, 0x54, 0xe3, 0xb3, 0x3b, 0xbb, 0x72); // 8F4430AB-0530-42E3-9045-54E3B33BBB72

        /// <summary>
        /// Контрагенты.
        /// </summary>
        public static readonly Guid Partners = new(0xf5656402, 0xa3ee, 0x4365, 0x90, 0x74, 0x98, 0xcf, 0x47, 0x2d, 0x77, 0x17); // F5656402-A3EE-4365-9074-98CF472D7717

        /// <summary>
        /// Виды заданий (Рекомендательное согласование).
        /// </summary>
        public static readonly Guid TaskKinds = new(0x50ad0f28, 0x2cc9, 0x4c68, 0xb0, 0xd4, 0xd3, 0xd8, 0x6e, 0x23, 0x46, 0xd6); // 50AD0F28-2CC9-4C68-B0D4-D3D86E2346D6

        #endregion
    }
}
