using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tessa.Extensions.Default.Server.Workflow.KrCompilers;
using Tessa.Extensions.Default.Server.Workflow.KrObjectModel;
using Tessa.Platform.Data;
using Tessa.Platform.Runtime;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow
{
    /// <summary>
    /// Стратегия, подготавливающая данные для пересчёта маршрута. Реализует продвижение вперёд по маршруту.
    /// </summary>
    /// <remarks>
    /// Инициализирует новый экземпляр класса <see cref="ForwardPreparingGroupRecalcStrategy"/>.
    /// </remarks>
    /// <param name="dbScope"><inheritdoc cref="IDbScope" path="/summary"/></param>
    /// <param name="session"><inheritdoc cref="ISession" path="/summary"/></param>
    /// <param name="krStageTemplateLockStrategy"><inheritdoc cref="IKrStageTemplateLockStrategy" path="/summary"/></param>
    /// <param name="transactionScope"><inheritdoc cref="ITransactionScope" path="/summary"/></param>
    public sealed class ForwardPreparingGroupRecalcStrategy(
        IDbScope dbScope,
        ISession session) : IPreparingGroupRecalcStrategy
    {
        #region Fields

        private readonly IDbScope dbScope = NotNullOrThrow(dbScope);
        private readonly ISession session = NotNullOrThrow(session);
        private int minOrder;

        #endregion

        #region IPreparingGroupRecalcStrategy Members

        /// <inheritdoc />
        public bool Used { get; private set; } = false;

        /// <inheritdoc />
        public IList<Guid> ExecutionUnits { get; private set; }

        /// <inheritdoc />
        public Stage GetSuitableStage(IList<Stage> stages) =>
            stages.FirstOrDefault(x => x.StageGroupOrder >= this.minOrder);

        /// <inheritdoc />
        public async Task ApplyAsync(
            IKrProcessRunnerContext context,
            Stage stage,
            Stage prevStage)
        {
            if (this.Used)
            {
                throw new InvalidOperationException($"Current object {this.GetType().FullName} was used previously.");
            }

            this.Used = true;

            if (stage != null && prevStage != null)
            {
                if (prevStage.StageGroupOrder < stage.StageGroupOrder)
                {
                    // Переход между группами
                    this.ExecutionUnits = await this.GetNextStageGroupsAsync(prevStage, stage, context);
                    this.minOrder = prevStage.StageGroupOrder + 1;

                    if (this.ExecutionUnits.Count == 0)
                    {
                        // Такая ситуация возможна, когда следующая группа удалена, а между ними ничего нет.
                        // Нужно попытаться найти что-нибудь для старта, а также добавить удаленную группу в расчет,
                        // чтобы она вылетела из маршрута.
                        this.ExecutionUnits = new List<Guid>();
                        if (await this.GetNextStageGroupsAsync(prevStage, null, context) is { Count: > 0 } nextGroups)
                        {
                            // Они отсортированы по Order, поэтому в 0 будет с минимальным ордером
                            this.ExecutionUnits.Add(nextGroups[0]);
                        }

                        this.ExecutionUnits.Add(stage.StageGroupID);
                    }
                }
                else
                {
                    context.ValidationResult.AddError(this, "$KrProcess_ErrorMessage_StageStageGroupOrderLessPrevStageStageGroupOrder");
                    this.ExecutionUnits = Array.Empty<Guid>();
                    return;
                }
            }
            else if (stage is null && prevStage is not null)
            {
                // Новой группы нет, процесс завершается.
                // Пытаемся найти еще что-нибудь
                this.ExecutionUnits = await this.GetNextStageGroupsAsync(prevStage, null, context);
                this.minOrder = prevStage.StageGroupOrder + 1;
            }
            else if (stage is not null)
            {
                // Процесс только начался, старой группы нет, только новая
                // При старте процесса считаем маршрут посчитанным и пересчет отдельной группы не имеет смысла.
                this.ExecutionUnits = Array.Empty<Guid>();
                this.minOrder = stage.StageGroupOrder;
            }
        }

        #endregion

        #region Private Methods

        private Task<List<Guid>> GetNextStageGroupsAsync(
            Stage from,
            Stage to,
            IKrProcessRunnerContext context)
        {
            return KrCompilersSqlHelper.SelectFilteredStageGroupsAsync(
                this.dbScope,
                context.DocTypeID ?? context.CardType?.ID ?? Guid.Empty,
                context.WorkflowProcess.ProcessOwnerCurrentProcess?.AuthorID ?? context.WorkflowProcess.AuthorCurrentProcess?.AuthorID ?? this.session.User.ID,
                from?.StageGroupOrder + 1,
                to?.StageGroupOrder,
                context.SecondaryProcess?.ID,
                cancellationToken: context.CancellationToken);
        }

        #endregion
    }
}
