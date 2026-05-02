#nullable enable
using System;

namespace Tessa.Extensions.Default.Server.Normalization
{
    /// <summary>
    /// Опции, связанные со справочниками нормализации. Применяются к справочникам, регистрируемым в типовом решении.
    /// </summary>
    public class DefaultNormalizationOptions
    {
        #region Properties

        /// <summary>
        /// Интервал, по истечению которого кэш в Redis отбрасывается и наполняется заново,
        /// или <c>null</c>, если кэш не отбрасывается по истечению времени.
        /// </summary>
        /// <remarks>
        /// Значение актуально для кэшей, конечные значения которых наполняются из базы данных или представлений.
        /// Если наполнение производится, например, из схемы или метаданных карточек, то соответствующие кэши не имеют истечения времени жизни.
        /// </remarks>
        public TimeSpan? RedisExpiry { get; set; } = DefaultRedisExpiry;

        /// <summary>
        /// Интервал, по истечению которого кэш в памяти процесса отбрасывается и наполняется заново,
        /// или <c>null</c>, если кэш не отбрасывается по истечению времени.
        /// </summary>
        /// <remarks>
        /// Значение актуально для кэшей, конечные значения которых наполняются из базы данных или представлений.
        /// Если наполнение производится, например, из схемы или метаданных карточек, то соответствующие кэши не имеют истечения времени жизни.
        /// </remarks>
        public TimeSpan? InMemoryExpiry { get; set; } = DefaultInMemoryExpiry;

        #endregion

        #region Static Fields

        /// <summary>
        /// Интервал по умолчанию для свойства <see cref="RedisExpiry"/>.
        /// </summary>
        public static readonly TimeSpan DefaultRedisExpiry = TimeSpan.FromDays(1.0);

        /// <summary>
        /// Интервал по умолчанию для свойства <see cref="InMemoryExpiry"/>.
        /// </summary>
        public static readonly TimeSpan DefaultInMemoryExpiry = TimeSpan.FromDays(0.5);

        #endregion
    }
}
