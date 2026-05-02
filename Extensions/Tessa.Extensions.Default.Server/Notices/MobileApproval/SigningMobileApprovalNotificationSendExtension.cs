#nullable enable

using System;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Caching;
using Tessa.Extensions.Default.Shared;
using Tessa.Extensions.Default.Shared.Notices;
using Tessa.Notices.Extensions;

namespace Tessa.Extensions.Default.Server.Notices.MobileApproval
{
    /// <summary>
    /// Расширение для процесса отправки уведомления мобильного согласования по заданию типа <see cref="DefaultTaskTypes.KrSigningTypeID"/>.
    /// </summary>
    /// <param name="cardCache"><inheritdoc cref="CardCache" path="/summary"/></param>
    public class SigningMobileApprovalNotificationSendExtension(ICardCache cardCache) : MobileApprovalNotificationSendExtensionBase
    {
        #region Properties

        /// <inheritdoc cref="ICardCache"/>
        protected ICardCache CardCache { get; } = NotNullOrThrow(cardCache);

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        protected override async ValueTask ReplaceOptionsForMobileApprovalAsync(
            INotificationSendExtensionContext context,
            CardTask task)
        {
            if (context.NotificationEmail.BodyTemplate is not { Length: not 0 } bodyTemplate
                || !bodyTemplate.Contains(OptionsForMobileApprovalPlaceholder, StringComparison.Ordinal))
            {
                return;
            }

            var mobileApprovalEmail = await DefaultNotificationHelper.GetMobileApprovalEmailAsync(this.CardCache, context.CancellationToken);

            context.NotificationEmail.BodyTemplate = bodyTemplate
                .Replace(
                    OptionsForMobileApprovalPlaceholder,
                    $"""
                     {StartMailAttachmentsBlock}
                     <br/>
                     <p>{CreateMobileApprovalLink(true, task.RowID, mobileApprovalEmail, "$KrMessages_SigningResultMessage", "{$KrMessages_SignLink}")}</p>
                     <p>{CreateMobileApprovalLink(false, task.RowID, mobileApprovalEmail, "$KrMessages_DecliningResultMessage", "{$KrMessages_DeclineLink}")}</p>
                     <br/>
                     {EndMailAttachmentsBlock}
                     """,
                    StringComparison.Ordinal);
        }

        #endregion
    }
}
