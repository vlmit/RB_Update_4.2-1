using System;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow
{
    /// <summary>
    /// Исключение, возникающее при прерывании выполнения <see cref="IKrProcessRunner"/>.
    /// </summary>
    public class ProcessRunnerInterruptedException: ApplicationException
    {
        #region constructors

        public ProcessRunnerInterruptedException()
            : base()
        {

        }

        public ProcessRunnerInterruptedException(
            string message)
            : base(message)
        {

        }

        public ProcessRunnerInterruptedException(
            string message,
            Exception innerException) 
            : base (message, innerException)
        {

        }

        #endregion
    }
}
