using System;

namespace Tessa.Extensions.Default.Server.Workflow.KrCompilers
{
    public class ScriptExecutionException : ExecutionExceptionBase
    {
        public ScriptExecutionException(string errorMessageText, string sourceText)
            : base(errorMessageText, sourceText)
        {
        }

        public ScriptExecutionException(string errorMessageText, string sourceText, Exception innerException)
            : base(errorMessageText, sourceText, innerException)
        {
        }
    }
}
