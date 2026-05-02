#nullable enable

using System;
using System.Linq;
using System.Threading.Tasks;
using Tessa.Cards.Caching;
using Tessa.Extensions.Default.Shared.Acquaintance;
using Tessa.Extensions.Default.Shared.Workflow.WorkflowEngine;
using Tessa.Normalization;
using Tessa.Platform.Validation;
using Tessa.Roles;
using Tessa.Workflow;
using Tessa.Workflow.Actions;
using Tessa.Workflow.Compilation;
using Tessa.Workflow.Helpful;
using Tessa.Workflow.Normalization;
using Unity;

namespace Tessa.Extensions.Default.Server.Workflow.WorkflowEngine
{
    /// <summary>
    /// Обработчик действия "Ознакомление".
    /// </summary>
    public sealed class KrAcquaintanceAction(
        [Dependency(KrAcquaintanceManagerNames.WithoutTransaction)] IKrAcquaintanceManager acquaintanceManager,
        IRoleGetStrategy roleGetStrategy,
        IContextRoleManager contextRoleManager,
        ICardContextRoleCache contextRoleCache)
        : WorkflowActionBase(KrDescriptors.AcquaintanceDescriptor)
    {
        #region Consts

        public const string MainActionSection = "KrAcquaintanceAction";
        public const string RolesSection = "KrAcquaintanceActionRoles";

        #endregion

        #region Fields

        private readonly IKrAcquaintanceManager acquaintanceManager = NotNullOrThrow(acquaintanceManager);
        private readonly IRoleGetStrategy roleGetStrategy = NotNullOrThrow(roleGetStrategy);
        private readonly IContextRoleManager contextRoleManager = NotNullOrThrow(contextRoleManager);
        private readonly ICardContextRoleCache contextRoleCache = NotNullOrThrow(contextRoleCache);

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(
            IWorkflowEngineContext context,
            IWorkflowEngineCompiled? scriptObject)
        {
            var roles = await context.GetAllRowsAsync(RolesSection);
            var mainCardID = context.ProcessInstance!.CardID;

            if (roles is not { Count: > 0 }
                || mainCardID == context.ProcessInstance.WorkflowCardID)
            {
                // Некому отправлять ознакомление или нет карточки для ознакомления
                return;
            }

            var roleIDs = roles.Select(x => WorkflowEngineHelper.Get<Guid>(x, WorkflowEngineHelper.WildCardHashMark, "ID")).ToList();
            var notificationID = await context.GetAsync<Guid?>(MainActionSection, "Notification", "ID");
            var excludeDeputies = await context.GetAsync<bool?>(MainActionSection, "ExcludeDeputies") ?? false;
            var comment = await context.GetAsync<string>(MainActionSection, "Comment");
            var placeholderAliases = await context.GetAsync<string>(MainActionSection, "AliasMetadata");
            var senderID = await context.GetAsync<Guid?>(MainActionSection, "Sender", "ID");

            if (senderID.HasValue)
            {
                var role = await this.roleGetStrategy.GetRoleParamsAsync(senderID.Value, cancellationToken: context.CancellationToken);
                if (role.Type is null)
                {
                    context.ValidationResult.AddError(this, "Sender role isn't found.");
                    return;
                }

                switch (role.Type)
                {
                    case RoleType.Personal:
                        // Do Nothing
                        break;

                    case RoleType.Context:
                        var contextRole = await this.contextRoleCache.GetContextRoleAsync(senderID.Value, context.CancellationToken);
                        var users = await this.contextRoleManager.GetCardContextUsersAsync(
                            contextRole, mainCardID, excludeUserNames: true, cancellationToken: context.CancellationToken);
                        senderID = users.Count > 0 ? users[0].UserID : null;
                        break;

                    default:
                        context.ValidationResult.AddError(this, "$KrProcess_Acquaintance_SenderShouldBePersonalOrContext");
                        return;
                }
            }

            var result = await this.acquaintanceManager.SendAsync(
                mainCardID,
                roleIDs,
                excludeDeputies,
                comment,
                true,
                placeholderAliases,
                null,
                notificationID,
                senderID,
                cancellationToken: context.CancellationToken);

            if (!result.IsSuccessful || result.HasWarnings)
            {
                context.ValidationResult.Add(result);
            }
        }

        /// <inheritdoc/>
        protected override WorkflowNormalizationSettings GetNormalizationSettings()
        {
            return new WorkflowNormalizationSettings(
            [
                new WorkflowNormalizationSetting(
                    PlatformNormalizationSources.Roles,
                    RolesSection,
                    "Role",
                    "Name",
                    true),
                new WorkflowNormalizationSetting(
                    PlatformNormalizationSources.Roles,
                    MainActionSection,
                    "Sender",
                    "Name")
            ]);
        }

        #endregion
    }
}
