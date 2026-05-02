#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform.Data;
using Tessa.Platform.Placeholders;
using Tessa.Platform.Runtime;
using Tessa.Platform.Validation;
using Unity;

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager
{
    /// <summary>
    /// Контекст <see cref="IKrTaskManager{T}"/>.
    /// </summary>
    public interface IKrTaskManagerContext
    {
        #region Properties

        /// <inheritdoc cref="IValidationResultBuilder" path="/summary"/>
        IValidationResultBuilder ValidationResult { get; }

        /// <summary>
        /// Идентификатор основной карточки.
        /// </summary>
        Guid MainCardID { get; }

        /// <inheritdoc cref="IDbScope" path="/summary"/>
        IDbScope DbScope { get; }

        /// <inheritdoc cref="ISession" path="/summary"/>
        ISession Session { get; }

        /// <inheritdoc cref="ICardMetadata" path="/summary"/>
        ICardMetadata CardMetadata { get; }

        /// <summary>
        /// Unity-контейнер.
        /// </summary>
        IUnityContainer UnityContainer { get; }

        /// <inheritdoc cref="IPlaceholderManager" path="/summary"/>
        IPlaceholderManager PlaceholderManager { get; }

        /// <summary>
        /// Флаг, определяющий, загружена ли основная карточка.
        /// </summary>
        bool IsMainCardLoaded { get; }

        /// <summary>
        /// Дата/время сохранения карточки.
        /// </summary>
        DateTime StoreDateTime { get; }

        /// <summary>
        /// Сохраняемая обрабатываемая карточка или <see langword="null"/>, если обработка идёт вне контекста сохранения карточки.
        /// </summary>
        Card? StoreCard { get; }

        /// <summary>
        /// Значение, показывающее, что при формировании дайджеста задания должны обрабатываться плейсхолдеры.
        /// </summary>
        bool CreateDigestWithPlaceholders { get; }

        /// <inheritdoc cref="System.Threading.CancellationToken" path="/summary"/>
        CancellationToken CancellationToken { get; }

        #endregion

        #region Methods

        /// <summary>
        /// Загружает карточку, имеющую указанный идентификатор. Метод загружает карточку с сервера, если она еще не была загружена.
        /// </summary>
        /// <param name="cardID">Идентификатор загружаемой карточки.</param>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="forceLoadTasks">Указывает, что для загружаемой карточки должны быть загружены задания. Если флаг не установлен, то задания могут быть не загружены.</param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Карточка или значение <see langword="null"/>, если при обработке запроса произошла ошибка.</returns>
        ValueTask<Card?> GetCardAsync(
            Guid cardID,
            IValidationResultBuilder validationResult,
            bool forceLoadTasks = false,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает файловый контейнер карточки.
        /// </summary>
        /// <param name="cardID">Идентификатор карточки.</param>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Файловый контейнер или <see langword="null"/>, если при загрузке карточки или контейнера файлов возникла ошибка.</returns>
        ValueTask<ICardFileContainer?> GetCardFileContainerAsync(
            Guid cardID,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает карточку сателлита.
        /// </summary>
        /// <param name="cardID">Идентификатор основной карточки.</param>
        /// <param name="satelliteTypeID">Идентификатор типа сателлита.</param>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="taskID">Идентификатор задания, если сателлит относится к заданию.</param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Карточка сателлита или значение <see langword="null"/>, если произошла ошибка.</returns>
        /// <remarks>Если карточка сателлита не существует, то она автоматически создаётся.</remarks>
        ValueTask<Card?> GetCardSatelliteAsync(
            Guid cardID,
            Guid satelliteTypeID,
            IValidationResultBuilder validationResult,
            Guid? taskID = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Отправляет задание для основной карточки.
        /// </summary>
        /// <param name="taskTypeID">Идентификатор типа задания.</param>
        /// <param name="digest">Описание задания.</param>
        /// <param name="planned">
        /// Плановая дата завершения задания или значение <see langword="null"/>, если она определяется
        /// по параметрам <paramref name="plannedQuants"/> или <paramref name="plannedWorkingDays"/>.
        /// </param>
        /// <param name="plannedQuants">
        /// Число квантов для расчёта плановой даты завершения задания или значение <see langword="null"/>, если она определяется
        /// по параметрам <paramref name="planned"/> или <paramref name="plannedWorkingDays"/>.
        /// </param>
        /// <param name="plannedWorkingDays">
        /// Число рабочих дней для расчёта плановой даты завершения задания или значение <see langword="null"/>, если она определяется
        /// по параметрам <paramref name="planned"/> или <paramref name="plannedQuants"/>.
        /// </param>
        /// <param name="roleID">Идентификатор исполнителя задания или значение <see langword="null"/>, если он будет задан позже.</param>
        /// <param name="roleName">Имя исполнителя задания.</param>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="modifyTaskAction">Действие для изменения задания после его создания или значение <see langword="null"/>, если дополнительное действие не требуется.</param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Созданное задание или значение <see langword="null"/>, если произошла ошибка при создании задания.</returns>
        ValueTask<CardTask?> SendTaskAsync(
            Guid taskTypeID,
            string? digest,
            DateTime? planned,
            int? plannedQuants,
            double? plannedWorkingDays,
            Guid? roleID,
            string? roleName,
            IValidationResultBuilder validationResult,
            Action<CardTask>? modifyTaskAction = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает состояние основной карточки.
        /// </summary>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns><inheritdoc cref="KrState" path="/summary"/></returns>
        ValueTask<KrState> GetStateAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Устанавливает состояние для основной карточки.
        /// </summary>
        /// <param name="state"><inheritdoc cref="KrState" path="/summary"/></param>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns><inheritdoc cref="ValueTask" path="/summary"/></returns>
        ValueTask SetStateAsync(
            KrState state,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает идентификатор текущей группы истории заданий.
        /// </summary>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Идентификатор текущей группы истории заданий или значение <see langword="null"/>, если при её определении произошла ошибка.</returns>
        ValueTask<Guid?> GetTaskHistoryGroupIDAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Добавляет в историю процесса запись о задании.
        /// </summary>
        /// <param name="taskRowID">Идентификатор задания.</param>
        /// <param name="cycle">Номер цикла согласования.</param>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="isAdvisory">Значение <see langword="true"/>, если задание является рекомендательным, иначе - <see langword="false"/>.</param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns><inheritdoc cref="ValueTask" path="/summary"/></returns>
        ValueTask AddToHistoryAsync(
            Guid taskRowID,
            int cycle,
            IValidationResultBuilder validationResult,
            bool isAdvisory = false,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает номер текущего цикла процесса согласования.
        /// </summary>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <remarks>Номер текущего цикла согласования.</remarks>
        ValueTask<int> GetProcessCycleAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает комментарий автора к циклу согласования.
        /// </summary>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Комментарий автора к циклу согласования.</returns>
        ValueTask<string?> GetAuthorCommentAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Задаёт комментарий автора к циклу согласования.
        /// </summary>
        /// <param name="comment">Комментарий автора к циклу согласования.</param>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns><inheritdoc cref="ValueTask" path="/summary"/></returns>
        ValueTask SetAuthorCommentAsync(
            string? comment,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Добавляет задание с указанным идентификатором в список активных.
        /// </summary>
        /// <param name="taskRowID">Идентификатор задания.</param>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns><inheritdoc cref="ValueTask" path="/summary"/></returns>
        ValueTask AddActiveTaskAsync(
            Guid taskRowID,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Удаляет задание с указанным идентификатором из списка активных.
        /// </summary>
        /// <param name="taskRowID">Идентификатор задания.</param>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Значение <see langword="true"/>, если задание успешно удалено из списка активных, иначе - <see langword="false"/>.</returns>
        ValueTask<bool> TryRemoveActiveTaskAsync(
            Guid taskRowID,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает список идентификаторов активных заданий.
        /// </summary>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Список идентификаторов активных заданий.</returns>
        ValueTask<IReadOnlyList<Guid>> GetActiveTasksAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Устанавливает задание в список обрабатываемых заданий для последующих действий.
        /// </summary>
        /// <param name="task"><inheritdoc cref="CardTask" path="/summary"/></param>
        void AddTaskToNext(CardTask task);

        /// <summary>
        /// Возвращает текст с учетом плейсхолдеров.
        /// </summary>
        /// <param name="text">Текст для обработки.</param>
        /// <param name="task">Задание для замены.</param>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Строка текста с заменёнными плейсхолдерами.</returns>
        ValueTask<string?> GetWithPlaceholdersAsync(
            string? text,
            CardTask? task,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Создаёт объект с дополнительной информацией для контекста плейсхолдеров из контекста обработки действия.
        /// </summary>
        /// <param name="task">Задание, добавляемое в контекст замены плейсхолдеров, или <see langword="null"/>, если его не надо добавлять.</param>
        /// <returns>Дополнительная информация для контекста замены плейсхолдеров.</returns>
        Dictionary<string, object?> CreatePlaceholderInfo(
            CardTask? task = null);

        /// <summary>
        /// Создаёт дайджест задания на основе дайджеста указанного в настройках действия, комментария инициатора процесса согласования и дополнительного комментария.
        /// </summary>
        /// <param name="baseDigest">Дайджест указанный в настройках действия.</param>
        /// <param name="oldTask">Текущее обрабатываемое задание.</param>
        /// <param name="additionalComment">Дополнительный комментарий.</param>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Дайджест.</returns>
        ValueTask<string?> CreateDigestAsync(
            string? baseDigest,
            CardTask? oldTask,
            string? additionalComment,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Делегирует задание другому пользователю.
        /// </summary>
        /// <param name="task">Делегируемое задание.</param>
        /// <param name="digest">Дайджест задания.</param>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="taskKindID">Идентификатор вида задания.</param>
        /// <param name="taskKindCaption">Название вида задания.</param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Делегированное задание.</returns>
        ValueTask<CardTask?> DelegateTaskAsync(
            CardTask task,
            string? digest,
            IValidationResultBuilder validationResult,
            Guid? taskKindID = null,
            string? taskKindCaption = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Обновляет значение поля <c>TaskHistory.Result</c> для задания <paramref name="task"/>.
        /// </summary>
        /// <param name="task">Задание, для которого требуется обновить значение.</param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns><inheritdoc cref="ValueTask" path="/summary"/></returns>
        /// <remarks>
        /// Если установлен флаг, запрещающий обновление значения <c>TaskHistory.Result</c> для <paramref name="task"/>, то действия не выполняются.
        /// </remarks>
        ValueTask UpdateTaskHistoryResultAsync(
            CardTask task,
            CancellationToken cancellationToken = default);

        #endregion
    }
}
