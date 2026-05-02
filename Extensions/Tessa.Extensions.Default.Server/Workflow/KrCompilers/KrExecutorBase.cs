#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tessa.Cards.Caching;
using Tessa.Extensions.Default.Server.Workflow.KrCompilers.SqlProcessing;
using Tessa.Extensions.Default.Server.Workflow.KrProcess;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Serialization;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform.Data;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Workflow.KrCompilers
{
    /// <summary>
    /// Базовая абстрактная реализация <see cref="IKrExecutor"/>.
    /// </summary>
    public abstract class KrExecutorBase :
        IKrExecutor
    {
        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса.
        /// </summary>
        /// <param name="krSqlExecutor"><inheritdoc cref="KrSqlExecutor" path="/summary"/></param>
        /// <param name="dbScope"><inheritdoc cref="DbScope" path="/summary"/></param>
        /// <param name="cardCache"><inheritdoc cref="CardCache" path="/summary"/></param>
        /// <param name="krTypesCache"><inheritdoc cref="KrTypesCache" path="/summary"/></param>
        /// <param name="krStageSerializer"><inheritdoc cref="KrStageSerializer" path="/summary"/></param>
        protected KrExecutorBase(
            IKrSqlExecutor krSqlExecutor,
            IDbScope dbScope,
            ICardCache cardCache,
            IKrTypesCache krTypesCache,
            IKrStageSerializer krStageSerializer)
        {
            this.KrSqlExecutor = NotNullOrThrow(krSqlExecutor);
            this.DbScope = NotNullOrThrow(dbScope);
            this.CardCache = NotNullOrThrow(cardCache);
            this.KrTypesCache = NotNullOrThrow(krTypesCache);
            this.KrStageSerializer = NotNullOrThrow(krStageSerializer);
        }

        #endregion

        #region Properties

        /// <inheritdoc cref="IKrSqlExecutor" path="/summary"/>
        protected IKrSqlExecutor KrSqlExecutor { get; }

        /// <inheritdoc cref="IDbScope" path="/summary"/>
        protected IDbScope DbScope { get; }

        /// <inheritdoc cref="ICardCache" path="/summary"/>
        protected ICardCache CardCache { get; }

        /// <inheritdoc cref="IKrTypesCache" path="/summary"/>
        protected IKrTypesCache KrTypesCache { get; }

        /// <inheritdoc cref="IKrStageSerializer" path="/summary"/>
        protected IKrStageSerializer KrStageSerializer { get; }

        #endregion

        #region IKrExecutor Members

        /// <inheritdoc />
        public abstract Task ExecuteAsync(IKrExecutionContext context);

        #endregion

        #region Protected Methods

        /// <summary>
        /// Выполняет сценарий инициализации для единицы выполнения.
        /// </summary>
        /// <param name="unit"><inheritdoc cref="IKrExecutionUnit" path="/summary"/></param>
        /// <returns>Асинхронная задача.</returns>
        protected static async Task RunBeforeAsync(
            IKrExecutionUnit unit)
        {
            try
            {
                await unit.Instance.RunBeforeAsync();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                var text = KrErrorHelper.DesignTimeError(
                    unit,
                    e.Message);

                throw new ScriptExecutionException(
                    text,
                    unit.DesignTimeSources?.SourceBefore,
                    e);
            }
        }

        /// <summary>
        /// Выполняет условия времени построения маршрута для единицы выполнения.
        /// </summary>
        /// <param name="unit"><inheritdoc cref="IKrExecutionUnit" path="/summary"/></param>
        /// <param name="context"><inheritdoc cref="IKrExecutionContext" path="/summary"/></param>
        /// <param name="confirmedIDs">Список идентификаторов подтверждённых единиц выполнения.</param>
        /// <returns>Асинхронная задача.</returns>
        protected async Task RunConditionsAsync(
            IKrExecutionUnit unit,
            IKrExecutionContext context,
            ICollection<Guid> confirmedIDs)
        {
            unit.Instance.Confirmed = await ExecuteScriptConditionAsync(unit)
                && await this.ExecuteSQLConditionAsync(unit, context);

            if (unit.Instance.Confirmed)
            {
                confirmedIDs.Add(unit.ID);
            }
        }

        /// <summary>
        /// Выполняет C#-условие времени построения маршрута для единицы выполнения.
        /// </summary>
        /// <param name="unit"><inheritdoc cref="IKrExecutionUnit" path="/summary"/></param>
        /// <returns>Значение <see langword="true"/>, если условие выполняется, иначе - <see langword="false"/>.</returns>
        protected static async ValueTask<bool> ExecuteScriptConditionAsync(
            IKrExecutionUnit unit)
        {
            try
            {
                return await unit.Instance.RunConditionAsync();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                var text = KrErrorHelper.DesignTimeError(
                    unit,
                    e.Message);

                throw new ScriptExecutionException(
                    text,
                    unit.DesignTimeSources?.SourceCondition,
                    e);
            }
        }

        /// <summary>
        /// Выполняет SQL-условие времени построения маршрута для единицы выполнения.
        /// </summary>
        /// <param name="unit"><inheritdoc cref="IKrExecutionUnit" path="/summary"/></param>
        /// <param name="context"><inheritdoc cref="IKrExecutionContext" path="/summary"/></param>
        /// <returns>Значение <see langword="true"/>, если условие выполняется, иначе - <see langword="false"/>.</returns>
        protected async Task<bool> ExecuteSQLConditionAsync(
            IKrExecutionUnit unit,
            IKrExecutionContext context)
        {
            var sqlExecutionContext = new KrSqlExecutorContext(
                unit.DesignTimeSources?.SqlCondition,
                context.ValidationResult,
                (_, errorText, args) =>
                    KrErrorHelper.SqlDesignTimeError(
                        unit,
                        errorText,
                        args),
                unit,
                context.SecondaryProcess,
                context.CardID,
                context.CardType?.ID,
                context.DocTypeID,
                context.WorkflowProcess.State,
                cancellationToken: context.CancellationToken);

            return await this.KrSqlExecutor.ExecuteConditionAsync(
                sqlExecutionContext);
        }

        /// <summary>
        /// Выполняет сценарий постобработки для единицы выполнения.
        /// </summary>
        /// <param name="unit"><inheritdoc cref="IKrExecutionUnit" path="/summary"/></param>
        /// <returns>Асинхронная задача.</returns>
        protected static async Task RunAfterAsync(IKrExecutionUnit unit)
        {
            try
            {
                await unit.Instance.RunAfterAsync();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                var text = KrErrorHelper.DesignTimeError(
                    unit,
                    e.Message);

                throw new ScriptExecutionException(
                    text,
                    unit.DesignTimeSources?.SourceAfter,
                    e);
            }
        }

        /// <summary>
        /// Выполняет заданное действие для всех единиц выполнения.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IKrExecutionContext" path="/summary"/></param>
        /// <param name="executionUnits">Список единиц выполнения.</param>
        /// <param name="funcAsync">Выполняемое действие.</param>
        /// <returns>Асинхронная задача.</returns>
        protected async Task RunForAllAsync(
            IKrExecutionContext context,
            IEnumerable<IKrExecutionUnit> executionUnits,
            Func<IKrExecutionUnit, Task> funcAsync)
        {
            foreach (var unit in executionUnits)
            {
                try
                {
                    await funcAsync(unit);

                    if (unit.Instance.ValidationResult is not null
                        && !unit.Instance.ValidationResult.IsSuccessful())
                    {
                        // В типовой реализации условие всегда будет ложным.
                        // Но в проектном решении может быть всё что угодно.
                        if (unit.Instance.ValidationResult != context.ValidationResult)
                        {
                            context.ValidationResult.Add(unit.Instance.ValidationResult);
                        }

                        return;
                    }
                }
                catch (ExecutionExceptionBase e)
                {
                    var validator = ValidationSequence
                        .Begin(context.ValidationResult)
                        .SetObjectName(this)
                        .ErrorDetails(e.ErrorMessageText, e.SourceText);

                    if (e.InnerException is not null)
                    {
                        validator.ErrorException(e.InnerException);
                    }

                    validator.End();

                    return;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    var text = KrErrorHelper.UnexpectedError(unit);
                    ValidationSequence
                        .Begin(context.ValidationResult)
                        .SetObjectName(this)
                        .ErrorText(text)
                        .ErrorException(e)
                        .End();

                    return;
                }
            }
        }

        #endregion
    }
}
