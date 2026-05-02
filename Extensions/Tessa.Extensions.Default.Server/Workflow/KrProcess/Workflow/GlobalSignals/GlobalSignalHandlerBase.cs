#nullable enable

using System.Threading.Tasks;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow.GlobalSignals
{
    /// <summary>
    /// Базовая абстрактная реализация <see cref="IGlobalSignalHandler"/>.
    /// </summary>
    public abstract class GlobalSignalHandlerBase :
        IGlobalSignalHandler
    {
        #region IGlobalSignalHandler Members

        /// <inheritdoc />
        public virtual Task<IGlobalSignalHandlerResult> Handle(
            IGlobalSignalHandlerContext context) =>
            Task.FromResult(GlobalSignalHandlerResult.EmptyHandlerResult);

        #endregion
    }
}
