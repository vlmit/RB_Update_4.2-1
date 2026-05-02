#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Server.Workflow.KrObjectModel;
using Tessa.Extensions.Default.Shared;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Scope
{
    /// <summary>
    /// Объект, предоставляющий методы для работы с текущим контекстом подсистемы маршрутов, содержащим разделяемые карточки.
    /// </summary>
    public interface IKrScope
    {
        #region Properties

        /// <summary>
        /// Значение, показывающее, что текущий код выполняется внутри операции с контекстом <see cref="IKrScope"/>.
        /// </summary>
        bool Exists { get; }

        /// <summary>
        /// Количество уровней в текущем контексте <see cref="IKrScope"/>.
        /// </summary>
        int Depth { get; }

        /// <summary>
        /// Результат валидации операций, производимых в текущем контексте <see cref="IKrScope"/>.
        /// Извне писать в это свойство не рекомендуется.
        /// </summary>
        /// <exception cref="InvalidOperationException">Код вызван вне контекста <see cref="IKrScope"/>. Необходимо создать контекст, путём вызова метода <see cref="EnterNewLevel"/>.</exception>
        IValidationResultBuilder ValidationResult { get; }

        /// <summary>
        /// Хранилище произвольных данных с областью видимости на текущий и вложенные запросы.
        /// </summary>
        /// <exception cref="InvalidOperationException">Код вызван вне контекста <see cref="IKrScope"/>. Необходимо создать контекст, путём вызова метода <see cref="EnterNewLevel"/>.</exception>
        Dictionary<string, object?> Info { get; }

        /// <summary>
        /// Текущий уровень контекста <see cref="IKrScope"/> или значение <see langword="null"/>, если код вызван вне контекста.
        /// </summary>
        KrScopeLevel? CurrentLevel { get; }

        #endregion

        #region Methods

        /// <summary>
        /// Удаляет и возвращает текущий уровень контекста <see cref="IKrScope"/>.
        /// </summary>
        /// <returns><inheritdoc cref="KrScopeLevel" path="/summary"/></returns>
        /// <exception cref="InvalidOperationException">Код вызван вне контекста <see cref="IKrScope"/>. Необходимо создать контекст, путём вызова метода <see cref="EnterNewLevel"/>.</exception>
        KrScopeLevel PopCurrentLevel();

        /// <summary>
        /// Создаёт новый уровень контекста <see cref="IKrScope"/>.
        /// </summary>
        /// <returns>Новый уровень контекста <see cref="IKrScope"/>.</returns>
        /// <remarks>После завершения работы с объектом <see cref="KrScopeLevel"/>, для выполнения задач связанных с освобождением ресурсов, вызовите метод <see cref="KrScopeLevel.ExitAsync(IValidationResultBuilder)"/>.</remarks>
        KrScopeLevel EnterNewLevel();

        /// <summary>
        /// Возвращает карточку с указанным идентификатором. При загрузке карточки исключается следующая информация: <see cref="CardGetRestrictionFlags.RestrictTasks"/> и <see cref="CardGetRestrictionFlags.RestrictTaskHistory"/>.
        /// </summary>
        /// <param name="mainCardID">Идентификатор карточки.</param>
        /// <param name="validationResult">
        /// Объект, выполняющий построение результатов валидации или значение <see langword="null"/>, если результат валидации должен быть записан в <see cref="ValidationResult"/>.<br/>
        ///
        /// Параметр является обязательным, при выполнении метода вне контекста <see cref="IKrScope"/>.
        /// </param>
        /// <param name="withoutTransaction">Значение <see langword="true"/>, если карточка, при выполнении вне контекста <see cref="IKrScope"/>, должна быть загружена без транзакции и без взятия блокировки на чтение карточки, иначе - <see langword="false"/>.</param>
        /// <param name="isStore">Значение <see langword="true"/>, если загруженная карточка должна быть сохранена в контексте <see cref="IKrScope"/>, иначе - <see langword="false"/>.</param>
        /// <param name="cardTypeID">Идентификатор типа карточки. Если не передан, то тип карточки определяется по её идентификатору.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить выполнение асинхронной задачи.</param>
        /// <returns>Загруженная карточка или значение <see langword="null"/>, если карточку не удалось загрузить.</returns>
        ValueTask<Card?> GetMainCardAsync(
            Guid mainCardID,
            IValidationResultBuilder? validationResult = null,
            bool withoutTransaction = false,
            bool isStore = true,
            Guid? cardTypeID = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает файловый контейнер для карточки.
        /// </summary>
        /// <param name="mainCardID">Идентификатор карточки, для которой необходимо получить файловый контейнер.</param>
        /// <param name="isStore">Значение <see langword="true"/>, если загруженная карточка должна быть сохранена в контексте <see cref="IKrScope"/>, иначе - <see langword="false"/>.</param>
        /// <param name="validationResult">
        /// Объект, выполняющий построение результатов валидации или значение <see langword="null"/>, если результат валидации должен быть записан в <see cref="ValidationResult"/>.<br/>
        ///
        /// Параметр является обязательным, при выполнении метода вне контекста <see cref="IKrScope"/>.
        /// </param>
        /// <param name="cardTypeID">Идентификатор типа карточки. Если не передан, то тип карточки определяется по её идентификатору.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить выполнение асинхронной задачи.</param>
        /// <returns>Контейнер, содержащий информацию по карточке и её файлам или значение <see langword="null"/>, если произошла ошибка.</returns>
        Task<ICardFileContainer?> GetMainCardFileContainerAsync(
            Guid mainCardID,
            bool isStore = true,
            IValidationResultBuilder? validationResult = null,
            Guid? cardTypeID = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Планирует увеличение версии карточки с заданным идентификатором.
        /// </summary>
        /// <param name="cardID">Идентификатор карточки, версию которой требуется увеличить.</param>
        /// <exception cref="InvalidOperationException">Код вызван вне контекста <see cref="IKrScope"/>. Необходимо создать контекст, путём вызова метода <see cref="EnterNewLevel"/>.</exception>
        void ForceIncrementMainCardVersion(Guid cardID);

        /// <summary>
        /// Возвращает признак, показывающий, нужно ли увеличить версию карточки с заданным идентификатором.
        /// </summary>
        /// <param name="cardID">Идентификатор карточки, версию которой требуется увеличить.</param>
        /// <returns>Значение <see langword="true"/>, если для карточки с идентификатором <paramref name="cardID"/> необходимо увеличить версию, иначе - <see langword="false"/>.</returns>
        /// <exception cref="InvalidOperationException">Код вызван вне контекста <see cref="IKrScope"/>. Необходимо создать контекст, путём вызова метода <see cref="EnterNewLevel"/>.</exception>
        /// <remarks>После вызова удаляет информацию о запланированном увеличении версии для карточки с <paramref name="cardID"/>.</remarks>
        bool GetForceIncrementCardVersion(Guid cardID);

        /// <summary>
        /// Возвращает список идентификаторов карточек, для которых должна быть принудительно увеличена версия.
        /// </summary>
        /// <returns>Список идентификаторов карточек, для которых должна быть принудительно увеличена версия.</returns>
        IReadOnlyCollection<Guid> GetForceIncrementCardVersionIdentifiers();

        /// <summary>
        /// Метод для отложенной модификации запроса на сохранение карточки.
        /// </summary>
        /// <param name="cardID">Идентификатор карточки.</param>
        /// <param name="requestModifier">Метод для модификации запроса на сохранение карточки.</param>
        void ModifyStoreRequest(Guid cardID, Action<CardStoreRequest> requestModifier);

        /// <summary>
        /// Метод для модификации запроса на сохранения с помощью отложенных методов, добавленных <see cref="ModifyStoreRequest(Guid, Action{CardStoreRequest})"/>.
        /// </summary>
        /// <param name="request">Обрабатываемый запрос на сохранение.</param>
        void ModifyStoreRequest(CardStoreRequest request);

        /// <summary>
        /// Загружает историю заданий для карточки с указанным идентификатором загруженной в <see cref="IKrScope"/>.
        /// По умолчанию история заданий не загружается.
        /// </summary>
        /// <param name="mainCardID">Идентификатор карточки, для которой требуется загрузить историю заданий.</param>
        /// <param name="validationResult">Объект, выполняющий построение результатов валидации или значение <see langword="null"/>, если результат валидации должен быть записан в <see cref="ValidationResult"/>.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить выполнение асинхронной задачи.</param>
        /// <returns>Асинхронная задача.</returns>
        /// <exception cref="InvalidOperationException">Код вызван вне контекста <see cref="IKrScope"/>. Необходимо создать контекст, путём вызова метода <see cref="EnterNewLevel"/>.</exception>
        Task EnsureMainCardHasTaskHistoryAsync(
            Guid mainCardID,
            IValidationResultBuilder? validationResult = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Загружает задания для карточки с указанным идентификатором, загруженной в <see cref="IKrScope"/>.
        /// По умолчанию задания не загружается.
        /// </summary>
        /// <param name="cardID">Идентификатор карточки, для которой требуется загрузить задания.</param>
        /// <param name="validationResult">Объект, выполняющий построение результатов валидации или значение <see langword="null"/>, если результат валидации должен быть записан в <see cref="ValidationResult"/>.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить выполнение асинхронной задачи.</param>
        /// <returns>Асинхронная задача.</returns>
        /// <exception cref="InvalidOperationException">Код вызван вне контекста <see cref="IKrScope"/>. Необходимо создать контекст, путём вызова метода <see cref="EnterNewLevel"/>.</exception>
        Task EnsureTasksLoadedAsync(
            Guid cardID,
            IValidationResultBuilder? validationResult = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает основной сателлит процесса (<see cref="DefaultCardTypes.KrSatelliteTypeID"/>) для заданной карточки.
        /// При наличии изменений сателлит будет сохранен в <see cref="ICardStoreExtension.BeforeCommitTransaction"/>.<br/>
        ///
        /// Если контекста <see cref="IKrScope"/> не существует, то сателлит будет загружен явно,
        /// дальнейшее отслеживание производиться не будет.<br/>
        ///
        /// Если сателлит не существует, то создаёт его.
        /// </summary>
        /// <param name="mainCardID">Идентификатор основной карточки.</param>
        /// <param name="noLockingMainCard">Значение <see langword="true"/>, если не следует выполнять блокировку основной карточки при создании сателлита, иначе - <see langword="false"/>.</param>
        /// <param name="isStore">Значение <see langword="true"/>, если загруженная карточка должна быть сохранена в контексте <see cref="IKrScope"/>, иначе - <see langword="false"/>.</param>
        /// <param name="validationResult">
        /// Объект, выполняющий построение результатов валидации или значение <see langword="null"/>, если результат валидации должен быть записан в <see cref="ValidationResult"/>.<br/>
        ///
        /// Параметр является обязательным, при выполнении метода вне контекста <see cref="IKrScope"/>.
        /// </param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить выполнение асинхронной задачи.</param>
        /// <returns>Основной сателлит процесса (<see cref="DefaultCardTypes.KrSatelliteTypeID"/>) для заданной карточки или значение <see langword="null"/>, если произошла ошибка.</returns>
        ValueTask<Card?> GetKrSatelliteAsync(
            Guid mainCardID,
            bool noLockingMainCard = false,
            bool isStore = true,
            IValidationResultBuilder? validationResult = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает основной сателлит процесса (<see cref="DefaultCardTypes.KrSatelliteTypeID"/>) для заданной карточки.
        /// При наличии изменений сателлит будет сохранен в <see cref="ICardStoreExtension.BeforeCommitTransaction"/>.<br/>
        ///
        /// Если контекста <see cref="IKrScope"/> не существует, то сателлит будет загружен явно, дальнейшее отслеживание производится не будет.
        /// </summary>
        /// <param name="mainCardID">Идентификатор основной карточки.</param>
        /// <param name="isStore">Значение <see langword="true"/>, если загруженная карточка должна быть сохранена в контексте <see cref="IKrScope"/>, иначе - <see langword="false"/>.</param>
        /// <param name="validationResult">
        /// Объект, выполняющий построение результатов валидации или значение <see langword="null"/>, если результат валидации должен быть записан в <see cref="ValidationResult"/>.<br/>
        ///
        /// Параметр является обязательным, при выполнении метода вне контекста <see cref="IKrScope"/>.
        /// </param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить выполнение асинхронной задачи.</param>
        /// <returns>Основной сателлит процесса (<see cref="DefaultCardTypes.KrSatelliteTypeID"/>) для заданной карточки или значение <see langword="null"/>, если сателлит не существует или произошла ошибка.</returns>
        ValueTask<Card?> TryGetKrSatelliteAsync(
            Guid mainCardID,
            bool isStore = true,
            IValidationResultBuilder? validationResult = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает текущую группу истории заданий для указанной карточки,
        /// чей контекстуальный сателлит находится в текущем <see cref="IKrScope"/>.
        /// </summary>
        /// <param name="mainCardID">Идентификатор основной карточки.</param>
        /// <param name="isStore">Значение <see langword="true"/>, если загруженная карточка должна быть сохранена в контексте <see cref="IKrScope"/>, иначе - <see langword="false"/>.</param>
        /// <param name="validationResult">Объект, выполняющий построение результатов валидации или значение <see langword="null"/>, если результат валидации должен быть записан в <see cref="ValidationResult"/>.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить выполнение асинхронной задачи.</param>
        /// <returns>Идентификатор текущей группы истории заданий.</returns>
        /// <exception cref="InvalidOperationException">Код вызван вне контекста <see cref="IKrScope"/>. Необходимо создать контекст, путём вызова метода <see cref="EnterNewLevel"/>.</exception>
        ValueTask<Guid?> GetCurrentHistoryGroupAsync(
            Guid mainCardID,
            bool isStore = true,
            IValidationResultBuilder? validationResult = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Устанавливает новую группу истории заданий для указанной карточки,
        /// чей контекстуальный сателлит находится в текущем KrScope.
        /// </summary>
        /// <param name="mainCardID">Идентификатор основной карточки.</param>
        /// <param name="newGroupHistoryID">Идентификатор новой группы истории заданий. Если <see langword="null"/>, то записи будут добавляться в пустую группу.</param>
        /// <param name="isStore">Значение <see langword="true"/>, если загруженная карточка должна быть сохранена в контексте <see cref="IKrScope"/>, иначе - <see langword="false"/>.</param>
        /// <param name="validationResult">Объект, выполняющий построение результатов валидации или значение <see langword="null"/>, если результат валидации должен быть записан в <see cref="ValidationResult"/>.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить выполнение асинхронной задачи.</param>
        /// <returns>Асинхронная задача.</returns>
        /// <exception cref="InvalidOperationException">Код вызван вне контекста <see cref="IKrScope"/>. Необходимо создать контекст, путём вызова метода <see cref="EnterNewLevel"/>.</exception>
        Task SetCurrentHistoryGroupAsync(
            Guid mainCardID,
            Guid? newGroupHistoryID,
            bool isStore = true,
            IValidationResultBuilder? validationResult = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Создаёт и сохраняет сателлит вторичного процесса.
        /// </summary>
        /// <param name="mainCardID">Идентификатор основной карточки.</param>
        /// <param name="processID">Идентификатор вторичного процесса.</param>
        /// <param name="isStore">Значение <see langword="true"/>, если загруженная карточка должна быть сохранена в контексте <see cref="IKrScope"/>, иначе - <see langword="false"/>.</param>
        /// <param name="validationResult">
        /// Объект, выполняющий построение результатов валидации или значение <see langword="null"/>, если результат валидации должен быть записан в <see cref="ValidationResult"/>.
        /// </param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить выполнение асинхронной задачи.</param>
        /// <returns>Сателлит вторичного процесса или значение <see langword="null"/>, если произошла ошибка.</returns>
        /// <exception cref="InvalidOperationException">Код вызван вне контекста <see cref="IKrScope"/>. Необходимо создать контекст, путём вызова метода <see cref="EnterNewLevel"/>.</exception>
        Task<Card?> CreateSecondaryKrSatelliteAsync(
            Guid mainCardID,
            Guid processID,
            bool isStore = true,
            IValidationResultBuilder? validationResult = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает существующий сателлит вторичного процесса.
        /// </summary>
        /// <param name="processID">Идентификатор вторичного процесса.</param>
        /// <param name="isStore">Значение <see langword="true"/>, если загруженная карточка должна быть сохранена в контексте <see cref="IKrScope"/>, иначе - <see langword="false"/>.</param>
        /// <param name="validationResult">
        /// Объект, выполняющий построение результатов валидации или значение <see langword="null"/>, если результат валидации должен быть записан в <see cref="ValidationResult"/>.<br/>
        ///
        /// Параметр является обязательным, при выполнении метода вне контекста <see cref="IKrScope"/>.
        /// </param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить выполнение асинхронной задачи.</param>
        /// <returns>Сателлит вторичного процесса или значение <see langword="null"/>, если сателлит не удалось загрузить.</returns>
        ValueTask<Card?> GetSecondaryKrSatelliteAsync(
            Guid processID,
            bool isStore = true,
            IValidationResultBuilder? validationResult = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Блокирует карточку для сохранения.
        /// Если карточка заблокирована, то при выходе с уровня сохранение произведено не будет.
        /// </summary>
        /// <param name="cardID">Идентификатор карточки.</param>
        /// <returns>Ключ для разблокирования карточки или значение <see langword="null"/>, если карточка была заблокирована ранее.</returns>
        /// <exception cref="InvalidOperationException">Код вызван вне контекста <see cref="IKrScope"/>. Необходимо создать контекст, путём вызова метода <see cref="EnterNewLevel"/>.</exception>
        Guid? LockCard(Guid cardID);

        /// <summary>
        /// Возвращает признак, показывающий, что карточка с указанным идентификатором заблокирована для сохранения.
        /// </summary>
        /// <param name="cardID">Идентификатор карточки.</param>
        /// <returns>Значение <see langword="true"/>, если карточка с указанным идентификатором заблокирована для сохранения, иначе - <see langword="false"/>.</returns>
        /// <exception cref="InvalidOperationException">Код вызван вне контекста <see cref="IKrScope"/>. Необходимо создать контекст, путём вызова метода <see cref="EnterNewLevel"/>.</exception>
        bool IsCardLocked(Guid cardID);

        /// <summary>
        /// Снимает блокировку с карточки на сохранение.
        /// </summary>
        /// <param name="cardID">Идентификатор карточки.</param>
        /// <param name="key">Ключ для снятия блокировки, полученный при выполнении метода <see cref="LockCard(Guid)"/>.</param>
        /// <returns>Значение <see langword="true"/>, если карточка успешно разблокирована, иначе - <see langword="false"/>, если карточка не заблокирована или ключ не подошел.
        /// </returns>
        /// <exception cref="InvalidOperationException">Код вызван вне контекста <see cref="IKrScope"/>. Необходимо создать контекст, путём вызова метода <see cref="EnterNewLevel"/>.</exception>
        bool ReleaseCard(Guid cardID, Guid key);

        /// <summary>
        /// Возвращает идентификаторы заблокированных карточек.
        /// </summary>
        /// <returns>Идентификаторы заблокированных карточек.</returns>
        IReadOnlyCollection<Guid> GetLockedCardIDs();

        /// <summary>
        /// Добавляет <see cref="ProcessHolder"/> в текущий контекст <see cref="IKrScope"/>.
        /// </summary>
        /// <param name="processHolder">Объект, содержащий информацию по текущему и основному процессу.</param>
        /// <exception cref="InvalidOperationException">Код вызван вне контекста <see cref="IKrScope"/>. Необходимо создать контекст, путём вызова метода <see cref="EnterNewLevel"/>.</exception>
        void AddProcessHolder(ProcessHolder processHolder);

        /// <summary>
        /// Возвращает <see cref="ProcessHolder"/> из текущего контекста <see cref="IKrScope"/>.
        /// </summary>
        /// <param name="processHolderID">Идентификатор объекта, содержащего информацию по текущему и основному процессу.</param>
        /// <returns>Объект содержащий информацию по текущему и основному процессу или значение <see langword="null"/>, если он отсутствует.</returns>
        /// <exception cref="InvalidOperationException">Код вызван вне контекста <see cref="IKrScope"/>. Необходимо создать контекст, путём вызова метода <see cref="EnterNewLevel"/>.</exception>
        ProcessHolder? GetProcessHolder(Guid processHolderID);

        /// <summary>
        /// Удаляет <see cref="ProcessHolder"/> из текущего контекста <see cref="IKrScope"/>.
        /// </summary>
        /// <param name="processHolderID">Идентификатор объекта, содержащего информацию по текущему и основному процессу.</param>
        /// <exception cref="InvalidOperationException">Код вызван вне контекста <see cref="IKrScope"/>. Необходимо создать контекст, путём вызова метода <see cref="EnterNewLevel"/>.</exception>
        void RemoveProcessHolder(Guid processHolderID);

        /// <summary>
        /// Добавляет объект, освобождение ресурсов которого будет выполнено при выполнении <see cref="IAsyncDisposable.DisposeAsync"/> этого объекта.
        /// </summary>
        /// <param name="obj">Объект, ресурсы которого требуется освободить при выполнении <see cref="IAsyncDisposable.DisposeAsync"/> этого объекта.</param>
        /// <exception cref="ArgumentNullException">Параметр <paramref name="obj"/> имеет значение null.</exception>
        /// <exception cref="InvalidOperationException">Код вызван вне контекста <see cref="IKrScope"/>. Необходимо создать контекст, путём вызова метода <see cref="EnterNewLevel"/>.</exception>
        void AddDisposableObject(IDisposable obj);

        /// <summary>
        /// Добавляет объект, освобождение ресурсов которого будет выполнено при выполнении <see cref="IAsyncDisposable.DisposeAsync"/> этого объекта.
        /// </summary>
        /// <param name="obj">Объект, ресурсы которого требуется освободить при выполнении <see cref="IAsyncDisposable.DisposeAsync"/> этого объекта.</param>
        /// <exception cref="ArgumentNullException">Параметр <paramref name="obj"/> имеет значение null.</exception>
        /// <exception cref="InvalidOperationException">Код вызван вне контекста <see cref="IKrScope"/>. Необходимо создать контекст, путём вызова метода <see cref="EnterNewLevel"/>.</exception>
        void AddDisposableObject(IAsyncDisposable obj);

        /// <summary>
        /// Проверяет, загружена ли карточка с заданным идентификатором или нет.
        /// </summary>
        /// <param name="cardID">Идентификатор карточки.</param>
        /// <returns>Возвращает <see langword="true"/>, если карточка уже была загружена, иначе <see langword="false"/>.</returns>
        /// <exception cref="InvalidOperationException">Код вызван вне контекста <see cref="IKrScope"/>. Необходимо создать контекст, путём вызова метода <see cref="EnterNewLevel"/>.</exception>
        bool CardIsLoaded(Guid cardID);

        /// <summary>
        /// Добавляет указанную карточку в контекст <see cref="IKrScope"/>.
        /// </summary>
        /// <param name="card">Добавляемая карточка.</param>
        /// <exception cref="InvalidOperationException">Код вызван вне контекста <see cref="IKrScope"/>. Необходимо создать контекст, путём вызова метода <see cref="EnterNewLevel"/>.</exception>
        void AddCard(Card card);

        /// <summary>
        /// Удаляет карточку с заданным идентификатором из контекста <see cref="IKrScope"/>.
        /// </summary>
        /// <param name="id">Идентификатор удаляемой карточки.</param>
        /// <exception cref="InvalidOperationException">Код вызван вне контекста <see cref="IKrScope"/>. Необходимо создать контекст, путём вызова метода <see cref="EnterNewLevel"/>.</exception>
        void RemoveCard(Guid id);

        /// <summary>
        /// Добавляет указанный контейнер <see cref="ICardFileContainer"/> в контекст <see cref="IKrScope"/>.
        /// </summary>
        /// <param name="cardFileContainer">Добавляемый контейнер.</param>
        /// <exception cref="InvalidOperationException">Код вызван вне контекста <see cref="IKrScope"/>. Необходимо создать контекст, путём вызова метода <see cref="EnterNewLevel"/>.</exception>
        void AddCardFileContainer(ICardFileContainer cardFileContainer);

        /// <summary>
        /// Возвращает список загруженных карточек.
        /// </summary>
        /// <param name="isSaveOrder">Значение <see langword="true"/>, если требуется получить карточки в порядке сохранения, иначе - <see langword="false"/>.</param>
        /// <returns>Список загруженных карточек.</returns>
        /// <exception cref="InvalidOperationException">Код вызван вне контекста <see cref="IKrScope"/>. Необходимо создать контекст, путём вызова метода <see cref="EnterNewLevel"/>.</exception>
        IReadOnlyCollection<Card> GetLoadedCards(bool isSaveOrder = false);

        /// <summary>
        /// Возвращает карточку, загруженную в контекст <see cref="IKrScope"/>.
        /// </summary>
        /// <param name="cardID">Идентификатор карточки.</param>
        /// <returns>Карточка или значение <see langword="null"/>, если она не содержится в контексте.</returns>
        /// <exception cref="InvalidOperationException">Код вызван вне контекста <see cref="IKrScope"/>. Необходимо создать контекст, путём вызова метода <see cref="EnterNewLevel"/>.</exception>
        Card? TryGetLoadedCard(Guid cardID);

        /// <summary>
        /// Возвращает <see cref="ICardFileContainer"/>, загруженный в контекст <see cref="IKrScope"/>.
        /// </summary>
        /// <param name="cardID">Идентификатор карточки.</param>
        /// <returns>Объект <see cref="ICardFileContainer"/> или значение <see langword="null"/>, если он не содержится в контексте.</returns>
        ICardFileContainer? TryGetLoadedCardFileContainer(Guid cardID);

        /// <summary>
        /// Сбрасывает все загруженные объекты.
        /// </summary>
        /// <exception cref="InvalidOperationException">Код вызван вне контекста <see cref="IKrScope"/>. Необходимо создать контекст, путём вызова метода <see cref="EnterNewLevel"/>.</exception>
        ValueTask InvalidateAsync();

        /// <summary>
        /// Возвращает карточку сателлита.
        /// </summary>
        /// <param name="mainCardID">Идентификатор основной карточки.</param>
        /// <param name="taskID">Идентификатор задания, если сателлит относится к заданию.</param>
        /// <param name="satelliteTypeID">Идентификатор типа сателлита.</param>
        /// <param name="noLockingMainCard">Значение <see langword="true"/>, если не следует выполнять блокировку основной карточки при создании сателлита, иначе - <see langword="false"/>.</param>
        /// <param name="isStore">Значение <see langword="true"/>, если загруженная карточка должна быть сохранена в контексте <see cref="IKrScope"/>, иначе - <see langword="false"/>.</param>
        /// <param name="validationResult">
        /// Объект, выполняющий построение результатов валидации или значение <see langword="null"/>, если результат валидации должен быть записан в <see cref="ValidationResult"/>.<br/>
        ///
        /// Параметр является обязательным, при выполнении метода вне контекста <see cref="IKrScope"/>.
        /// </param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Карточка сателлита или значение <see langword="null"/>, если произошла ошибка.</returns>
        /// <remarks>Если карточка сателлита не существует, то она автоматически создаётся.</remarks>
        ValueTask<Card?> GetSatelliteAsync(
            Guid mainCardID,
            Guid? taskID,
            Guid satelliteTypeID,
            bool noLockingMainCard = false,
            bool isStore = true,
            IValidationResultBuilder? validationResult = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает карточку сателлита.
        /// </summary>
        /// <param name="mainCardID">Идентификатор основной карточки.</param>
        /// <param name="taskID">Идентификатор задания, если сателлит относится к заданию.</param>
        /// <param name="satelliteTypeID">Идентификатор типа сателлита.</param>
        /// <param name="isStore">Значение <see langword="true"/>, если загруженная карточка должна быть сохранена в контексте <see cref="IKrScope"/>, иначе - <see langword="false"/>.</param>
        /// <param name="validationResult">
        /// Объект, выполняющий построение результатов валидации или значение <see langword="null"/>, если результат валидации должен быть записан в <see cref="ValidationResult"/>.<br/>
        ///
        /// Параметр является обязательным, при выполнении метода вне контекста <see cref="IKrScope"/>.
        /// </param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Карточка сателлита или значение <see langword="null"/>, если произошла ошибка.</returns>
        /// <remarks>Если сателлит не существует, то не создаёт его.</remarks>
        ValueTask<Card?> TryGetSatelliteAsync(
            Guid mainCardID,
            Guid? taskID,
            Guid satelliteTypeID,
            bool isStore = true,
            IValidationResultBuilder? validationResult = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает карточку сателлита, загруженную в контекст <see cref="IKrScope"/>.
        /// </summary>
        /// <param name="mainCardID">Идентификатор основной карточки.</param>
        /// <param name="taskID">Идентификатор задания, если сателлит относится к заданию.</param>
        /// <param name="satelliteTypeID">Идентификатор типа сателлита.</param>
        /// <returns>Сателлит или значение <see langword="null"/>, если он не загружен в контекст <see cref="IKrScope"/>.</returns>
        /// <exception cref="InvalidOperationException">Код вызван вне контекста <see cref="IKrScope"/>. Необходимо создать контекст, путём вызова метода <see cref="EnterNewLevel"/>.</exception>
        Card? TryGetLoadedSatellite(
            Guid mainCardID,
            Guid? taskID,
            Guid satelliteTypeID);

        #endregion
    }
}
