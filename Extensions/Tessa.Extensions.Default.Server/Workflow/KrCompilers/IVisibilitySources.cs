using Tessa.Extensions.Default.Shared.Workflow.KrCompilers;

namespace Tessa.Extensions.Default.Server.Workflow.KrCompilers
{
    /// <summary>
    /// Описывает объект, предоставляющий информацию, определяющую видимость.
    /// </summary>
    public interface IVisibilitySources :
        IKrHasSource
    {
        /// <summary>
        /// Возвращает текст SQL запроса с условием определяющим видимость.
        /// </summary>
        string VisibilitySqlCondition { get; }

        /// <summary>
        /// Возвращает C# код, определяющий видимость.
        /// </summary>
        string VisibilitySourceCondition { get; }
    }
}
