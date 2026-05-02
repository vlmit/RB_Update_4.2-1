using System;

namespace Tessa.Extensions.Default.Server.Workflow.KrCompilers.SqlProcessing
{
    public class QueryExecutionException: ExecutionExceptionBase
    {
        public QueryExecutionException(string errorMessageText, string sourceText)
            : base(errorMessageText, sourceText)
        {
        }

        public QueryExecutionException(string errorMessageText, string sourceText, Exception innerException)
            : base(errorMessageText, sourceText, innerException)
        {
        }
    }
}
