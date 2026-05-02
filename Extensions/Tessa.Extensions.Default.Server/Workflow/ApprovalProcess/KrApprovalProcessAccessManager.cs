#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Extensions.Default.Server.Workflow.KrPermissions;
using Tessa.Extensions.Default.Shared.Workflow.KrPermissions;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform.Data;
using Tessa.Platform.Runtime;
using Tessa.Platform.Validation;
using Tessa.Workflow.ApprovalProcess;

namespace Tessa.Extensions.Default.Server.Workflow.ApprovalProcess
{
    /// <inheritdoc cref="IApprovalProcessAccessManager"/>
    /// <param name="dbScope"><inheritdoc cref="IDbScope" path="/summary"/></param>
    /// <param name="session"><inheritdoc cref="ISession" path="/summary"/></param>
    /// <param name="cardMetadata"><inheritdoc cref="ICardMetadata" path="/summary"/></param>
    /// <param name="cardRepository"><inheritdoc cref="ICardRepository" path="/summary"/></param>
    /// <param name="configurationInfoProvider"><inheritdoc cref="IConfigurationInfoProvider" path="/summary"/></param>
    /// <param name="instanceRepository"><inheritdoc cref="IApprovalProcessInstanceRepository" path="/summary"/></param>
    /// <param name="permissionsManager"><inheritdoc cref="IKrPermissionsManager" path="/summary"/></param>
    public sealed class KrApprovalProcessAccessManager(
        IDbScope dbScope,
        ISession session,
        ICardMetadata cardMetadata,
        ICardRepository cardRepository,
        IConfigurationInfoProvider configurationInfoProvider,
        IApprovalProcessInstanceRepository instanceRepository,
        IKrPermissionsManager permissionsManager) : ApprovalProcessAccessManager(dbScope, session, cardMetadata, cardRepository, configurationInfoProvider)
    {
        #region Fields

        private readonly IApprovalProcessInstanceRepository instanceRepository = NotNullOrThrow(instanceRepository);
        private readonly IKrPermissionsManager permissionsManager = NotNullOrThrow(permissionsManager);

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async ValueTask<ApprovalProcessInstanceAccessLevel> CheckInstanceAccessAsync(
            Guid instanceID,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            var parentCardID = await this.instanceRepository.GetInstanceParentCardIDAsync(instanceID, cancellationToken);
            if (parentCardID is null)
            {
                validationResult.AddError(
                    this,
                    "$ApprovalProcess_Errors_ProcessInstanceNotFound",
                    instanceID);

                return ApprovalProcessInstanceAccessLevel.None;
            }

            var contextCreationResult = await this.permissionsManager.TryCreateContextAsync(
                new KrPermissionsCreateContextParams
                {
                    CardID = parentCardID.Value,
                    ValidationResult = validationResult,
                },
                cancellationToken);

            if (contextCreationResult.Status != KrPermissionsCreateContextStatus.Success)
            {
                if (contextCreationResult.Status == KrPermissionsCreateContextStatus.NotAllowed)
                {
                    return await base.CheckInstanceAccessAsync(instanceID, validationResult, cancellationToken);
                }

                ValidationSequence
                    .Begin(validationResult)
                    .SetObjectName(typeof(KrApprovalProcessAccessManager))
                    .ErrorText("$KrMessages_PermissionManagerContextRequired")
                    .End();
                return ApprovalProcessInstanceAccessLevel.None;
            }

            var checkPermissionsResult = await this.permissionsManager.GetEffectivePermissionsAsync(
                contextCreationResult.Context,
                KrPermissionFlagDescriptors.ReadCard,
                KrPermissionFlagDescriptors.EditApprovalScheme);

            if (checkPermissionsResult.Has(KrPermissionFlagDescriptors.ReadCard))
            {
                return
                    checkPermissionsResult.Has(KrPermissionFlagDescriptors.EditApprovalScheme)
                        ? ApprovalProcessInstanceAccessLevel.Edit
                        : ApprovalProcessInstanceAccessLevel.Read;
            }

            return ApprovalProcessInstanceAccessLevel.None;
        }

        /// <inheritdoc/>
        public override async ValueTask<bool> CanCreateInstanceAsync(
            Guid cardID,
            IValidationResultBuilder? validationResult = null,
            CancellationToken cancellationToken = default)
        {
            var contextCreationResult = await this.permissionsManager.TryCreateContextAsync(
                new KrPermissionsCreateContextParams
                {
                    CardID = cardID,
                    ValidationResult = validationResult,
                },
                cancellationToken);

            return
                await this.CheckEditApprovalScheme(
                    contextCreationResult,
                    validationResult) ?? await base.CanCreateInstanceAsync(cardID, validationResult, cancellationToken);
        }

        /// <inheritdoc/>
        public override async ValueTask<bool> CanCreateInstanceAsync(
            Card card,
            IValidationResultBuilder? validationResult = null,
            CancellationToken cancellationToken = default)
        {
            KrToken? krToken = KrToken.TryGet(card.Info);

            var contextResult = await this.permissionsManager.TryCreateContextAsync(
                new KrPermissionsCreateContextParams
                {
                    Card = card,
                    ValidationResult = validationResult,
                    PrevToken = krToken
                },
                cancellationToken: cancellationToken);

            return
                await this.CheckEditApprovalScheme(
                    contextResult,
                    validationResult) ?? await base.CanCreateInstanceAsync(card, validationResult, cancellationToken);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Проверяет, что объект контекста проверки прав доступа был создан корректно и с помощью него проверяет наличие флага <see cref="KrPermissionFlagDescriptors.EditApprovalScheme"/>.
        /// </summary>
        /// <param name="contextCreationResult"><inheritdoc cref="KrPermissionsCreateContextResult" path="/summary"/></param>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <returns>Признак наличие флага <see cref="KrPermissionFlagDescriptors.EditApprovalScheme"/> в правилах доступа для проверяемого случая
        /// или <c>null</c>, если проверка типового решения не нужна.</returns>
        private async ValueTask<bool?> CheckEditApprovalScheme(
            KrPermissionsCreateContextResult contextCreationResult,
            IValidationResultBuilder? validationResult = null)
        {
            if (contextCreationResult.Status != KrPermissionsCreateContextStatus.Success)
            {
                if (contextCreationResult.Status == KrPermissionsCreateContextStatus.NotAllowed)
                {
                    return null;
                }

                ValidationSequence
                    .Begin(validationResult)
                    .SetObjectName(typeof(KrApprovalProcessAccessManager))
                    .ErrorText("$KrMessages_PermissionManagerContextRequired")
                    .End();
                return false;
            }

            var permissionsCheckResult = await this.permissionsManager.CheckRequiredPermissionsAsync(
                contextCreationResult.Context,
                KrPermissionFlagDescriptors.EditApprovalScheme);

            return permissionsCheckResult.Result;
        }

        #endregion
    }
}
