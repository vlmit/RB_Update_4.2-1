#nullable enable

using System;
using Tessa.Test.Default.Shared.Kr;
using Tessa.Workflow;
using Tessa.Workflow.Signals;

namespace Tessa.Test.Default.Shared.Workflow
{
    /// <summary>
    /// Предоставляет методы расширения для <see cref="CardLifecycleCompanion"/>, используемые в тестах WorkflowEngine.
    /// </summary>
    public static class CardLifecycleCompanionExtensions
    {
        #region CardLifecycleCompanion Extensions

        /// <summary>
        /// Планирует создание бизнес-процесса в карточке, которой управляет объект <paramref name="clc"/>.
        /// </summary>
        /// <param name="clc">Объект, управляющий жизненным циклов карточки, в которой должен быть запущен бизнес-процесс.</param>
        /// <param name="dependencies"><inheritdoc cref="IWeLifecycleCompanionDependencies" path="/summary"/></param>
        /// <param name="signal"><inheritdoc cref="IWorkflowEngineSignal" path="/summary"/></param>
        /// <param name="processTemplateID"><inheritdoc cref="WeProcessInstanceLifecycleCompanion.StartNew(IWorkflowEngineSignal, Guid?, Guid?, Guid?, WorkflowEngineProcessFlags)" path="/param[@name='processTemplateID']"/></param>
        /// <param name="processTemplateVersionID"><inheritdoc cref="WeProcessInstanceLifecycleCompanion.StartNew(IWorkflowEngineSignal, Guid?, Guid?, Guid?, WorkflowEngineProcessFlags)" path="/param[@name='processTemplateVersionID']"/></param>
        /// <param name="processInstanceID"><inheritdoc cref="WeProcessInstanceLifecycleCompanion.StartNew(IWorkflowEngineSignal, Guid?, Guid?, Guid?, WorkflowEngineProcessFlags)" path="/param[@name='processInstanceID']"/></param>
        /// <returns>Объект, управляющий жизненным циклом карточки, в которой запущен экземпляр бизнес-процесса.</returns>
        /// <remarks>
        /// <inheritdoc cref="WeProcessInstanceLifecycleCompanion.StartNew(IWorkflowEngineSignal, Guid?, Guid?, Guid?, WorkflowEngineProcessFlags)" path="/remarks"/>
        /// </remarks>
        public static WeProcessInstanceLifecycleCompanion CreateWorkflowEngineProcess(
            this CardLifecycleCompanion clc,
            IWeLifecycleCompanionDependencies dependencies,
            IWorkflowEngineSignal signal,
            Guid? processTemplateID = null,
            Guid? processTemplateVersionID = null,
            Guid? processInstanceID = null)
        {
            return new WeProcessInstanceLifecycleCompanion(
                    clc,
                    dependencies)
                .StartNew(
                    signal,
                    processTemplateID,
                    processTemplateVersionID,
                    processInstanceID);
        }

        /// <summary>
        /// Планирует создание бизнес-процесса в карточке, которой управляет объект <paramref name="clc"/>.
        /// </summary>
        /// <param name="clc">Объект, управляющий жизненным циклов карточки, в которой должен быть запущен бизнес-процесс.</param>
        /// <param name="dependencies"><inheritdoc cref="IWeLifecycleCompanionDependencies" path="/summary"/></param>
        /// <param name="signalType">Тип сигнала.</param>
        /// <param name="processTemplateID"><inheritdoc cref="WeProcessInstanceLifecycleCompanion.StartNew(IWorkflowEngineSignal, Guid?, Guid?, Guid?, WorkflowEngineProcessFlags)" path="/param[@name='processTemplateID']"/></param>
        /// <param name="processTemplateVersionID"><inheritdoc cref="WeProcessInstanceLifecycleCompanion.StartNew(IWorkflowEngineSignal, Guid?, Guid?, Guid?, WorkflowEngineProcessFlags)" path="/param[@name='processTemplateVersionID']"/></param>
        /// <param name="processInstanceID"><inheritdoc cref="WeProcessInstanceLifecycleCompanion.StartNew(IWorkflowEngineSignal, Guid?, Guid?, Guid?, WorkflowEngineProcessFlags)" path="/param[@name='processInstanceID']"/></param>
        /// <returns>Объект, управляющий жизненным циклом карточки, в которой запущен экземпляр бизнес-процесса.</returns>
        /// <remarks>
        /// <inheritdoc cref="WeProcessInstanceLifecycleCompanion.StartNew(IWorkflowEngineSignal, Guid?, Guid?, Guid?, WorkflowEngineProcessFlags)" path="/remarks"/>
        /// </remarks>
        public static WeProcessInstanceLifecycleCompanion CreateWorkflowEngineProcess(
            this CardLifecycleCompanion clc,
            IWeLifecycleCompanionDependencies dependencies,
            string signalType = WorkflowSignalTypes.Start,
            Guid? processTemplateID = null,
            Guid? processTemplateVersionID = null,
            Guid? processInstanceID = null)
        {
            return clc.CreateWorkflowEngineProcess(
                dependencies,
                new WorkflowEngineSignal(signalType),
                processTemplateID,
                processTemplateVersionID,
                processInstanceID);
        }

        #endregion
    }
}
