#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Extensions.Default.Server.Workflow.KrObjectModel;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow;
using Tessa.Extensions.Default.Shared.Workflow.KrCompilers;
using Tessa.Platform.Collections;

namespace Tessa.Extensions.Default.Server.Workflow.KrCompilers.UserAPI
{
    /// <summary>
    /// Предоставляет методы для манипулирования этапами процесса.
    /// </summary>
    public sealed class StagesContainer
    {
        #region Fields

        private readonly IObjectModelMapper objectModelMapper;

        private readonly WorkflowProcess process;

        private bool needSorting;

        private readonly Guid stageGroupID;

        #endregion

        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="StagesContainer"/>.
        /// </summary>
        /// <param name="objectModelMapper">Объект обеспечивающий работу с хранилищами Kr процесса и объектной моделью процесса.</param>
        /// <param name="process">Объектная модель процесса.</param>
        /// <param name="stageGroupID">Идентификатор группы этапов, для которой был создан контейнер.</param>
        /// <remarks>
        /// Конструктор создает контейнер для этапов наследования
        /// на основе существующей карточки с этапом согласования.
        /// Предполагается, что в карточке есть секции с этапами, согласующими и доп. согласующими.
        /// Необходимо учитывать, что этапы в карточке уже могли быть созданы на основе шаблона.
        /// Так как для этапов хранится только ID, KrStageTemplates и StageRowID, необходима исходная карточка шаблона.
        /// </remarks>
        public StagesContainer(
            IObjectModelMapper objectModelMapper,
            WorkflowProcess process,
            Guid stageGroupID)
        {
            this.objectModelMapper = NotNullOrThrow(objectModelMapper);
            this.process = NotNullOrThrow(process);
            this.stageGroupID = stageGroupID;

            this.DetermineUserModifiedArea();

            this.needSorting = false;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Возвращает коллекцию этапов на момент формирования маршрута.
        /// </summary>
        public SealableObjectList<Stage> InitialStages => NotNullOrThrow(this.process.InitialWorkflowProcess).Stages;

        /// <summary>
        /// Возвращает коллекцию этапов.
        /// </summary>
        /// <remarks>При необходимости выполняет сортировку этапов.</remarks>
        public SealableObjectList<Stage> Stages
        {
            get
            {
                if (this.needSorting)
                {
                    this.process.Stages = this.process.Stages
                        .OrderBy(static i => i, StageOrderComparer.Instance)
                        .ToSealableObjectList();
                    this.needSorting = false;
                }

                return this.process.Stages;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Объединяет существующие этапы с новыми.
        /// </summary>
        /// <param name="newStages">Новые этапы.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <rturns>Асинхронная задача.</rturns>
        public async ValueTask MergeStagesAsync(
            IReadOnlyCollection<Stage> newStages,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(newStages);

            if (newStages.Count == 0)
            {
                return;
            }

            var currentStages = this.process.Stages;
            var oldStagesTable = new Dictionary<Guid, int>(currentStages.Count);

            for (var stageIndex = 0; stageIndex < currentStages.Count; stageIndex++)
            {
                oldStagesTable.Add(currentStages[stageIndex].ID, stageIndex);
            }

            foreach (var newStage in newStages)
            {
                if (oldStagesTable.TryGetValue(newStage.ID, out var oldStageIndex))
                {
                    var currentStage = currentStages[oldStageIndex];

                    // Меняем только если строка не изменена пользователем.
                    if (!currentStage.RowChanged)
                    {
                        newStage.Inherit(currentStage);
                        newStage.IsPositionUnspecified = currentStage.IsPositionUnspecified;
                        currentStages[oldStageIndex] = newStage;
                    }
                    else
                    {
                        currentStage.InitialStage = false;

                        // Если порядок не изменен, нужно добавить для обновления порядка сортировки.
                        if (!currentStage.OrderChanged)
                        {
                            currentStage.InheritPosition(newStage);
                        }
                    }
                }
                else
                {
                    currentStages.Add(newStage);
                }

                await this.objectModelMapper.RepairSettingsAsync(
                    newStage,
                    cancellationToken);
            }

            this.needSorting = true;
        }

        /// <summary>
        /// Заменяет этап расположенный по индексу <paramref name="index"/> на новый.
        /// </summary>
        /// <param name="index">Порядковый индекс заменяемого этапа.</param>
        /// <param name="stage">Новый этап.</param>
        public void ReplaceStage(int index, Stage stage)
        {
            ThrowIfNull(stage);

            this.process.Stages[index] = stage;
            this.needSorting = true;
        }

        /// <summary>
        /// Объединяет существующие этапы с этапами из карточки шаблона этапов.
        /// </summary>
        /// <param name="template">Шаблон этапов.</param>
        /// <param name="runtimeStages">Этапы из шаблона <paramref name="template"/>.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить выполнение асинхронной задачи.</param>
        /// <returns>Асинхронная задача.</returns>
        public async ValueTask MergeWithAsync(
            IKrStageTemplate template,
            IReadOnlyList<IKrRuntimeStage> runtimeStages,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(template);
            ThrowIfNull(runtimeStages);

            var templateStages = await this.objectModelMapper.GetTemplateStagesAsync(
                template,
                runtimeStages,
                cancellationToken);

            await this.MergeStagesAsync(
                templateStages,
                cancellationToken);
        }

        /// <summary>
        /// Объединяет существующие этапы с этапами из карточки шаблона этапов.
        /// </summary>
        /// <param name="templates">Перечисление шаблонов этапов.</param>
        /// <param name="stages">Словарь, содержащий: ключ - идентификатор шаблона этапов, значение - коллекция этапов, содержащихся в шаблоне этапов.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить выполнение асинхронной задачи.</param>
        /// <returns>Асинхронная задача.</returns>
        public async ValueTask MergeWithAsync(
            IEnumerable<IKrStageTemplate> templates,
            IReadOnlyDictionary<Guid, IReadOnlyList<IKrRuntimeStage>> stages,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(templates);
            ThrowIfNull(stages);

            var templateStages = new List<Stage>();

            foreach (var template in templates)
            {
                var runtimeStages = stages.GetValueOrDefault(
                    template.ID,
                    ImmutableList<IKrRuntimeStage>.Empty);

                var templateStagesTemp = await this.objectModelMapper.GetTemplateStagesAsync(
                    template,
                    runtimeStages,
                    cancellationToken);

                templateStages.AddRange(templateStagesTemp);
            }

            await this.MergeStagesAsync(
                templateStages,
                cancellationToken);
        }

        /// <summary>
        /// Удаляет этапы, подставленные из шаблонов ранее, которые при текущем пересчете не заменены.
        /// </summary>
        public void DeleteUnconfirmedStages()
        {
            this.process.Stages = this.process.Stages
                .Where(this.StageHasRightToLive)
                .ToSealableObjectList();

            this.needSorting = true;
        }

        /// <summary>
        /// Восстанавливает всем этапам внутри контейнера значение флага <see cref="Stage.InitialStage"/>.
        /// </summary>
        public void RestoreFlags()
        {
            var currentGroupProcessed = false;
            foreach (var stage in this.process.Stages)
            {
                if (KrCompilersHelper.ReferToGroup(this.stageGroupID, stage))
                {
                    currentGroupProcessed = true;

                    if (!stage.InitialStage)
                    {
                        stage.InitialStage = true;
                    }
                }
                else if (currentGroupProcessed)
                {
                    break;
                }
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Возвращает значение, показывающее, может ли этап остаться после пересчёта.
        /// </summary>
        /// <param name="stage">Проверяемый этап.</param>
        /// <returns>Значение <see langword="true"/>, если этап может остаться после пересчёта, иначе - <see langword="false"/>.</returns>
        /// <remarks>Оставляет только ручные и обновленные этапы.</remarks>
        private bool StageHasRightToLive(
            Stage stage) =>
            !KrCompilersHelper.ReferToGroup(this.stageGroupID, stage)
            || !stage.BasedOnTemplate
            || !stage.InitialStage;

        /// <summary>
        /// Выполняет поиск области в середине списка, в которую пользователь вносил изменения.
        /// </summary>
        /// <remarks>
        /// Для изменённых этапов будет выставлен <see cref="GroupPosition.Unspecified"/>, что позволит сохранить положение, указанное пользователем.
        /// </remarks>
        private void DetermineUserModifiedArea()
        {
            // 1. Группировка по группам этапов.
            var groupedCurrentStages = this.process.Stages
                .GroupBy(i => i.StageGroupID)
                .Select(i => i.ToArray());

            // 2. Определение областей модифицированных этапов в каждой из групп.
            foreach (var currentStages in groupedCurrentStages)
            {
                var firstUserModifiedStageIndex = currentStages.IndexOf(
                    p => p.GroupPosition == GroupPosition.Unspecified
                        || p.GroupPosition == GroupPosition.AtLast
                        || p.OrderChanged);

                if (firstUserModifiedStageIndex == -1)
                {
                    // Все этапы по шаблонам.
                    continue;
                }

                var lastUserModifiedStageIndex = currentStages.LastIndexOf(
                    p => p.GroupPosition == GroupPosition.Unspecified
                        || p.GroupPosition == GroupPosition.AtFirst
                        || p.OrderChanged);

                if (lastUserModifiedStageIndex == -1)
                {
                    // Довольно странное поведение. Если нашли выше,
                    // то скорее всего и здесь должны что нибудь найти
                    continue;
                }

                // Помечаем область, тронутую пользователем, как "неопределенную".
                // Для всех элементов сортировка применяется по одному ключу,
                // а, за счет стабильности linq-сортировки, этапы передвинуты не будут.
                for (var currentStageIndex = firstUserModifiedStageIndex;
                    currentStageIndex <= lastUserModifiedStageIndex;
                    currentStageIndex++)
                {
                    var currentStage = currentStages[currentStageIndex];

                    // Необходимо исключить возможные вкрапления неперемещаемых этапов из
                    // центральной области "неопределенной" группы, чтобы при сортировке
                    // они встали на свои места.
                    if (currentStage.CanChangeOrder)
                    {
                        currentStage.IsPositionUnspecified = true;
                    }
                }
            }
        }

        #endregion
    }
}
