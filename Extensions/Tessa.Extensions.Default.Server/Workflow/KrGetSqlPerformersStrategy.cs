#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB;
using Tessa.Extensions.Default.Shared.Workflow;
using Tessa.Platform.Data;
using Tessa.Platform.Runtime;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Workflow
{
    /// <inheritdoc cref="IKrGetSqlPerformersStrategy"/>
    /// <param name="dbScope"><inheritdoc cref="IDbScope" path="/summary"/></param>
    /// <param name="session"><inheritdoc cref="ISession" path="/summary"/></param>
    public sealed class KrGetSqlPerformersStrategy(
        IDbScope dbScope,
        ISession session)
        : IKrGetSqlPerformersStrategy
    {
        #region Fields

        private readonly IDbScope dbScope = NotNullOrThrow(dbScope);
        private readonly ISession session = NotNullOrThrow(session);

        #endregion

        #region ISqlPerformersStrategy Members

        /// <inheritdoc/>
        public async Task<IReadOnlyList<RoleEntryStorage>> GetAsync(
            string? sqlPerformerScript,
            Guid mainCardID,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(validationResult);

            if (string.IsNullOrWhiteSpace(sqlPerformerScript))
            {
                return [];
            }

            await using (this.dbScope.Create())
            {
                var db = this.dbScope.Db;
                db
                    .SetCommand(
                        sqlPerformerScript,
                        db.Parameter("CardID", mainCardID, DataType.Guid),
                        db.Parameter("UserID", this.session.User.ID, DataType.Guid))
                    .LogCommand();

                await using var reader = await db.ExecuteReaderAsync(cancellationToken);
                if (!await reader.ReadAsync(cancellationToken))
                {
                    return [];
                }

                if (reader.FieldCount != 2
                    || reader[0] is not Guid firstRoleID
                    || reader[1] is not string firstRoleName)
                {
                    ValidationSequence
                        .Begin(validationResult)
                        .SetObjectName(this)
                        .ErrorDetails(
                            "$KrActions_IncorrectCalculatedPerformersQuery_IncorrectSqlResultSet",
                            sqlPerformerScript)
                        .End();
                    return [];
                }

                var result = new List<RoleEntryStorage>
                {
                    new(firstRoleID, firstRoleName)
                };

                while (await reader.ReadAsync(cancellationToken))
                {
                    result.Add(new(
                        reader.GetGuid(0),
                        reader.GetString(1)));
                }

                // Проверка есть ли ещё запросы, в т.ч. содержащие ошибки.
                if (await reader.NextResultAsync(cancellationToken))
                {
                    ValidationSequence
                        .Begin(validationResult)
                        .SetObjectName(this)
                        .ErrorDetails(
                            "$KrActions_IncorrectCalculatedPerformersQuery_SeveralQueries",
                            sqlPerformerScript)
                        .End();
                    return [];
                }

                return result;
            }
        }

        #endregion
    }
}
