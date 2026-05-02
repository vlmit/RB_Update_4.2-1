#nullable enable

using System;
using Tessa.Extensions.Default.Shared.Workflow.KrCompilers;

namespace Tessa.Extensions.Default.Server.Workflow.KrCompilers
{
    /// <summary>
    /// Объект, представляющий метод-расширения.
    /// </summary>
    public interface IKrCommonMethod :
        IKrHasSource
    {
        /// <summary>
        /// Уникальный идентификатор карточки KrCommonMethod.
        /// </summary>
        Guid ID { get; }

        /// <summary>
        /// Имя метода, подставляемое в генерируемом коде.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Тело метода, подставляемое в генерируемый код.
        /// </summary>
        string Source { get; }
    }
}
