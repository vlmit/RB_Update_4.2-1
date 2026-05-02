#nullable enable

using System;
using System.Linq;
using System.Threading.Tasks;
using Tessa.Cards.Caching;
using Tessa.Extensions.Default.Shared.Acquaintance;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.Roles;
using Unity;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow.Handlers
{
    /// <summary>
    /// Обработчик этапа <see cref="StageTypeDescriptors.AcquaintanceDescriptor"/>.
    /// </summary>
    /// <param name="acquaintanceManager"><inheritdoc cref="AcquaintanceManager" path="/summary"/></param>
    /// <param name="roleGetStrategy"><inheritdoc cref="RoleGetStrategy" path="/summary"/></param>
    /// <param name="contextRoleManager"><inheritdoc cref="ContextRoleManager" path="/summary"/></param>
    /// <param name="contextRoleCache"><inheritdoc cref="ContextRoleCache" path="/summary"/></param>
    public class AcquaintanceStageTypeHandler(
        [Dependency(KrAcquaintanceManagerNames.WithoutTransaction)] IKrAcquaintanceManager acquaintanceManager,
        IRoleGetStrategy roleGetStrategy,
        IContextRoleManager contextRoleManager,
        ICardContextRoleCache contextRoleCache)
        : StageTypeHandlerBase
    {
        #region Protected Properties

        /// <inheritdoc cref="IKrAcquaintanceManager" path="/summary"/>
        protected IKrAcquaintanceManager AcquaintanceManager { get; } = NotNullOrThrow(acquaintanceManager);

        /// <inheritdoc cref="IRoleGetStrategy" path="/summary"/>
        protected IRoleGetStrategy RoleGetStrategy { get; } = NotNullOrThrow(roleGetStrategy);

        /// <inheritdoc cref="IContextRoleManager" path="/summary"/>
        protected IContextRoleManager ContextRoleManager { get; } = NotNullOrThrow(contextRoleManager);

        /// <inheritdoc cref="ICardContextRoleCache" path="/summary"/>
        protected ICardContextRoleCache ContextRoleCache { get; } = NotNullOrThrow(contextRoleCache);

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async Task<StageHandlerResult> HandleStageStartAsync(
            IStageTypeHandlerContext context)
        {
            if (context.Stage.Performers is not { Count: not 0 } performers)
            {
                // Некому отправлять ознакомление: считаем, что этап завершен
                return StageHandlerResult.CompleteResult;
            }

            var mainCardID = context.Stage.InfoStorage.TryGet<Guid?>("MainCardID") ?? context.MainCardID ?? Guid.Empty;
            if (mainCardID == Guid.Empty)
            {
                // Нет карточки для ознакомления: считаем, что этап завершен
                return StageHandlerResult.CompleteResult;
            }

            var roles = performers.Select(static x => x.PerformerID).ToList();
            var stageSettings = context.Stage.SettingsStorage;
            var notificationID = stageSettings.TryGet<Guid?>(KrConstants.KrAcquaintanceSettingsVirtual.NotificationID);
            var excludeDeputies = stageSettings.TryGet<bool?>(KrConstants.KrAcquaintanceSettingsVirtual.ExcludeDeputies) ?? false;
            var comment = stageSettings.TryGet<string>(KrConstants.KrAcquaintanceSettingsVirtual.Comment);
            var placeholderAliases = stageSettings.TryGet<string>(KrConstants.KrAcquaintanceSettingsVirtual.AliasMetadata);
            var senderID = stageSettings.TryGet<Guid?>(KrConstants.KrAcquaintanceSettingsVirtual.SenderID);

            if (senderID.HasValue)
            {
                var role = await this.RoleGetStrategy.GetRoleParamsAsync(
                    senderID.Value,
                    context.CancellationToken);

                if (role.Type is null)
                {
                    context.ValidationResult.AddError(this, "Sender role isn't found.");
                    return StageHandlerResult.EmptyResult;
                }

                switch (role.Type)
                {
                    case RoleType.Personal:
                        // Do Nothing
                        break;

                    case RoleType.Context:
                        var contextRole = await this.ContextRoleCache.GetContextRoleAsync(senderID.Value, context.CancellationToken);
                        var users = await this.ContextRoleManager.GetCardContextUsersAsync(
                            contextRole, mainCardID, excludeUserNames: true, cancellationToken: context.CancellationToken);
                        senderID = users.Count > 0 ? users[0].UserID : null;
                        break;

                    default:
                        context.ValidationResult.AddError(this, "$KrProcess_Acquaintance_SenderShouldBePersonalOrContext");
                        return StageHandlerResult.EmptyResult;
                }
            }

            var result = await this.AcquaintanceManager.SendAsync(
                mainCardID,
                roles,
                excludeDeputies,
                comment,
                true,
                placeholderAliases,
                null,
                notificationID,
                senderID,
                cancellationToken: context.CancellationToken);

            // при успешной отправке записывается текст вида "Ознакомление отправлено N сотрудникам",
            // его нет смысла отображать пользователю, который "продвинул" маршрут
            if (!result.IsSuccessful || result.HasWarnings)
            {
                context.ValidationResult.Add(result);
            }

            return StageHandlerResult.CompleteResult;
        }

        #endregion
    }
}
