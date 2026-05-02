#nullable enable

using System.Text.RegularExpressions;

namespace Tessa.Extensions.Default.Server.Workflow.KrCompilers
{
    /// <inheritdoc cref="IKrPreprocessor"/>
    public class KrProcedurePreprocessor :
        IKrPreprocessor
    {
        #region Constants And Static Fields

        // Директивы, которые необходимо искать в коде
        private const string ExpressionDirective = "expression";
        private const string ScriptDirective = "script";

        // Для регулярки
        private const string DirectiveGroup = "dir";
        private static readonly string regexStr =
            $@"^\s*#(?<{DirectiveGroup}>{ExpressionDirective}|{ScriptDirective})(;|\b)";

        #endregion

        #region Private Methods

        private static string ReplaceDirectives(string sourceText) =>
            Regex.Replace(
                sourceText,
                regexStr,
                m => string.Empty,
                RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Compiled);

        #endregion

        #region IKrPreprocessor Members

        /// <inheritdoc/>
        public string Preprocess(string source) => ReplaceDirectives(source);

        #endregion
    }
}
