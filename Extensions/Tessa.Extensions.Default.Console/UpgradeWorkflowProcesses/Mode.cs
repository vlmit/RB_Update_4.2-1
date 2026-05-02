using Tessa.Localization;

namespace Tessa.Extensions.Default.Console.UpgradeWorkflowProcesses
{
    /// <summary>
    /// Режим выполнения команды.
    /// </summary>
    public enum Mode
    {
        /// <summary>
        /// Выполняется обновление только шаблонов.
        /// </summary>
        [LocalizableDescription("Common_CLI_UpgradeWorkflowProcesses_Template")]
        Template,

        /// <summary>
        /// Выполняется обновление только экземпляров.
        /// </summary>
        [LocalizableDescription("Common_CLI_UpgradeWorkflowProcesses_Instance")]
        Instance,

        /// <summary>
        /// Выполняется обновление как шаблонов так и экземпляров. Значение по умочланию.
        /// </summary>
        [LocalizableDescription("Common_CLI_UpgradeWorkflowProcesses_All")]
        All,
    }
}
