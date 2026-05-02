using System;

namespace Tessa.Extensions.Default.Server.Workflow.KrCompilers
{
    public abstract class ExecutionExceptionBase: Exception
    {
        protected ExecutionExceptionBase(string errorMessageText, string sourceText)
            : base()
        {
            this.ErrorMessageText = errorMessageText;
            this.SourceText = sourceText;
        }

        protected ExecutionExceptionBase(string errorMessageText, string sourceText, Exception innerException)
            : base(string.Empty, innerException)
        {
            this.ErrorMessageText = errorMessageText;
            this.SourceText = sourceText;
        }

        public string ErrorMessageText { get; }

        public string SourceText { get; }
    }
}
