using Tessa.Localization;

namespace Tessa.Extensions.Default.Console.PrintJson
{
    /// <summary>
    /// Режимы работы команды "PrintJson".
    /// </summary>
    public enum PrintJsonMode
    {
        /// <summary>
        /// Режим работы, при котором учитывается возможность переопределения ключей.
        /// </summary>
        [LocalizableDescription("Common_CLI_PrintJsonMode_Config")]
        Config,

        /// <summary>
        /// Режим работы, при котором повторяющиеся ключи всегда перезаписываются.
        /// </summary>
        [LocalizableDescription("Common_CLI_PrintJsonMode_Theme")]
        Theme
    }
}
