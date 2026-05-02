#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Caching;
using Tessa.Cards.ComponentModel;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Localization;
using Tessa.Platform;
using Tessa.Platform.Collections;
using Tessa.Platform.Data;
using Tessa.Platform.Runtime;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.Roles;
using Tessa.Views;
using Tessa.Views.Metadata;
using Tessa.Views.Metadata.Criteria;

namespace Tessa.Extensions.Default.Shared.Workflow
{
    /// <summary>
    /// Предоставляет вспомогательные методы для работы с Workflow.
    /// </summary>
    public static class WorkflowCommonHelper
    {
        #region Public Methods

        /// <summary>
        /// Возвращает персональную роль (пользователя) для роли, имеющую указанный идентификатор.
        /// </summary>
        /// <param name="roleID">Идентификатор роли.</param>
        /// <param name="cardID">Идентификатор карточки, для которой требуется получить состав контекстной роли. Используется, если указанная роль является контекстной.</param>
        /// <param name="roleGetStrategy">Стратегия для получения информации о ролях.</param>
        /// <param name="contextRoleManager">Обработчик контекстных ролей.</param>
        /// <param name="contextRoleCache"><inheritdoc cref="ICardContextRoleCache" path="/summary"/></param>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>
        /// Идентификатор и название персональной роли или значения по умолчанию для типа, если указанная роль является контекстной и не содержит участников,
        /// или указанная роль имеет тип, отличный от <see cref="RoleType.Personal"/> или <see cref="RoleType.Context"/>.<br/>
        /// Если роль, являющаяся контекстной, возвращает более одного участника, то берётся первый участник.</returns>
        public static async Task<(Guid?, string?)> TryGetPersonalRoleIDAsync(
            Guid roleID,
            Guid cardID,
            IRoleGetStrategy roleGetStrategy,
            IContextRoleManager contextRoleManager,
            ICardContextRoleCache contextRoleCache,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(roleGetStrategy);
            ThrowIfNull(contextRoleManager);
            ThrowIfNull(contextRoleCache);
            ThrowIfNull(validationResult);

            var role = await roleGetStrategy.GetRoleParamsAsync(
                roleID,
                cancellationToken);

            switch (role.Type)
            {
                case null:
                    validationResult.AddError(
                        nameof(WorkflowCommonHelper),
                        "$KrActions_RoleNotFound",
                        roleID.ToString());
                    break;

                case RoleType.Personal:
                    return (roleID, role.Name);

                case RoleType.Context:
                    var contextRole = await contextRoleCache.GetContextRoleAsync(roleID, cancellationToken);
                    var users = await contextRoleManager.GetCardContextUsersAsync(contextRole, cardID, cancellationToken: cancellationToken);
                    if (users.Count > 0)
                    {
                        var firstUser = users[0];
                        return (firstUser.UserID, firstUser.UserName);
                    }

                    validationResult.AddError(
                        nameof(WorkflowCommonHelper),
                        "$KrActions_ContextRoleIsEmpty",
                        role.Name);
                    break;
            }

            return default;
        }

        /// <summary>
        /// Формирует единый список исполнителей, составленный из исполнителей, указанных в настройках действия и вычисляемых исполнителей.
        /// </summary>
        /// <param name="performers">Коллекция исполнителей, указанных в настройках действия. Список должен быть отсортирован в соответствии с порядком следования элементов.</param>
        /// <param name="sqlPerformers">Коллекция вычисляемых исполнителей.</param>
        /// <param name="sqlPerformerRoleID">Идентификатор роли, на место которой будет подставлен список с вычисляемыми исполнителями.</param>
        /// <returns>Единый список исполнителей.</returns>
        public static List<RoleEntryStorage> CombinePerformers(
            IEnumerable<RoleEntryStorage> performers,
            IReadOnlyCollection<RoleEntryStorage> sqlPerformers,
            Guid sqlPerformerRoleID)
        {
            ThrowIfNull(performers);
            ThrowIfNull(sqlPerformers);

            const int defaultCapacity = 5;

            var result = new List<RoleEntryStorage>(defaultCapacity + sqlPerformers.Count);
            var isSqlPerformersProcessed = false;

            foreach (var performer in performers)
            {
                if (performer.ID == sqlPerformerRoleID)
                {
                    foreach (var sqlPerformer in sqlPerformers)
                    {
                        result.Add(sqlPerformer);
                    }

                    isSqlPerformersProcessed = true;
                }
                else
                {
                    result.Add(performer);
                }
            }

            if (!isSqlPerformersProcessed)
            {
                foreach (var sqlPerformer in sqlPerformers)
                {
                    result.Add(sqlPerformer);
                }
            }

            return result;
        }

        /// <summary>
        /// Добавляет информацию о согласовавшем/не согласовавшем пользователе в строковую секцию <see cref="KrConstants.KrApprovalCommonInfo.Name"/>.
        /// </summary>
        /// <param name="sections">Словарь, содержащий информацию о секциях карточки.</param>
        /// <param name="dbScope"><inheritdoc cref="IDbScope" path="/summary"/></param>
        /// <param name="user">Пользователь, завершивший задание процесса согласования.</param>
        /// <param name="task">Завершенное задание.</param>
        /// <param name="isNegativeResult">Значение <see langword="true"/>, если результат завершения задания отрицательный, иначе - <see langword="false"/>.</param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns><inheritdoc cref="ValueTask" path="/summary"/></returns>
        public static async ValueTask AppendApprovalInfoUserCompleteTaskAsync(
            IDictionary<string, CardSection> sections,
            IDbScope dbScope,
            IUser user,
            CardTask task,
            bool isNegativeResult,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(sections);
            ThrowIfNull(dbScope);
            ThrowIfNull(user);
            ThrowIfNull(task);

            if (!sections.TryGetValue(KrConstants.KrApprovalCommonInfo.Name, out var section))
            {
                return;
            }

            var targetFieldName = isNegativeResult
                ? KrConstants.KrApprovalCommonInfo.DisapprovedBy
                : KrConstants.KrApprovalCommonInfo.ApprovedBy;

            var value = section.RawFields.TryGet<string>(targetFieldName);
            var result = StringBuilderHelper.Acquire();
            if (!string.IsNullOrEmpty(value))
            {
                result.Append(value).Append("; ");
            }

            result.Append(user.Name);

            await CardComponentHelper.FillTaskAssignedRolesAsync(
                task,
                dbScope,
                cancellationToken: cancellationToken);
            await CardComponentHelper.FillTaskSessionRolesAsync(
                task,
                user.ID,
                dbScope.Db,
                dbScope.BuilderFactory,
                cancellationToken: cancellationToken);

            // Ищем роль сотрудника. Если таких ролей несколько, берём первую по алфавиту. Если среди ролей есть его персональная роль - используем её.
            CardTaskAssignedRole? role = null;
            if (task.TaskSessionRoles.Count > 0)
            {
                foreach (var taskSessionRole in task.TaskSessionRoles)
                {
                    if (task.TaskAssignedRoles.TryFirst(x => x.RowID == taskSessionRole.TaskRoleRowID, out var taskAssignedRole))
                    {
                        if (taskAssignedRole.RoleID == user.ID)
                        {
                            role = null;
                            break;
                        }
                        else if (role is null
                                 || taskAssignedRole.RoleName?.CompareTo(role.RoleName) < 0)
                        {
                            role = taskAssignedRole;
                        }
                    }
                }
            }

            if (role is not null)
            {
                result.Append(" (").Append(LocalizationManager.EscapeIfLocalizationString(role.RoleName)).Append(')');
            }

            section.Fields[targetFieldName] = result.ToStringAndRelease();
        }

        /// <summary>
        /// Возвращает информацию по указанному идентификатору вида задания.
        /// </summary>
        /// <param name="viewService"><inheritdoc cref="IViewService" path="/summary"/></param>
        /// <param name="id">Идентификатор вида задания.</param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Кортеж содержащий: флаг, показывающий, что вид задания, имеющий указанный идентификатор, найден, и название вида задания.</returns>
        public static async Task<(bool, string?)> GetKindAsync(
            IViewService viewService,
            Guid id,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(viewService);

            var taskKindsView = await viewService.GetByNameAsync(KrConstants.Views.TaskKinds, cancellationToken)
                ?? throw new InvalidOperationException($"Can't resolve view \"{KrConstants.Views.TaskKinds}\".");

            var taskKindsMetadata = await taskKindsView.GetMetadataAsync(cancellationToken);

            var result = await taskKindsView.GetDataAsync(
                new TessaViewRequest(taskKindsMetadata.Alias) { new RequestParameter("ID").Add(EqualsToCriteriaOperator.Instance, id) },
                cancellationToken);

            return result.Rows.FirstOrDefault() is { } row
                ? (true, (string?) row[result.GetColumnIndex("KindCaption")])
                : default;
        }

        /// <summary>
        /// Устанавливает вид задания.
        /// </summary>
        /// <param name="cardTask">Задание, в котором надо установить вид.</param>
        /// <param name="kindID">Идентификатор вида задания.</param>
        /// <param name="kindCaption">Отображаемое имя вида задания.</param>
        /// <param name="objectName">Текущий объект <c>this</c> для задания имени объекта вызвавшего ошибку. Можно также передать тип объекта, строку с именем объекта или <see langword="null"/>, если имя остаётся неизвестным.</param>
        /// <returns>Результат выполнения.</returns>
        /// <remarks>Для задания значения необходимо, что бы тип карточки задания содержал комплексную колонку <c>TaskCommonInfo.Kind</c>.</remarks>
        public static ValidationResult SetTaskKind(
            CardTask cardTask,
            Guid? kindID,
            string? kindCaption,
            object? objectName = null)
        {
            ThrowIfNull(cardTask);

            if (!kindID.HasValue
                || kindCaption is null)
            {
                return ValidationResult.Empty;
            }

            cardTask.Info[CardHelper.TaskKindIDKey] = kindID;
            cardTask.Info[CardHelper.TaskKindCaptionKey] = kindCaption;

            if (cardTask.Card.Sections.TryGetValue(KrConstants.TaskCommonInfo.Name, out var taskCommonInfoSection))
            {
                var tciFields = taskCommonInfoSection.Fields;
                tciFields[KrConstants.TaskCommonInfo.KindID] = kindID;
                tciFields[KrConstants.TaskCommonInfo.KindCaption] = kindCaption;
                return ValidationResult.Empty;
            }

            return ValidationResult.FromText(
                objectName,
                "$KrActions_MissingTaskCommonInfoKind",
                ValidationResultType.Error);
        }

        #endregion
    }
}
