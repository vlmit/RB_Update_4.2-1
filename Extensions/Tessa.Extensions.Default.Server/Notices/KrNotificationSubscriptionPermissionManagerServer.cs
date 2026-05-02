#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.ComponentModel;
using Tessa.Extensions.Default.Server.Workflow.KrPermissions;
using Tessa.Extensions.Default.Server.Workflow.KrProcess;
using Tessa.Extensions.Default.Shared.Notices;
using Tessa.Extensions.Default.Shared.Workflow.KrPermissions;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform.Data;
using Tessa.Platform.Runtime;
using Tessa.Platform.Validation;
using Tessa.Scheme;

namespace Tessa.Extensions.Default.Server.Notices
{
    ///<inheritdoc/>
    public sealed class KrNotificationSubscriptionPermissionManagerServer(
        ISession session,
        IKrTypesCache typesCache,
        IDbScope dbScope,
        IKrTokenProvider krTokenProvider,
        IKrPermissionsManager permissionsManager,
        IKrPermissionsCacheContainer permissionsCacheContainer)
        : KrNotificationSubscriptionPermissionManager(session, typesCache)
    {
        #region Fields

        private readonly IDbScope dbScope = NotNullOrThrow(dbScope);
        private readonly IKrTokenProvider krTokenProvider = NotNullOrThrow(krTokenProvider);
        private readonly IKrPermissionsManager permissionsManager = NotNullOrThrow(permissionsManager);
        private readonly IKrPermissionsCacheContainer permissionsCacheContainer = NotNullOrThrow(permissionsCacheContainer);

        private static readonly KrPermissionFlagDescriptor[] notificationPermissions
            = [KrPermissionFlagDescriptors.SubscribeForNotifications];

        #endregion

        #region Base Overrides

        ///<inheritdoc/>
        public override async ValueTask<bool> CheckAccessAsync(
            Guid cardID,
            IValidationResultBuilder? validationResult = null,
            CancellationToken cancellationToken = default)
        {
            var permContextResult = await this.permissionsManager.TryCreateContextAsync(
                new()
                {
                    CardID = cardID,
                    ValidationResult = validationResult
                },
                cancellationToken);

            return permContextResult.Status switch
            {
                KrPermissionsCreateContextStatus.Success =>
                    await this.permissionsManager.CheckRequiredPermissionsAsync(
                        permContextResult.Context,
                        notificationPermissions),
                KrPermissionsCreateContextStatus.Fail => false,
                KrPermissionsCreateContextStatus.NotAllowed => await base.CheckAccessAsync(cardID, validationResult, cancellationToken),
                _ => throw ArgumentOutOfRange(permContextResult.Status)
            };
        }

        ///<inheritdoc/>
        public override async ValueTask<bool> CheckAccessAsync(
            Card card,
            IValidationResultBuilder? validationResult = null,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(card);

            var permContextResult = await this.permissionsManager.TryCreateContextAsync(
                new()
                {
                    Card = card,
                    CardTypeID = await this.GetCardTypeIDAsync(card.ID, cancellationToken),
                    ValidationResult = validationResult,
                    PrevToken = KrToken.TryGet(card.Info)
                },
                cancellationToken);

            return permContextResult.Status switch
            {
                KrPermissionsCreateContextStatus.Success =>
                    await this.permissionsManager.CheckRequiredPermissionsAsync(
                        permContextResult.Context,
                        notificationPermissions),
                KrPermissionsCreateContextStatus.Fail => false,
                KrPermissionsCreateContextStatus.NotAllowed => await base.CheckAccessAsync(card, validationResult, cancellationToken),
                _ => throw ArgumentOutOfRange(permContextResult.Status)
            };
        }

        ///<inheritdoc/>
        public override async ValueTask SetAccessAsync(Card card, CancellationToken cancellationToken = default)
        {
            ThrowIfNull(card);

            this.krTokenProvider
                .CreateToken(
                    card.ID,
                    CardComponentHelper.DoNotCheckVersion,
                    await this.permissionsCacheContainer.GetVersionAsync(cancellationToken),
                    notificationPermissions)
                .Set(card.Info);
        }

        #endregion

        #region Private Methods

        private async Task<Guid> GetCardTypeIDAsync(Guid cardID, CancellationToken cancellationToken = default)
        {
            await using (this.dbScope.Create())
            {
                var query = this.dbScope.BuilderFactory
                    .Select().C("TypeID")
                    .From(Names.Instances).NoLock()
                    .Where().C("ID").Equals().P("CardID")
                    .Build();
                return await this.dbScope.Db.SetCommand(
                        query,
                        this.dbScope.Db.Parameter("CardID", cardID))
                    .LogCommand()
                    .ExecuteAsync<Guid>(cancellationToken);
            }
        }

        #endregion
    }
}
