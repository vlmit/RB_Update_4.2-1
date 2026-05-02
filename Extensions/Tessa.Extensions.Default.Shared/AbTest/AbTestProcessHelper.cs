namespace Tessa.Extensions.Default.Shared.AbTest
{
    public static class AbTestProcessHelper
    {
        #region Process Name

        /// <summary>
        /// Имя процесса, используемое для его запуска.
        /// </summary>
        public const string ProcessName = "AbTestProcess";

        #endregion

        #region SubProcess Names

        /// <summary>
        /// Имя основного подпроцесса, который доступен один на карточку.
        /// </summary>
        public const string MainSubProcess = "Main";

        /// <summary>
        /// Имя подпроцесса "Подпроцесс 1", может запускаться несколько раз из основного подпроцесса <see cref="MainSubProcess"/>.
        /// </summary>
        public const string SubProcess1 = "Process1";

        /// <summary>
        /// Имя тестового сигнала. Отправляется на основной подпроцесс <see cref="MainSubProcess"/>.
        /// </summary>
        public const string TestSignal = "TestSignal";

        #endregion
    }
}
