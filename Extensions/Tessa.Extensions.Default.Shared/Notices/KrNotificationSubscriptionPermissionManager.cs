#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Extensions.Default.Shared.Workflow.KrPermissions;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Notices;
using Tessa.Platform.Runtime;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Shared.Notices
{
    ///<inheritdoc/>
    public class KrNotificationSubscriptionPermissionManager(ISession session, IKrTypesCache typesCache)
        : NotificationSubscriptionPermissionManager(session)
    {
        #region Fields

        private readonly ISession session = NotNullOrThrow(session);

        private readonly IKrTypesCache typesCache = NotNullOrThrow(typesCache);

        #endregion

        #region INotificationSubscriptionPermissionManager Implementation

        ///<inheritdoc/>
        public override async ValueTask<bool> CheckAccessAsync(
            Card card,
            IValidationResultBuilder? validationResult = null,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(card);

            Guid? typeID = card.TypeID;
            if (typeID == CardHelper.NotificationSubscriptionsTypeID)
            {
                typeID = card.Sections.GetOrAddEntry(Tessa.Notices.NotificationHelper.NotificationSubscriptionSettings).RawFields.TryGet<Guid?>("CardTypeID");
            }

            if (!typeID.HasValue
                || !await KrComponentsHelper.HasBaseAsync(typeID.Value, this.typesCache, cancellationToken))
            {
                return this.session.User.IsAdministrator();
            }

            var krToken = KrToken.TryGet(card.Info);
            var result = krToken != null && krToken.HasPermission(KrPermissionFlagDescriptors.SubscribeForNotifications);

            return result;
        }

        #endregion
    }
}
