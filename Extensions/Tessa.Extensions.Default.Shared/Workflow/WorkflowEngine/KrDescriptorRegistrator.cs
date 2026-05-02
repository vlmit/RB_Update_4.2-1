using Tessa.Platform;
using Tessa.Workflow.Actions.Descriptors;

namespace Tessa.Extensions.Default.Shared.Workflow.WorkflowEngine
{
    /// <summary>
    /// Регистрирует дескрипторы типовых действий в реестре дескрипторов действий процессов Workflow Engine.
    /// </summary>
    [Registrator(Tag = RegistratorTag.GroupForDefault | RegistratorTag.ClientConsole)]
    public sealed class KrDescriptorRegistrator :
        RegistratorBase
    {
        #region Base Overrides

        /// <inheritdoc/>
        public override void FinalizeRegistration()
        {
            var descriptorRegistry = this.UnityContainer.TryResolve<WorkflowActionDescriptorRegistry>();
            if (descriptorRegistry is not null)
            {
                foreach (var descriptor in KrDescriptors.All)
                {
                    descriptorRegistry.TryRegisterDescriptor(descriptor);
                }
            }
        }

        #endregion
    }
}
