#nullable enable

using System.Collections.Generic;
using Tessa.Notices;

namespace Tessa.Test.Default.Shared.Notices
{
    public sealed record TestNotificationListItem(
        NotificationEmail NotificationEmail,
        IReadOnlyList<NotificationRecipient> Recipients,
        INotificationSendContext Context);
}
