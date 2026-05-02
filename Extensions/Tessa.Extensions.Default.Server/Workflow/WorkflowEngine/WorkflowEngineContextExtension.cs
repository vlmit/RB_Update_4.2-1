#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Caching;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow.Handlers;
using Tessa.Extensions.Default.Shared;
using Tessa.Extensions.Default.Shared.Workflow;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.Roles;
using Tessa.Workflow;

namespace Tessa.Extensions.Default.Server.Workflow.WorkflowEngine
{
    /// <summary>
    /// Предоставляет методы расширения для <see cref="IWorkflowEngineContext"/>.
    /// </summary>
    public static class WorkflowEngineContextExtension
    {
        /// <summary>
        /// Возвращает карточку основного сателлита <see cref="DefaultCardTypes.KrSatelliteTypeID"/>.
        /// </summary>
        /// <param name="context">Контекст обработки процесса.</param>
        /// <returns>Карточка сателлита или значение <see langword="null"/>, если произошла ошибка.</returns>
        /// <remarks>Если карточка сателлита не существует, то она автоматически создаётся.</remarks>
        public static async ValueTask<Card?> GetKrSatelliteAsync(
            this IWorkflowEngineContext context)
        {
            ThrowIfNull(context);

            return await context.GetKrSatelliteAsync(
                context.ValidationResult,
                context.CancellationToken);
        }

        /// <summary>
        /// Возвращает карточку основного сателлита <see cref="DefaultCardTypes.KrSatelliteTypeID"/>.
        /// </summary>
        /// <param name="context">Контекст обработки процесса.</param>
        /// <returns>Карточка сателлита или значение <see langword="null"/>, если произошла ошибка.</returns>
        /// <remarks>Если карточка сателлита не существует, то она автоматически создаётся.</remarks>
        public static async ValueTask<Card?> GetKrSatelliteAsync(
            this IWorkflowEngineContext context,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(context);
            ThrowIfNull(validationResult);

            return await context.CardsScope.GetSatelliteAsync(
                NotNullOrThrow(context.ProcessInstance).CardID,
                null,
                DefaultCardTypes.KrSatelliteTypeID,
                validationResult,
                cancellationToken);
        }

        /// <summary>
        /// Возвращает идентификатор роли автора задания.
        /// </summary>
        /// <param name="context">Контекст обработки процесса в WorkflowEngine.</param>
        /// <param name="roleGetStrategy">Стратегия для получения информации о ролях.</param>
        /// <param name="contextRoleManager">Обработчик контекстных ролей.</param>
        /// <param name="contextRoleCache"><inheritdoc cref="ICardContextRoleCache" path="/summary"/></param>
        /// <param name="roleID">Идентификатор роли заданный в параметрах действия.</param>
        /// <returns>Идентификатор роли автора задания или значение по умолчанию для типа, если при обработке возникли ошибки.</returns>
        public static async Task<Guid?> GetAuthorIDAsync(
            this IWorkflowEngineContext context,
            IRoleGetStrategy roleGetStrategy,
            IContextRoleManager contextRoleManager,
            ICardContextRoleCache contextRoleCache,
            Guid? roleID)
        {
            ThrowIfNull(context);
            ThrowIfNull(context.ProcessInstance);
            ThrowIfNull(roleGetStrategy);
            ThrowIfNull(contextRoleManager);
            ThrowIfNull(contextRoleCache);

            if (roleID.HasValue)
            {
                var (resultRoleID, _) = await WorkflowCommonHelper.TryGetPersonalRoleIDAsync(
                    roleID.Value,
                    context.ProcessInstance.CardID,
                    roleGetStrategy,
                    contextRoleManager,
                    contextRoleCache,
                    context.ValidationResult,
                    context.CancellationToken);

                if (resultRoleID.HasValue)
                {
                    return resultRoleID;
                }

                context.ValidationResult.AddError(
                    nameof(WorkflowEngineContextExtension),
                    "$KrProcess_ErrorMessage_OnlyPersonalAndContextRoles");
            }
            else
            {
                var sCard = await context.GetKrSatelliteAsync();

                if (sCard is not null)
                {
                    return sCard.GetApprovalInfoSection().Fields.TryGet<Guid?>(KrConstants.KrApprovalCommonInfo.AuthorID) ?? context.Session.User.ID;
                }
            }

            return null;
        }

        /// <summary>
        /// Добавляет указанный идентификатор задания в список активных заданий.
        /// </summary>
        /// <param name="context">Контекст обработки процесса.</param>
        /// <param name="taskID">Идентификатор задания.</param>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Асинхронная задача.</returns>
        public static async ValueTask AddActiveTaskAsync(
            this IWorkflowEngineContext context,
            Guid taskID,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(context);

            var sCard = await context.GetKrSatelliteAsync(
                validationResult,
                cancellationToken);

            if (sCard is null)
            {
                return;
            }

            var activeTasksSection = sCard.GetActiveTasksSection();

            if (activeTasksSection.Rows.Any(p => taskID.Equals(p.Fields[KrConstants.KrActiveTasks.TaskID])))
            {
                throw new InvalidOperationException($"Task with id \"{taskID:B}\" is already active.");
            }

            var row = activeTasksSection.Rows.Add();
            row.State = CardRowState.Inserted;
            row.RowID = Guid.NewGuid();
            row.Fields[KrConstants.KrActiveTasks.TaskID] = taskID;
        }

        /// <summary>
        /// Удаляет указанный идентификатор задания из списка активных заданий.
        /// </summary>
        /// <param name="context">Контекст обработки процесса.</param>
        /// <param name="taskID">Идентификатор задания.</param>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Значение <see langword="true"/>, если идентификатор задания успешно удалён из списка активных заданий, иначе - <see langword="false"/>.</returns>
        public static async ValueTask<bool> TryRemoveActiveTaskAsync(
            this IWorkflowEngineContext context,
            Guid taskID,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(context);

            var sCard = await context.GetKrSatelliteAsync(
                validationResult,
                cancellationToken);

            if (sCard is null)
            {
                return false;
            }

            var activeTasksSection = sCard.GetActiveTasksSection();
            var activeTaskRow = activeTasksSection.Rows.FirstOrDefault(p => taskID.Equals(p.Fields[KrConstants.KrActiveTasks.TaskID]));

            switch (activeTaskRow?.State)
            {
                case null:
                case CardRowState.Deleted:
                    return false;
                case CardRowState.Inserted:
                    activeTasksSection.Rows.Remove(activeTaskRow);
                    break;
                case CardRowState.Modified:
                case CardRowState.None:
                    activeTaskRow.State = CardRowState.Deleted;
                    break;
            }

            return true;
        }

        /// <summary>
        /// Возвращает доступную только для чтения коллекцию идентификаторов активных заданий.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IWorkflowEngineContext" path="/summary"/></param>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Доступная только для чтения коллекция идентификаторов активных заданий.</returns>
        public static async ValueTask<IReadOnlyList<Guid>> GetActiveTasksAsync(
            this IWorkflowEngineContext context,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(context);
            ThrowIfNull(validationResult);

            var sCard = await context.GetKrSatelliteAsync(
                validationResult,
                cancellationToken);

            if (sCard is null)
            {
                return [];
            }

            return sCard.GetActiveTasksSection()
                .Rows
                .Select(p => p.Get<Guid>(KrConstants.KrActiveTasks.TaskID))
                .ToImmutableList();
        }

        /// <summary>
        /// Добавляет в историю процесса запись о задании.
        /// </summary>
        /// <param name="context">Контекст обработки процесса в WorkflowEngine.</param>
        /// <param name="taskRowID">Идентификатор задания.</param>
        /// <param name="cycle">Номер цикла согласования.</param>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="isAdvisory">Значение <see langword="true"/>, если задание является рекомендательным, иначе - <see langword="false"/>.</param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns><inheritdoc cref="ValueTask" path="/summary"/></returns>
        public static async ValueTask AddToHistoryAsync(
            this IWorkflowEngineContext context,
            Guid taskRowID,
            int cycle,
            IValidationResultBuilder validationResult,
            bool isAdvisory = false,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(context);

            var satellite = await context.GetKrSatelliteAsync(
                validationResult,
                cancellationToken);

            satellite?.AddToHistory(
                taskRowID,
                cycle: cycle,
                advisory: isAdvisory);
        }
    }
}
