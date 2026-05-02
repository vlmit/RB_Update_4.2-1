#nullable enable

using System;
using Tessa.Platform.Collections;
using Unity;

namespace Tessa.Extensions.Default.Shared.Workflow.KrProcess.Formatters
{
    /// <inheritdoc cref="IStageTypeFormatterContainer"/>
    /// <param name="unityContainer">Unity-контейнер.</param>
    public class StageTypeFormatterContainer(
        IUnityContainer unityContainer) :
        IStageTypeFormatterContainer
    {
        #region Nested Types

        private struct RegistrationItem
        {
            public StageTypeDescriptor Descriptor;
            public Type Type;
        }

        #endregion

        #region Fields

        private readonly IUnityContainer unityContainer = NotNullOrThrow(unityContainer);

        private readonly HashSet<Guid, RegistrationItem> items =
            new(p => p.Descriptor.ID);

        #endregion

        #region IStageTypeFormatterContainer Members

        /// <inheritdoc />
        public IStageTypeFormatterContainer RegisterFormatter<T>(
            StageTypeDescriptor descriptor)
            where T : IStageTypeFormatter
        {
            if (!this.unityContainer.IsRegistered<T>())
            {
                throw new ArgumentException(
                    $"Type {typeof(T).FullName} is not registered in UnityContainer.{Environment.NewLine}" +
                    $"Add container.RegisterType<{nameof(IStageTypeFormatter)}, {typeof(T).Name}>() in your Registrator class.");
            }

            this.RegisterFormatterInternal(descriptor, typeof(T));
            return this;
        }

        /// <inheritdoc />
        public IStageTypeFormatterContainer RegisterFormatter(
            StageTypeDescriptor descriptor,
            Type formatterType)
        {
            ThrowIfNull(descriptor);
            ThrowIfNull(formatterType);

            if (!this.unityContainer.IsRegistered(formatterType))
            {
                throw new ArgumentException(
                    $"Type {formatterType.FullName} is not registered in UnityContainer.{Environment.NewLine}" +
                    $"Add container.RegisterType<{nameof(IStageTypeFormatter)}, {formatterType.Name}>() in your Registrator class.");
            }

            this.RegisterFormatterInternal(descriptor, formatterType);
            return this;
        }

        /// <inheritdoc />
        public IStageTypeFormatter? ResolveFormatter(Guid descriptorID)
        {
            if (this.items.TryGetItem(descriptorID, out var item))
            {
                return (IStageTypeFormatter) this.unityContainer.Resolve(item.Type);
            }

            return null;
        }

        #endregion

        #region Private Methods

        private void RegisterFormatterInternal(
            StageTypeDescriptor descriptor,
            Type t) =>
            this.items.Replace(new RegistrationItem
            {
                Descriptor = descriptor,
                Type = t,
            });

        #endregion
    }
}
