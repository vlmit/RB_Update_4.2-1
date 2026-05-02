using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Extensions.Default.Server.Workflow.KrCompilers;
using Tessa.Extensions.Default.Server.Workflow.KrObjectModel;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Scope;
using Tessa.Extensions.Default.Shared;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform;
using Tessa.Platform.Collections;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow
{
    /// <summary>
    /// Предоставляет вспомогательные методы используемые в подсистеме маршрутов.
    /// </summary>
    public static class KrProcessExtensions
    {
        #region Constants And Static Fields

        private const string LaunchOnLevel = nameof(LaunchOnLevel);

        private const string SingleRunList = nameof(SingleRunList);

        private const string KrProcessTrace = nameof(KrProcessTrace);

        private const string KrProcessClientCommands = nameof(KrProcessClientCommands);

        private const string LaunchedRunners = nameof(LaunchedRunners);

        #endregion

        #region Card Extensions

        /// <summary>
        /// Возвращает значение, показывающее, что указанный сателлит содержит информацию по основному процессу <see cref="KrConstants.KrProcessName"/>.
        /// </summary>
        /// <param name="satellite">Проверяемая карточка основного сателлита.</param>
        /// <returns>Значение <see langword="true"/>, если указанный сателлит содержит информацию по основному процессу <see cref="KrConstants.KrProcessName"/>, иначе - <see langword="false"/>.</returns>
        public static bool IsMainProcessStarted(
            this Card satellite)
        {
            KrErrorHelper.AssertKrSatellite(satellite);

            return satellite.Sections.TryGetValue("WorkflowProcesses", out var wpSec)
                && wpSec.TryGetRows()?.Any(p => p.Get<string>("TypeName") == KrConstants.KrProcessName) == true;
        }

        #endregion

        #region CardStoreRequest Extensions

        private const string IntermediateApplyKey = StorageHelper.SystemKeyPrefix + "IntermediateApply";

        /// <summary>
        /// Выполняет установку флага промежуточного сохранения в запрос на сохранение карточки.
        /// </summary>
        /// <param name="request"><inheritdoc cref="CardStoreRequest" path="/summary"/></param>
        /// <param name="value">Значение устанавливаемого флага.</param>
        public static void SetIntermediateApply(
            this CardStoreRequest request,
            bool value = true)
        {
            ThrowIfNull(request);

            if (value)
            {
                request.Info[IntermediateApplyKey] = BooleanBoxes.True;
            }
            else
            {
                request.TryGetInfo()?.Remove(IntermediateApplyKey);
            }
        }

        /// <summary>
        /// Возвращает значение флага промежуточного сохранения из запроса на сохранение карточки.
        /// </summary>
        /// <param name="request"><inheritdoc cref="CardStoreRequest" path="/summary"/></param>
        public static bool IsIntermediateApply(
            this CardStoreRequest request)
        {
            return NotNullOrThrow(request)
                .TryGetInfo()?
                .TryGet<bool>(IntermediateApplyKey) ?? false;
        }

        #endregion

        #region CardGetRequest Extensions

        private const string IgnoreStoreKrSatelliteInKrScopeKey = StorageHelper.SystemKeyPrefix + "IgnoreStoreKrSatelliteInKrScope";

        /// <summary>
        /// Устанавливает значение, показывающее, что при загрузке карточки не надо сохранять информацию по основному сателлиту (<see cref="DefaultCardTypes.KrSatelliteTypeID"/>) карточки в <see cref="IKrScope"/>.
        /// </summary>
        /// <param name="request">Хранилище, в котором устанавливается флаг.</param>
        /// <param name="value">Значение <see langword="true"/>, если при загрузке карточки не надо сохранять информацию по основному сателлиту (<see cref="DefaultCardTypes.KrSatelliteTypeID"/>) карточки в <see cref="IKrScope"/>, иначе - <see langword="false"/>.</param>
        public static void SetIgnoreStoreKrSatelliteInKrScope(
            this CardGetRequest request,
            bool value = true)
        {
            ThrowIfNull(request);

            if (value)
            {
                request.Info[IgnoreStoreKrSatelliteInKrScopeKey] = BooleanBoxes.True;
            }
            else
            {
                request.TryGetInfo()?.Remove(IgnoreStoreKrSatelliteInKrScopeKey);
            }
        }

        /// <summary>
        /// Возвращает значение, показывающее, что при загрузке карточки не надо сохранять информацию по основному сателлиту (<see cref="DefaultCardTypes.KrSatelliteTypeID"/>) карточки в <see cref="IKrScope"/>.
        /// </summary>
        /// <param name="request">Хранилище, из которого запрашивается значение флага.</param>
        /// <returns>Значение <see langword="true"/>, если при загрузке карточки не надо сохранять информацию по основному сателлиту (<see cref="DefaultCardTypes.KrSatelliteTypeID"/>) карточки в <see cref="IKrScope"/>, иначе - <see langword="false"/>.</returns>
        public static bool IsIgnoreStoreKrSatelliteInKrScope(
            this CardGetRequest request) =>
            NotNullOrThrow(request).TryGetInfo()?.TryGet<bool>(IgnoreStoreKrSatelliteInKrScopeKey) ?? false;

        #endregion

        #region IKrScope Extensions

        /// <summary>
        /// Возвращает значение, показывающее, что процесс с указанным идентификатором запускается первый раз за запрос.
        /// </summary>
        /// <param name="scope">Объект, предоставляющий методы для работы с текущим контекстом подсистемы маршрутов, содержащим разделяемые карточки.</param>
        /// <param name="processID">Идентификатор процесса.</param>
        /// <returns>Значение <see langword="true"/>, если процесс с указанным идентификатором запускается первый раз за запрос, иначе - <see langword="false"/>.</returns>
        public static bool FirstLaunchPerRequest(
            this IKrScope scope,
            Guid processID)
        {
            ThrowIfNull(scope);
            AssertKrScope(scope);

            return !GetListFromInfo(scope, LaunchOnLevel, processID).Contains(scope.CurrentLevel.LevelID);
        }

        /// <summary>
        /// Добавляет информацию о запуске процесса в рамках запроса.
        /// </summary>
        /// <param name="scope">Объект, предоставляющий методы для работы с текущим контекстом подсистемы маршрутов, содержащим разделяемые карточки.</param>
        /// <param name="processID">Идентификатор процесса.</param>
        public static void AddToLaunchedLevels(
            this IKrScope scope,
            Guid processID)
        {
            ThrowIfNull(scope);
            AssertKrScope(scope);

            GetListFromInfo(scope, LaunchOnLevel, processID).Add(scope.CurrentLevel.LevelID);
        }

        /// <summary>
        /// Запрещает повторное выполнение процесса за запрос.
        /// </summary>
        /// <param name="scope">Объект, предоставляющий методы для работы с текущим контекстом подсистемы маршрутов, содержащим разделяемые карточки.</param>
        /// <param name="processID">Идентификатор процесса.</param>
        public static void DisableMultirunForRequest(
            this IKrScope scope,
            Guid processID)
        {
            ThrowIfNull(scope);
            AssertKrScope(scope);

            GetListFromInfo(scope, SingleRunList, processID).Add(scope.CurrentLevel.LevelID);
        }

        /// <summary>
        /// Возвращает значение, показывающее разрешено ли запускать процесс повторно за запрос.
        /// </summary>
        /// <param name="scope">Объект, предоставляющий методы для работы с текущим контекстом подсистемы маршрутов, содержащим разделяемые карточки.</param>
        /// <param name="processID">Идентификатор процесса.</param>
        /// <returns>Значение, <see langword="true"/>, если разрешён повторный запуск процесса за запрос, иначе - <see langword="false"/>.</returns>
        public static bool MultirunEnabled(
            this IKrScope scope,
            Guid processID)
        {
            ThrowIfNull(scope);
            AssertKrScope(scope);

            return !GetListFromInfo(scope, SingleRunList, processID).Contains(scope.CurrentLevel.LevelID);
        }

        /// <summary>
        /// Возвращает список, содержащий информацию по истории выполнения.
        /// </summary>
        /// <param name="scope">Объект, предоставляющий методы для работы с текущим контекстом подсистемы маршрутов, содержащим разделяемые карточки.</param>
        /// <returns>Список, содержащий информацию по истории выполнения или значение <see langword="null"/>, если выполнение происходит вне контекста <paramref name="scope"/>.</returns>
        public static List<KrProcessTraceItem> GetKrProcessRunnerTrace(
            this IKrScope scope)
        {
            ThrowIfNull(scope);

            if (!scope.Exists)
            {
                return null;
            }

            if (!scope.Info.TryGetValue(KrProcessTrace, out var traceObj)
                || traceObj is not List<KrProcessTraceItem> trace)
            {
                trace = new List<KrProcessTraceItem>();
                scope.Info[KrProcessTrace] = trace;
            }

            return trace;
        }

        /// <summary>
        /// Добавляет новую запись в историю выполнения процесса.
        /// </summary>
        /// <param name="scope">Объект, предоставляющий методы для работы с текущим контекстом подсистемы маршрутов, содержащим разделяемые карточки.</param>
        /// <param name="traceItem">Элемент истории выполнения процесса.</param>
        public static void TryAddToTrace(this IKrScope scope, KrProcessTraceItem traceItem)
        {
            ThrowIfNull(scope);
            ThrowIfNull(traceItem);

            scope.GetKrProcessRunnerTrace()?.Add(traceItem);
        }

        /// <summary>
        /// Возвращает список клиентских команд.
        /// </summary>
        /// <param name="scope">Объект, предоставляющий методы для работы с текущим контекстом подсистемы маршрутов, содержащим разделяемые карточки.</param>
        /// <returns>Список клиентских команд или значение <see langword="null"/>, если текущий код выполнялся вне контекста <see cref="KrScopeContext"/>.</returns>
        public static List<KrProcessClientCommand> GetKrProcessClientCommands(
            this IKrScope scope)
        {
            ThrowIfNull(scope);

            if (!scope.Exists)
            {
                return null;
            }

            if (!scope.Info.TryGetValue(KrProcessClientCommands, out var commandsListObj)
                || commandsListObj is not List<KrProcessClientCommand> commandsList)
            {
                commandsList = new List<KrProcessClientCommand>();
                scope.Info[KrProcessClientCommands] = commandsList;
            }

            return commandsList;
        }

        /// <summary>
        /// Возвращает список клиентских команд.
        /// </summary>
        /// <param name="scope">Объект, предоставляющий методы для работы с текущим контекстом подсистемы маршрутов, содержащим разделяемые карточки.</param>
        /// <returns>Список клиентских команд или значение <see langword="null"/>, если его не удалось получить.</returns>
        public static List<KrProcessClientCommand> TryGetKrProcessClientCommands(
            this IKrScope scope)
        {
            ThrowIfNull(scope);

            if (!scope.Exists)
            {
                return null;
            }

            if (scope.Info.TryGetValue(KrProcessClientCommands, out var commandsListObj)
                && commandsListObj is List<KrProcessClientCommand> commandsList)
            {
                return commandsList;
            }

            return null;
        }

        /// <summary>
        /// Добавляет клиентскую команду, если список команд доступен.
        /// </summary>
        /// <param name="scope">Объект, предоставляющий методы для работы с текущим контекстом подсистемы маршрутов, содержащим разделяемые карточки.</param>
        /// <param name="clientCommand">Команда, формируемая на сервере при работе процесса Kr и возвращаемая на клиент для дальнейшей интерпретации.</param>
        public static void TryAddClientCommand(this IKrScope scope, KrProcessClientCommand clientCommand)
        {
            ThrowIfNull(scope);
            ThrowIfNull(clientCommand);

            scope.GetKrProcessClientCommands()?.Add(clientCommand);
        }

        /// <summary>
        /// Добавляет информацию о том, что для указанного процесса запущен обработчик.
        /// </summary>
        /// <param name="scope">Объект, предоставляющий методы для работы с текущим контекстом подсистемы маршрутов, содержащим разделяемые карточки.</param>
        /// <param name="processID">Идентификатор процесса.</param>
        public static void AddLaunchedRunner(
            this IKrScope scope,
            Guid processID)
        {
            ThrowIfNull(scope);

            if (!scope.Exists)
            {
                return;
            }

            if (!scope.Info.TryGetValue(LaunchedRunners, out var runnersObj)
                || runnersObj is not List<Guid> runnersList)
            {
                runnersList = new List<Guid>();
                scope.Info[LaunchedRunners] = runnersList;
            }

            runnersList.Add(processID);
        }

        /// <summary>
        /// Возвращает значение, показывающее, запущен ли для указанного процесса раннер или нет.
        /// </summary>
        /// <param name="scope">Объект, предоставляющий методы для работы с текущим контекстом подсистемы маршрутов, содержащим разделяемые карточки.</param>
        /// <param name="processID">Идентификатор процесса.</param>
        /// <returns>Значение, показывающее, запущен ли для указанного процесса раннер или нет.</returns>
        public static bool HasLaunchedRunner(
            this IKrScope scope,
            Guid processID)
        {
            ThrowIfNull(scope);

            if (scope.Exists
                && scope.Info.TryGetValue(LaunchedRunners, out var runnersObj)
                && runnersObj is List<Guid> runnersList)
            {
                return runnersList.Contains(processID);
            }

            return false;
        }

        /// <summary>
        /// Удаляет информацию о том, что для указанного процесса запущен раннер.
        /// </summary>
        /// <param name="scope">Объект, предоставляющий методы для работы с текущим контекстом подсистемы маршрутов, содержащим разделяемые карточки.</param>
        /// <param name="processID">Идентификатор процесса.</param>
        public static void RemoveLaunchedRunner(
            this IKrScope scope,
            Guid processID)
        {
            ThrowIfNull(scope);

            if (scope.Exists
                && scope.Info.TryGetValue(LaunchedRunners, out var runnersObj)
                && runnersObj is List<Guid> runnersList)
            {
                runnersList.Remove(processID);
            }
        }

        #endregion

        #region KrProcessRunnerMode Extensions

        /// <summary>
        /// Возвращает строку локализации соответствующую названию заданного режима.
        /// </summary>
        /// <param name="mode">Режим выполнения маршрута.</param>
        /// <returns>Строка локализации соответствующая названию заданного режима.</returns>
        public static string GetCaption(
            this KrProcessRunnerMode mode)
        {
            return mode switch
            {
                KrProcessRunnerMode.Sync => "$KrProcess_SyncMode",
                KrProcessRunnerMode.Async => "$KrProcess_AsyncMode",
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null),
            };
        }

        #endregion

        #region IObjectModelMapper Extensions

        /// <summary>
        /// Возвращает этапы из шаблона этапов.
        /// </summary>
        /// <param name="objectModelMapper"><inheritdoc cref="IObjectModelMapper" path="/summary"/></param>
        /// <param name="template"><inheritdoc cref="IKrStageTemplate" path="/summary"/></param>
        /// <param name="runtimeStages">Этапы из шаблона <paramref name="template"/>.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить выполнение асинхронной задачи.</param>
        /// <returns>Этапы из шаблона этапов.</returns>
        public static async ValueTask<SealableObjectList<Stage>> GetTemplateStagesAsync(
            this IObjectModelMapper objectModelMapper,
            IKrStageTemplate template,
            IReadOnlyList<IKrRuntimeStage> runtimeStages,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(objectModelMapper);

            // Параметры template и stages будут проверены в CardRowsToObjectModelAsync.
            var templateProcess = await objectModelMapper.CardRowsToObjectModelAsync(
                stageTemplate: template,
                runtimeStages: runtimeStages,
                primaryPci: null,
                initialStage: false,
                saveInitialStages: false,
                cancellationToken: cancellationToken);

            return templateProcess.Stages;
        }

        #endregion

        #region Private Methods

        private static List<Guid> GetListFromInfo(
            IKrScope scope,
            string listKey,
            Guid processID)
        {
            Dictionary<Guid, List<Guid>> singleRunDict;
            if ((singleRunDict = scope.Info.TryGet<Dictionary<Guid, List<Guid>>>(listKey)) is null)
            {
                singleRunDict = new Dictionary<Guid, List<Guid>>();
                scope.Info[listKey] = singleRunDict;
            }

            singleRunDict.TryGetValue(processID, out var singleRunList);

            if (singleRunList is null)
            {
                singleRunList = new List<Guid>();
                singleRunDict[processID] = singleRunList;
            }

            return singleRunList;
        }

        private static void AssertKrScope(IKrScope scope)
        {
            if (!scope.Exists)
            {
                throw new InvalidOperationException($"Execution without {nameof(IKrScope)} object.");
            }
        }

        #endregion
    }
}
