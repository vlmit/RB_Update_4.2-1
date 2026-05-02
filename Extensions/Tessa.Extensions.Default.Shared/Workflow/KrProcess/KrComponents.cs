using System;

namespace Tessa.Extensions.Default.Shared.Workflow.KrProcess
{
    /// <summary>
    /// Компоненты типового решения.
    /// </summary>
    [Flags]
    public enum KrComponents
    {
        /// <summary>
        /// Типовое решение не используется (для типа).
        /// </summary>
        None = 0x0,

        /// <summary>
        /// Используется типовое решение.
        /// </summary>
        Base = 0x1,

        /// <summary>
        /// Используются типы документов.
        /// </summary>
        DocTypes = 0x2,

        /// <summary>
        /// Используется согласование.
        /// </summary>
        Routes = 0x4,

        /// <summary>
        /// Используется регистрация.
        /// </summary>
        Registration = 0x8,

        /// <summary>
        /// Используется типовой процесс отправки задач.
        /// </summary>
        Resolutions = 0x10,

        /// <summary>
        /// Используется система форумов.
        /// </summary>
        UseForum = 0x20,
    }
}
