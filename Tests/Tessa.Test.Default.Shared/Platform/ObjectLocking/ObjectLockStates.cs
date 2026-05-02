#nullable enable

namespace Tessa.Test.Default.Shared.Platform.ObjectLocking
{
    /// <summary>
    /// Состояния блокировок объектов.
    /// </summary>
    public enum ObjectLockStates
    {
        /// <summary>
        /// Блокировка отсутствует.
        /// </summary>
        None,

        /// <summary>
        /// Получена блокировка на чтение.
        /// </summary>
        ReadLock,

        /// <summary>
        /// Получена блокировка на запись.
        /// </summary>
        WriteLock,

        /// <summary>
        /// Ожидание взятия блокировки на запись.
        /// </summary>
        WritePending,
    }
}
