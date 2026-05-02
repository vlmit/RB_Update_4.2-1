#nullable enable

using System;
using System.Threading.Tasks;
using Tessa.Notices.Extensions;

namespace Tessa.Extensions.Default.Server.Notices.MobileApproval
{
    /// <summary>
    /// Расширение для процесса отправки уведомления мобильного согласования,
    /// выполняющее удаление блока с вариантами завершения, если для пользователя,
    /// которому отправляется уведомление, мобильное согласование отключено.
    /// </summary>
    public sealed class FinalizeMobileApprovalNotificationSendExtension :
        MobileApprovalNotificationSendExtensionBase
    {
        #region Base Overrides

        /// <inheritdoc/>
        public override Task AfterFormingNotificationForUser(
            INotificationSendExtensionContext context)
        {
            if (context.CurrentRecipient.HasMobileApproval)
            {
                return Task.CompletedTask;
            }

            // Удаляем блок с вариантами завершения.
            var startIndex = NotNullOrThrow(context.Body).IndexOf(
                StartMailAttachmentsBlock,
                StringComparison.Ordinal);

            if (startIndex < 0)
            {
                return Task.CompletedTask;
            }

            var endIndex = context.Body.IndexOf(
                EndMailAttachmentsBlock,
                StringComparison.Ordinal);

            if (endIndex < 0)
            {
                return Task.CompletedTask;
            }

            context.Body = context.Body.Remove(
                startIndex,
                endIndex - startIndex + EndMailAttachmentsBlock.Length);

            return Task.CompletedTask;
        }

        #endregion
    }
}
