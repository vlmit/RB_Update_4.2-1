#nullable enable

using System;
using System.Threading.Tasks;
using Tessa.Workflow;

namespace Tessa.Test.Default.Shared.Workflow
{
    /// <inheritdoc cref="IWeInterprocessExecutor"/>
    public sealed class WeInterprocessExecutor(
        Func<IWorkflowEngineContext, object[], ValueTask> action) : IWeInterprocessExecutor
    {
        #region Fields

        private readonly Func<IWorkflowEngineContext, object[], ValueTask> action = NotNullOrThrow(action);

        #endregion

        #region IWeInterprocessExecutor Implementation

        /// <inheritdoc/>
        public ValueTask ExecuteAsync(IWorkflowEngineContext workflowEngineContext, params object[] additionalData)
        {
            return this.action.Invoke(workflowEngineContext, additionalData);
        }

        #endregion
    }
}
