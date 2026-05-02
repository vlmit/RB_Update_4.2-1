#nullable enable

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Tessa.Extensions.Default.Server.Workflow.KrObjectModel;
using Tessa.Platform.Data;

namespace Tessa.Extensions.Default.Server.Workflow.KrCompilers.SqlProcessing
{
    /// <inheritdoc cref="IKrSqlExecutor" path="/summary"/>
    public sealed class KrSqlExecutor :
        IKrSqlExecutor
    {
        #region Fields

        private readonly Func<IKrSqlPreprocessor> getSqlPreprocessor;

        private readonly IDbScope dbScope;

        #endregion

        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса.
        /// </summary>
        /// <param name="getSqlPreprocessor"><inheritdoc cref="IKrSqlPreprocessor" path="/summary"/></param>
        /// <param name="dbScope"><inheritdoc cref="IDbScope" path="/summary"/></param>
        public KrSqlExecutor(
            Func<IKrSqlPreprocessor> getSqlPreprocessor,
            IDbScope dbScope)
        {
            this.getSqlPreprocessor = NotNullOrThrow(getSqlPreprocessor);
            this.dbScope = NotNullOrThrow(dbScope);
        }

        #endregion

        #region IKrSqlExecutor Members

        /// <inheritdoc />
        public async Task<bool> ExecuteConditionAsync(IKrSqlExecutorContext context)
        {
            ThrowIfNull(context);

            if (string.IsNullOrWhiteSpace(context.Query))
            {
                return true;
            }

            var sqlPreprocessorResult = this.getSqlPreprocessor()
                .Preprocess(context);

            try
            {
                await using var _ = this.dbScope.Create();
                var db = this.dbScope.Db;
                db.SetCommand(
                    sqlPreprocessorResult.Query,
                    sqlPreprocessorResult
                        .Parameters
                        .Select(p => db.Parameter(p.Key, p.Value))
                        .ToArray());

                var result = false;
                await using (var reader = await db.ExecuteReaderAsync(context.CancellationToken))
                {
                    if (await reader.ReadAsync(context.CancellationToken))
                    {
                        if (reader.FieldCount != 1)
                        {
                            throw CreateQueryExecutionException(
                                null,
                                "$KrProcess_ErrorMessage_SqlConditionTooManyColumns",
                                sqlPreprocessorResult,
                                context);
                        }

                        var value = reader.GetValue<object>(0);

                        if (value is null
                            || value.Equals(0)
                            || value.Equals(false))
                        {
                            // ReSharper disable once RedundantAssignment
                            result = false;
                        }
                        else
                        {
                            result = value.Equals(1) || value.Equals(true)
                                ? true
                                : throw CreateQueryExecutionException(
                                    null,
                                    "$KrProcess_ErrorMessage_SqlConditionTooManyRows",
                                    sqlPreprocessorResult,
                                    context);
                        }
                    }
                    else
                    {
                        // ReSharper disable once RedundantAssignment
                        result = false;
                    }

                    if (await reader.ReadAsync(context.CancellationToken))
                    {
                        throw CreateQueryExecutionException(
                            null,
                            "$KrProcess_ErrorMessage_SqlPerformersError",
                            sqlPreprocessorResult,
                            context);
                    }
                }

                return result;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (QueryExecutionException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw CreateQueryExecutionException(
                    e,
                    "$KrProcess_ErrorMessage_SqlPerformersError",
                    sqlPreprocessorResult,
                    context);
            }
        }

        /// <inheritdoc />
        public async Task<IList<Performer>> ExecutePerformersAsync(IKrSqlExecutorContext context)
        {
            ThrowIfNull(context);

            if (string.IsNullOrWhiteSpace(context.Query))
            {
                return Array.Empty<Performer>();
            }

            var sqlPreprocessorResult = this.getSqlPreprocessor()
                .Preprocess(context);

            try
            {
                await using var _ = this.dbScope.Create();
                var db = this.dbScope.Db;
                db.SetCommand(
                    sqlPreprocessorResult.Query,
                    sqlPreprocessorResult
                        .Parameters
                        .Select(p => db.Parameter(p.Key, p.Value))
                        .ToArray());

                await using var reader = await db.ExecuteReaderAsync(context.CancellationToken);

                return await ReadPerformersAsync(
                    reader,
                    context.StageRowID,
                    context);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (QueryExecutionException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw CreateQueryExecutionException(
                    e,
                    "$KrProcess_ErrorMessage_SqlPerformersError",
                    sqlPreprocessorResult,
                    context);
            }
        }

        #endregion

        #region Private Methods

        private static async Task<IList<Performer>> ReadPerformersAsync(
            DbDataReader reader,
            Guid stageRowID,
            IKrSqlExecutorContext context)
        {
            // на разных СУБД разные типы, поэтому мы не можем их проверить по GetDataTypeName;
            // читаем первую строку и смотрим

            if (!await reader.ReadAsync(context.CancellationToken))
            {
                return Array.Empty<Performer>();
            }

            if (reader.FieldCount != 2
                || reader[0] is not Guid firstID
                || reader[1] is not string firstName)
            {
                var errorText = context.GetErrorTextFunc(
                    context,
                    "$KrProcess_ErrorMessage_IncorrectRoleResultSet",
                    Array.Empty<object>());

                throw new QueryExecutionException(
                    errorText,
                    context.Query);
            }

            var result = new List<Performer>
            {
                new MultiPerformer(
                    firstID,
                    firstName,
                    stageRowID,
                    isSql: true)
            };

            while (await reader.ReadAsync(context.CancellationToken))
            {
                result.Add(new MultiPerformer(
                    reader.GetGuid(0),
                    reader.GetNullableString(1),
                    stageRowID,
                    isSql: true));
            }

            // Проверка есть ли ещё запросы, в т.ч. содержащие ошибки.
            if (await reader.NextResultAsync(context.CancellationToken))
            {
                var errorText = context.GetErrorTextFunc(
                    context,
                    "$KrProcess_ErrorMessage_SeveralQueries",
                    Array.Empty<object>());

                throw new QueryExecutionException(
                    errorText,
                    context.Query);
            }

            return result;
        }

        /// <summary>
        /// Преобразует список SQL параметров в одну строку.
        /// </summary>
        /// <param name="parameters">Параметры.</param>
        /// <returns>Преобразованные параметры.</returns>
        private static string SqlParametersToString(
            IEnumerable<KeyValuePair<string, object>> parameters) =>
            string.Join(
                Environment.NewLine,
                parameters.Select(x => $"{x.Key} = {x.Value}"));

        private static QueryExecutionException CreateQueryExecutionException(
            Exception? e,
            string text,
            IKrSqlPreprocessorResult sqlPreprocessorResult,
            IKrSqlExecutorContext context)
        {
            var query = SqlParametersToString(sqlPreprocessorResult.Parameters)
                + Environment.NewLine
                + sqlPreprocessorResult.Query;

            var errorText = context.GetErrorTextFunc(
                context,
                text,
                Array.Empty<object>());

            return new QueryExecutionException(
                errorText,
                query,
                e);
        }

        #endregion
    }
}
