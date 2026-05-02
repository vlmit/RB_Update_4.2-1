#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Notices;
using Tessa.Notices.Parameters;
using Tessa.Notices.Sources;
using Tessa.Platform.Data;
using Tessa.Platform.Validation;

namespace Tessa.Test.Default.Shared.Notices
{
    public sealed class TestNotificationManagerDecorator(
        INotificationManager decoratedNotificationManager,
        TestNotificationList notificationList,
        ICardMetadata cardMetadata,
        IDbScope dbScope,
        INotificationEmailSourceResolver emailSourceProvider,
        INotificationRecipientsSourceResolver recipientsSourceProvider)
        : NotificationManagerBase(cardMetadata, dbScope, emailSourceProvider, recipientsSourceProvider)
    {
        #region Base Overrides

        /// <inheritdoc/>
        public override async Task<ValidationResult> SendAsync(
            INotificationEmailSourceParameter emailSourceParameter,
            INotificationRecipientsSourceParameter recipientsSourceParameter,
            INotificationSendContext context,
            CancellationToken cancellationToken = default)
        {
            await base.SendAsync(emailSourceParameter, recipientsSourceParameter, context, cancellationToken);
            return await decoratedNotificationManager.SendAsync(emailSourceParameter, recipientsSourceParameter, context, cancellationToken);
        }

        /// <inheritdoc/>
        protected override Task<ValidationResult> SendCoreAsync(
            NotificationEmail notificationEmail,
            IReadOnlyList<NotificationRecipient> recipients,
            INotificationSendContext context,
            CancellationToken cancellationToken = default)
        {
            notificationList.Add(new(notificationEmail, recipients, context));
            return Task.FromResult(ValidationResult.Empty);
        }

        #endregion
    }
}
