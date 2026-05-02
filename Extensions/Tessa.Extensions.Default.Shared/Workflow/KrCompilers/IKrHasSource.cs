#nullable enable

namespace Tessa.Extensions.Default.Shared.Workflow.KrCompilers
{
    /// <summary>
    /// Описывает объект, сообщающий о наличии исходного кода, требующего компиляции.
    /// </summary>
    public interface IKrHasSource
    {
        /// <summary>
        /// Показывает, задан ли исходный код, требующий компиляции.
        /// </summary>
        bool HasSource { get; }
    }
}
