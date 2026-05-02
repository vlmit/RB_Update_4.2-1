#nullable enable

using System;
using System.Text.RegularExpressions;

namespace Tessa.Extensions.Default.Server.Workflow.KrCompilers
{
    /// <inheritdoc cref="IKrPreprocessor"/>
    public class KrFunctionPreprocessor :
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

        #region Fields

        private bool isStatement = true;

        #endregion

        #region Private Methods

        private string OnMatching(Match match)
        {
            if (match.Groups[DirectiveGroup].Value == ExpressionDirective)
            {
                this.isStatement = true;
            }
            else if (match.Groups[DirectiveGroup].Value == ScriptDirective)
            {
                this.isStatement = false;
            }

            return string.Empty;
        }

        private string ReplaceDirectives(string sourceText) => Regex.Replace(
                sourceText,
                regexStr,
                this.OnMatching,
                RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Compiled);

        #endregion

        #region IKrPreprocessor Members

        /// <inheritdoc/>
        public string Preprocess(string source)
        {
            var newSource = this.ReplaceDirectives(source);
            return this.isStatement
                ? $"return {Environment.NewLine}{newSource}{Environment.NewLine};"
                : newSource;
        }

        #endregion
    }
}
