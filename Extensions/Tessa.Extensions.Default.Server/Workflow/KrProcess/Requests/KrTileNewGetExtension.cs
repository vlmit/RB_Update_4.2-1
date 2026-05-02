#nullable enable

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Shared;
using Tessa.Extensions.Default.Shared.Workflow.KrPermissions;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform;
using Tessa.Platform.Runtime;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Requests
{
    /// <summary>
    /// Вычисляем видимость плиток типового решения на сервере
    /// при создании карточек, добавленных в типовое решение.
    /// </summary>
    /// <remarks>
    /// Разрешения <see cref="Card.Permissions"/> рассчитываются в расширениях с порядком
    /// <see cref="ExtensionStage.BeforePlatform"/> и <see cref="ExtensionStage.Platform"/>.
    /// </remarks>
    public sealed class KrTileNewGetExtension :
        CardNewGetExtension
    {
        #region Constructors

        public KrTileNewGetExtension(
            IKrTypesCache krTypesCache,
            ISession session)
        {
            this.krTypesCache = NotNullOrThrow(krTypesCache);
            this.session = NotNullOrThrow(session);
        }

        #endregion

        #region Fields

        private readonly IKrTypesCache krTypesCache;

        private readonly ISession session;

        #endregion

        #region private

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Card? TryGetCard(
            CardResponseBase response)
        {
            switch (response)
            {
                case CardGetResponse cardGetResponse:
                    return cardGetResponse.Card;
                case CardNewResponse cardNewResponse:
                    return cardNewResponse.Card;
            }

            return null;
        }

        private async Task AfterRequestInternalAsync(
            CardType? cardType,
            Dictionary<string, object?>? requestInfo,
            CardResponseBase response,
            IValidationResultBuilder validationResult,
            bool cardIsNew,
            CardServiceType cardServiceType,
            CancellationToken cancellationToken = default)
        {
            if (!validationResult.IsSuccessful()
                || cardType is null
                || cardServiceType == CardServiceType.Default
                || (cardType.Flags & (CardTypeFlags.Administrative | CardTypeFlags.Singleton)) != 0
                || TryGetCard(response) is not { } card
                || await KrProcessSharedHelper.TryGetKrTypeAsync(this.krTypesCache, card, cardType.ID, cancellationToken: cancellationToken) is not { } krType)
            {
                return;
            }

            var state = await KrProcessSharedHelper.GetKrStateAsync(card, cancellationToken: cancellationToken) ?? KrState.Draft;

            var isUserAdministrator = this.session.User.IsAdministrator();
            var canShowHiddenStages =
                !cardIsNew
                && isUserAdministrator
                && !krType.UseRoutesInWorkflowEngine
                && card.HasHiddenStages();

            // если уже выполняется расчёт пермишенов кнопкой "Редактировать" CalculatePermissionsMark
            // или пермишены были вычислены ранее кнопкой "Редактировать" PermissionsCalculatedMark, то не показываем кнопку "Редактировать"

            var canEdit =
                !cardIsNew
                && requestInfo?.TryGet<bool>(KrPermissionsHelper.CalculatePermissionsMark) != true
                && requestInfo?.TryGet<bool>(KrPermissionsHelper.PermissionsCalculatedMark) != true
                && !card.Info.TryGet<bool>(KrPermissionsHelper.PermissionsCalculatedMark)
                && KrToken.TryGet(card.Info)?.HasPermission(KrPermissionFlagDescriptors.FullCardPermissionsGroup) != true;

            var useTasks = krType.UseResolutions && cardType.Flags.Has(CardTypeFlags.AllowTasks);

            var permissionsResolver = card.Permissions.CreateResolver();
            var canEditNumber = permissionsResolver.GetCardPermissions().Has(CardPermissionFlags.AllowEditNumber);
            var canReplaceNumber = state == KrState.Registered
                ? krType.AllowManualRegistrationDocNumberAssignment
                : krType.AllowManualRegularDocNumberAssignment;

            var canShowSkippedStages = !cardIsNew
                && !isUserAdministrator
                && !krType.UseRoutesInWorkflowEngine
                && card.HasSkipStages();

            card.SetTileIsVisible(
                DefaultTileNames.KrShowHiddenStages,
                canShowHiddenStages);

            card.SetTileIsVisible(
                DefaultTileNames.KrEditMode,
                canEdit);

            card.SetTileIsVisible(
                DefaultTileNames.WfCreateResolution,
                useTasks);

            card.SetTileIsVisible(
                ButtonNames.EditNumber,
                canEditNumber);

            card.SetTileIsVisible(
                ButtonNames.ReplaceNumber,
                canEditNumber && canReplaceNumber);

            card.SetTileIsVisible(
                DefaultTileNames.KrShowSkippedStages,
                canShowSkippedStages);
        }

        #endregion

        #region Base Overrides

        public override Task AfterRequest(ICardNewExtensionContext context) =>
            this.AfterRequestInternalAsync(
                context.CardType,
                context.Request.TryGetInfo(),
                context.Response!,
                context.ValidationResult,
                true,
                context.Request.ServiceType,
                context.CancellationToken);

        public override Task AfterRequest(ICardGetExtensionContext context) =>
            this.AfterRequestInternalAsync(
                context.CardType,
                context.Request.TryGetInfo(),
                context.Response!,
                context.ValidationResult,
                false,
                context.Request.ServiceType,
                context.CancellationToken);

        #endregion
    }
}

