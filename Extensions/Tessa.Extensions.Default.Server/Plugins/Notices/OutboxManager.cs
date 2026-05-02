#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB;
using LinqToDB.Data;
using Tessa.Platform.Data;

namespace Tessa.Extensions.Default.Server.Plugins.Notices
{
    /// <inheritdoc cref="IOutboxManager"/>
    public class OutboxManager : IOutboxManager
    {
        #region Fields

        private readonly IDbScope dbScope;

        #endregion

        #region Constructors

        public OutboxManager(IDbScope dbScope) =>
            this.dbScope = NotNullOrThrow(dbScope);

        #endregion

        #region Properties

        /// <summary>
        /// Manager used in SaaS environment and need to select InstanceName for messages.
        /// </summary>
        protected virtual bool InSaas => false;

        /// <summary>
        /// Get required SaaS instance names to select messages.
        /// </summary>
        /// <returns></returns>
        protected virtual ValueTask<IReadOnlyCollection<string>> GetSaasInstanceNamesAsync(CancellationToken cancellationToken = default) => 
            new(Array.Empty<string>());

        #endregion

        #region IOutboxManager Members

        /// <inheritdoc/>
        public Task<ConcurrentQueue<OutboxMessage>> GetTopMessagesAsync(
            int topCount,
            int retryIntervalMinutes,
            CancellationToken cancellationToken = default) =>
            this.GetTopMessagesCoreAsync(topCount, retryIntervalMinutes, cancellationToken);

        /// <inheritdoc/>
        public Task MarkAsBadMessageAsync(
            Guid id,
            int attemptNum,
            string? exceptionMessage,
            CancellationToken cancellationToken = default) =>
            this.MarkAsBadMessageCoreAsync(id, attemptNum, exceptionMessage, cancellationToken);

        /// <inheritdoc/>
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
            this.DeleteCoreAsync(id, cancellationToken);

        #endregion

        #region Protected Methods

        /// <inheritdoc cref="GetTopMessagesAsync"/>
        protected virtual async Task<ConcurrentQueue<OutboxMessage>> GetTopMessagesCoreAsync(
            int topCount,
            int retryIntervalMinutes,
            CancellationToken cancellationToken = default)
        {
            await using var _ = this.dbScope.Create();
            var db = this.dbScope.Db;
            var dbms = db.Dbms;
            var builderFactory = this.dbScope.BuilderFactory;

            DateTime maxLastErrorDateToProcess = DateTime.UtcNow - TimeSpan.FromMinutes(retryIntervalMinutes);
            var names = await this.GetSaasInstanceNamesAsync(cancellationToken);
            DataParameter? dbInstanceNames = null;
            var result = new ConcurrentQueue<OutboxMessage>();
            await using DbDataReader reader = await db
                .SetCommand(
                    builderFactory
                        .Select().Top(topCount)
                        .C(null, "ID", "Email", "Subject", "Body", "Attempts", "Info")
                        .If(this.InSaas, b => b.C("Instance"))
                        .From("Outbox").NoLock()
                        .Where().E(b=>b.C("LastErrorDate").IsNull()
                                .Or().C("LastErrorDate").LessOrEquals().P("MaxLastErrorDateToProcess"))
                        .If(this.InSaas, b => b
                            .And().C("Instance").InArray(names, "InstanceNames", out dbInstanceNames))
                        .OrderBy("Created")
                        .Limit(topCount)
                        .Build(),
                    DataParameters.Get(
                        db.Parameter("MaxLastErrorDateToProcess", maxLastErrorDateToProcess, DataType.DateTime),
                        dbInstanceNames)
                    )
                .LogCommand()
                .ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var message =
                    new OutboxMessage
                    {
                        ID = reader.GetGuid(0),
                        Email = reader.GetNullableString(1),
                        Subject = reader.GetNullableString(2),
                        Body = reader.GetNullableString(3),
                        Attempts = reader.GetInt32(4),
                        Info = await reader.GetSequentialNullableStringAsync(5, dbms, cancellationToken)
                    };
                if (this.InSaas)
                {
                    message.InstanceName = reader.GetString(6);
                }
                result.Enqueue(message);
            }

            return result;
        }

        /// <inheritdoc cref="MarkAsBadMessageAsync"/>
        protected virtual async Task MarkAsBadMessageCoreAsync(
            Guid id,
            int attemptNum,
            string? exceptionMessage,
            CancellationToken cancellationToken = default)
        {
            await using var _ = this.dbScope.Create();
            var db = this.dbScope.Db;
            var builderFactory = this.dbScope.BuilderFactory;

            await db
                .SetCommand(
                    builderFactory
                        .Update("Outbox")
                        .C("Attempts").Assign().P("Attempts")
                        .C("LastErrorDate").Assign().P("LastErrorDate")
                        .C("LastErrorText").Assign().P("LastErrorText")
                        .Where().C("ID").Equals().P("ID")
                        .Build(),
                    db.Parameter("Attempts", attemptNum, DataType.Int32),
                    db.Parameter("LastErrorDate", DateTime.UtcNow, DataType.DateTime),
                    db.Parameter("LastErrorText", SqlHelper.LimitString(exceptionMessage, 256), DataType.NVarChar),
                    db.Parameter("ID", id, DataType.Guid))
                .LogCommand()
                .ExecuteNonQueryAsync(cancellationToken);
        }

        /// <inheritdoc cref="DeleteAsync"/>
        protected virtual async Task DeleteCoreAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await using var _ = this.dbScope.Create();
            var db = this.dbScope.Db;
            var builderFactory = this.dbScope.BuilderFactory;

            await db
                .SetCommand(
                    builderFactory
                        .DeleteFrom("Outbox")
                        .Where().C("ID").Equals().P("ID")
                        .Build(),
                    db.Parameter("ID", id, DataType.Guid))
                .LogCommand()
                .ExecuteNonQueryAsync(cancellationToken);
        }

        #endregion
    }
}
