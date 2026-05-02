#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB;
using Tessa.Platform;
using Tessa.Platform.Data;
using Tessa.Platform.Operations;
using Tessa.Platform.Runtime;
using Unity;

namespace Tessa.Test.Default.Shared.Platform.Operations
{
    public class TestOperationServerRepository(
        IDbScope dbScope,
        IDbmsErrorCodeProvider dbmsErrorCodeProvider,
        ISession session,
        ITransactionStrategy transactionStrategy,
        IOperationProgressStrategy operationProgressStrategy,
        IOperationQueueStrategy operationQueueStrategy,
        IClock clock,
        [Dependency(SignatureProviderNames.Operations)] ISignatureProvider? signatureProvider = null)
        : OperationServerRepository(dbScope, dbmsErrorCodeProvider, session, transactionStrategy, operationProgressStrategy, operationQueueStrategy, clock, signatureProvider)
    {
        #region Methods

        public async Task<List<IOperation>> GetAllAsync(Guid typeID, bool loadEverything, CancellationToken cancellationToken = default)
        {
            var operations = new List<Operation>();

            await using (this.DbScope.Create())
            {
                var db = this.DbScope.Db;
                var builderFactory = this.DbScope.BuilderFactory;

                await using var reader = await db
                    .SetCommand(
                        BuildSelectOperationsQueryWithoutFilters(builderFactory, loadEverything)
                            .Where().C("o", "TypeID").Equals().P("TypeID")
                            .Build(),
                        db.Parameter("TypeID", typeID, DataType.Guid))
                    .LogCommand()
                    .ExecuteReaderAsync(loadEverything ? CommandBehavior.SequentialAccess : CommandBehavior.Default, cancellationToken);

                var dbms = db.Dbms;
                while (await reader.ReadAsync(cancellationToken))
                {
                    var operation = await ReadOperationCoreAsync(reader, dbms, loadEverything, cancellationToken);
                    operations.Add(operation);
                }
            }

            foreach (var operation in operations)
            {
                operation.Progress = await this.TryGetOperationProgressAsync(operation.ID, operation.State, operation.CreationFlags, cancellationToken);
            }

            return operations.Cast<IOperation>().ToList();
        }

        public async Task<List<IOperation>> GetAllAsync(bool loadEverything, CancellationToken cancellationToken = default)
        {
            var operations = new List<Operation>();
            await using (this.DbScope.Create())
            {
                var db = this.DbScope.Db;
                var builderFactory = this.DbScope.BuilderFactory;

                await using var reader = await db
                    .SetCommand(
                        BuildSelectOperationsQueryWithoutFilters(builderFactory, loadEverything)
                            .Build())
                    .LogCommand()
                    .ExecuteReaderAsync(loadEverything ? CommandBehavior.SequentialAccess : CommandBehavior.Default, cancellationToken);

                var dbms = db.Dbms;
                while (await reader.ReadAsync(cancellationToken))
                {
                    var operation = await ReadOperationCoreAsync(reader, dbms, loadEverything, cancellationToken);
                    operations.Add(operation);
                }
            }

            foreach (var operation in operations)
            {
                operation.Progress = await this.TryGetOperationProgressAsync(operation.ID, operation.State, operation.CreationFlags, cancellationToken);
            }

            return operations.Cast<IOperation>().ToList();
        }

        #endregion
    }
}
