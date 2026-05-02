#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB;
using LinqToDB.Data;
using Tessa.Cards;
using Tessa.Cards.ComponentModel;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Shared;
using Tessa.Platform;
using Tessa.Platform.Data;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.Roles;
using static Tessa.Extensions.Default.Shared.Workflow.KrProcess.KrConstants;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess
{
    /// <summary>
    /// Расширение на сохранение задания дополнительного согласования <see cref="DefaultTaskTypes.KrAdditionalApprovalTypeID"/>.
    /// </summary>
    public sealed class KrAdditionalApprovalCardStoreExtension :
        CardStoreExtension
    {
        #region Private Methods

        private static async Task InsertChildRecordAsync(
            CardTask task,
            Guid parentID,
            DateTime storeDateTime,
            IDbScope dbScope,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            // при вставке задания у него присутствуют все секции,
            // а вся его информация актуальна в объекте task

            // метод следует вызывать только в том случае, если задание parentID ещё не завершено

            var taskCard = task.Card;
            var taskFields = taskCard.Sections[KrAdditionalApproval.Name].RawFields;
            var additionalApprovalFields = taskCard.Sections[KrAdditionalApprovalTaskInfo.Name].RawFields;

            var created = taskCard.Created ?? storeDateTime;
            var planned = task.Planned ?? storeDateTime;
            var parentComment = taskFields.TryGet<string>(KrAdditionalApproval.AuthorComment);
            var isResponsible = additionalApprovalFields.TryGet<bool>(KrAdditionalApprovalTaskInfo.IsResponsible);

            // У заданий доп. согласования всегда есть исполнитель. Если его вдруг нет, значит где-то была изменена логика этих задач и она работает некорректно.
            // В этом случае возьмём за исполнителя запись с признаком "Master".
            await CardComponentHelper.FillTaskAssignedRolesAsync(task, dbScope, cancellationToken: cancellationToken);

            var role =
                CardComponentHelper.TryGetMasterTaskAssignedRole(task, validationResult, typeof(KrAdditionalApprovalCardStoreExtension));

            if (role is null)
            {
                return;
            }

            var executor = dbScope.Executor;
            await executor
                .ExecuteNonQueryAsync(
                    dbScope.BuilderFactory
                        .InsertInto(
                            KrAdditionalApprovalInfo.Name,
                            KrAdditionalApprovalBase.ID,
                            KrAdditionalApprovalBase.RowID,
                            KrAdditionalApprovalBase.PerformerID,
                            KrAdditionalApprovalBase.PerformerName,
                            KrAdditionalApprovalBase.UserID,
                            KrAdditionalApprovalBase.UserName,
                            KrAdditionalApprovalBase.OptionID,
                            KrAdditionalApprovalBase.OptionCaption,
                            KrAdditionalApprovalBase.Comment,
                            KrAdditionalApprovalBase.Answer,
                            KrAdditionalApprovalBase.Created,
                            KrAdditionalApprovalBase.Planned,
                            KrAdditionalApprovalBase.InProgress,
                            KrAdditionalApprovalBase.Completed,
                            KrAdditionalApprovalInfo.IsResponsible)
                        .Values(v => v
                            .P("ParentID", "TaskID", "RoleID", "RoleName").V(null)
                            .V(null).V(null).V(null).P("Comment").V(null)
                            .P("Created", "Planned").V(null).V(null).P("IsResponsible"))
                        .Build(),
                    cancellationToken,
                    executor.Parameter("ParentID", parentID, DataType.Guid),
                    executor.Parameter("TaskID", task.RowID, DataType.Guid),
                    executor.Parameter("RoleID", role.RoleID, DataType.Guid),
                    executor.Parameter("RoleName", SqlHelper.LimitString(role.RoleName, RoleHelper.RoleNameMaxLength), DataType.NVarChar),
                    executor.Parameter("Comment", parentComment, DataType.NVarChar),
                    executor.Parameter("Created", created, DataType.DateTime),
                    executor.Parameter("Planned", planned, DataType.DateTime),
                    executor.Parameter("IsResponsible", BooleanBoxes.Box(isResponsible), DataType.Boolean));
        }

        private static Task ClearMainTaskInfoAsync(
            Guid parentID,
            IQueryExecutor executor,
            IQueryBuilderFactory builderFactory,
            CancellationToken cancellationToken = default) =>
            executor
                .ExecuteNonQueryAsync(
                    builderFactory
                        .DeleteFrom(KrAdditionalApprovalUsers.Name)
                        .Where().C(KrAdditionalApprovalUsers.ID).Equals().P("ParentID").Z()
                        .Update(KrAdditionalApproval.Name)
                            .C(KrAdditionalApproval.FirstIsResponsible).Assign().V(false)
                            .C(KrAdditionalApproval.Comment).Assign().V(null)
                        .Where().C("ID").Equals().P("ParentID")
                        .Build(),
                    cancellationToken,
                    executor.Parameter("ParentID", parentID, DataType.Guid));

        private static Task<bool> HasTaskAsync(
            Guid taskRowID,
            DbManager db,
            IQueryBuilderFactory builderFactory,
            CancellationToken cancellationToken = default) =>
            db
                .SetCommand(
                    builderFactory
                        .Select().V(true)
                        .From("Tasks").NoLock()
                        .Where().C("RowID").Equals().P("RowID")
                        .Build(),
                    db.Parameter("RowID", taskRowID, DataType.Guid))
                .LogCommand()
                .ExecuteAsync<bool>(cancellationToken);

        private static async Task UpdateChildRecordAsync(
            CardTask task,
            IDbScope dbScope,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            var executor = dbScope.Executor;
            var parameters = new List<DataParameter>
            {
                executor.Parameter("TaskID", task.RowID, DataType.Guid)
            };

            await CardComponentHelper.FillTaskAssignedRolesAsync(task, dbScope, cancellationToken: cancellationToken);

            // Найдём запись в TaskAssignedRoles с пометкой "Master".
            var masterTaskAssignedRole = CardComponentHelper.TryGetMasterTaskAssignedRole(task, validationResult, typeof(KrAdditionalApprovalCardStoreExtension));

            // если даже родительское задание уже завершено, то update не упадёт, а просто не обновит строки
            var builder = dbScope.BuilderFactory
                .Update(KrAdditionalApprovalInfo.Name)
                .C(KrAdditionalApprovalBase.InProgress).Assign().C("th", "InProgress")
                .C(KrAdditionalApprovalBase.UserID).Assign().C("th", "UserID")
                .C(KrAdditionalApprovalBase.UserName).Assign().C("th", "UserName");

            if (task.State == CardRowState.Deleted)
            {
                builder
                    .C(KrAdditionalApprovalBase.Completed).Assign().C("th", "Completed")
                    .C(KrAdditionalApprovalBase.OptionID).Assign().C("th", "OptionID")
                    .C(KrAdditionalApprovalBase.OptionCaption).Assign().C("th", "OptionCaption");
            }

            if (masterTaskAssignedRole is not null)
            {
                builder
                    .C(KrAdditionalApprovalBase.PerformerID).Assign().P("PerformerRoleID")
                    .C(KrAdditionalApprovalBase.PerformerName).Assign().P("PerformerRoleName");

                parameters.Add(executor.Parameter("PerformerRoleID", masterTaskAssignedRole.RoleID, DataType.Guid));
                parameters.Add(executor.Parameter("PerformerRoleName", masterTaskAssignedRole.RoleName, DataType.NVarChar));
            }

            if (task.Action == CardTaskAction.Complete && task.OptionID.HasValue)
            {
                string? answer = null;

                if (task.OptionID != DefaultCompletionOptions.Revoke)
                {
                    if (task.TryGetCard() is { } taskCard
                        && taskCard.TryGetSections() is { } taskSections
                        && taskSections.TryGetValue(KrAdditionalApprovalTaskInfo.Name, out var additionalApprovalSection))
                    {
                        answer = additionalApprovalSection.RawFields.TryGet<string>(KrAdditionalApprovalTaskInfo.Comment);
                    }
                }
                else
                {
                    // удаляем комментарий автора, если он есть, отделённый переводом строки от прочей информации
                    answer = task.Result;
                    if (!string.IsNullOrEmpty(answer))
                    {
                        var firstLineEnding = answer.IndexOf(Environment.NewLine, StringComparison.Ordinal);
                        if (firstLineEnding > 0)
                        {
                            answer = answer[(firstLineEnding + Environment.NewLine.Length)..];
                        }
                    }
                }

                if (answer is not null)
                {
                    builder
                        .C(KrAdditionalApprovalBase.Answer).Assign().P("Answer");
                    parameters.Add(
                        executor.Parameter("Answer", answer, DataType.NVarChar));
                }
            }

            builder
                .From("TaskHistory", "th").NoLock()
                .Where()
                    .C(KrAdditionalApprovalInfo.Name, KrAdditionalApprovalBase.RowID).Equals().C("th", "RowID")
                    .And().C(KrAdditionalApprovalInfo.Name, KrAdditionalApprovalBase.RowID).Equals().P("TaskID");

            await executor
                .ExecuteNonQueryAsync(
                    builder.Build(),
                    cancellationToken,
                    parameters.ToArray());
        }

        private static Task DeleteRecordAsync(
            CardTask task,
            IDbScope dbScope,
            CancellationToken cancellationToken = default)
        {
            var db = dbScope.Db;

            return db
                .SetCommand(
                    dbScope.BuilderFactory
                        .DeleteFrom(KrAdditionalApprovalInfo.Name)
                        .Where().C(KrAdditionalApprovalInfo.RowID).Equals().P(KrAdditionalApprovalInfo.RowID)
                        .Build(),
                    db.Parameter(KrAdditionalApprovalInfo.RowID, task.RowID, DataType.Guid))
                .ExecuteNonQueryAsync(cancellationToken);
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async Task BeforeCommitTransaction(
            ICardStoreExtensionContext context)
        {
            if (context.Request.TryGetCard() is not { } card
                || card.TryGetTasks() is not { Count: > 0 } tasks)
            {
                return;
            }

            await using var _ = context.DbScope!.Create();
            var executor = context.DbScope.Executor;
            var builderFactory = context.DbScope.BuilderFactory;
            var db = context.DbScope.Db;

            foreach (var task in tasks)
            {
                var parentRowID = task.ParentRowID;
                if (parentRowID is null || task.TypeID != DefaultTaskTypes.KrAdditionalApprovalTypeID)
                {
                    continue;
                }

                if (task.State == CardRowState.Inserted)
                {
                    // родительское задание расположено в той же карточке, что и дочернее;
                    // поскольку мы находимся в транзакции на сохранение этой карточки,
                    // то никто посторонний гарантированно не может завершить родительское задание,
                    // пока не закончится этот метод

                    // однако задание уже может быть завершено на момент выполнения метода,
                    // поэтому надо проверить его существование перед вставкой строки

                    if (await HasTaskAsync(parentRowID.Value, db, builderFactory, context.CancellationToken))
                    {
                        await InsertChildRecordAsync(
                            task,
                            parentRowID.Value,
                            context.StoreDateTime ?? DateTime.UtcNow,
                            context.DbScope,
                            context.ValidationResult,
                            context.CancellationToken);

                        await ClearMainTaskInfoAsync(
                            parentRowID.Value,
                            executor,
                            builderFactory,
                            context.CancellationToken);
                    }
                }
                else
                {
                    if (task.Action == CardTaskAction.None
                        && task.State == CardRowState.Deleted)
                    {
                        await DeleteRecordAsync(
                            task,
                            context.DbScope,
                            context.CancellationToken);
                    }
                    else
                    {
                        await UpdateChildRecordAsync(
                            task,
                            context.DbScope,
                            context.ValidationResult,
                            context.CancellationToken);
                    }
                }
            }
        }

        #endregion
    }
}
