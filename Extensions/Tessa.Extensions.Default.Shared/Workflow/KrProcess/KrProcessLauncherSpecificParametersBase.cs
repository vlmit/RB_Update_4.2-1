#nullable enable

namespace Tessa.Extensions.Default.Shared.Workflow.KrProcess
{
    /// <summary>
    /// Базовая реализация <see cref="IKrProcessLauncherSpecificParameters"/>.
    /// </summary>
    public class KrProcessLauncherSpecificParametersBase :
        IKrProcessLauncherSpecificParameters
    {
        /// <inheritdoc/>
        public bool RaiseErrorWhenExecutionIsForbidden { get; set; }
    }
}
