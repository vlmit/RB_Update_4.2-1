using System;

namespace Tessa.Extensions.Default.Shared.Workflow.KrProcess
{
    /// <summary>
    /// Представляет ошибки, происходящие при инициализации дескриптора типа этапа.
    /// </summary>
    public class StageTypeDescriptorNotInitializedException : Exception
    {
        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="StageTypeDescriptorNotInitializedException"/>.
        /// </summary>
        /// <param name="descriptorID">Идентификатор дескриптора типа этапа.</param>
        /// <param name="caption">Название типа этапа.</param>
        /// <param name="notInitializedField">Имя не инициализированного поля дескриптора типа этапа.</param>
        public StageTypeDescriptorNotInitializedException(
                Guid descriptorID,
                string caption,
                string notInitializedField)
            : base($"Descriptor ID = {descriptorID:B}, caption = \"{caption}\" has not initialized field \"{notInitializedField}\".")
        {
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="StageTypeDescriptorNotInitializedException"/>.
        /// </summary>
        /// <param name="message">Сообщение, описывающее ошибку.</param>
        public StageTypeDescriptorNotInitializedException(
            string message)
            : base(message)
        {

        }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="StageTypeDescriptorNotInitializedException"/>.
        /// </summary>
        /// <param name="message">Сообщение, описывающее ошибку.</param>
        /// <param name="innerException">Исключение, вызвавшее текущее исключение, или пустая ссылка, если внутреннее исключение не задано.</param>
        public StageTypeDescriptorNotInitializedException(
            string message,
            Exception innerException)
            : base(message, innerException)
        {

        }

        #endregion

    }
}
