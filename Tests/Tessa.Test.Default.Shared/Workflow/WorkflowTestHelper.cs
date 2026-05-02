#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.SmartMerge;
using Tessa.Test.Default.Shared.Cards;
using Tessa.Test.Default.Shared.Kr;
using Tessa.Workflow;
using Tessa.Workflow.Actions.Descriptors;
using Tessa.Workflow.Helpful;
using Tessa.Workflow.Storage;

namespace Tessa.Test.Default.Shared.Workflow
{
    /// <summary>
    /// Содержит вспомогательные методы используемые в тестах WorkflowEngine.
    /// </summary>
    public static class WorkflowTestHelper
    {
        #region Constants

        /// <summary>
        /// Имя ключа, по которому в <see cref="ICardLifecycleCompanionData.OtherResponses"/> содержится информация о последнем запросе на обработку сигнала в <see cref="IWorkflowEngineProcessor"/>. Тип значения: <see cref="IWorkflowEngineProcessRequest"/>.
        /// </summary>
        public const string WorkflowEngineProcessRequestKey = nameof(WorkflowEngineProcessRequest);

        /// <summary>
        /// Имя ключа, по которому в <see cref="ICardLifecycleCompanionData.OtherResponses"/> содержится информация о последнем результате обработки сигнала в <see cref="IWorkflowEngineProcessor"/>. Тип значения: <see cref="IWorkflowEngineProcessResult"/>.
        /// </summary>
        public const string WorkflowEngineProcessResultKey = nameof(WorkflowEngineProcessResult);

        #endregion

        #region Public Methods

        /// <summary>
        /// Импортирует все карточки из папки Workflow из ресурсов указанной сборки.
        /// </summary>
        /// <param name="assembly">Сборка, содержащая ресурсы.</param>
        /// <param name="cardManager">Объект, управляющий операциями с карточками.</param>
        /// <param name="cardRepository">Репозиторий для управления типами карточек.</param>
        /// <param name="cardPredicateAsync">Функция определяющая возможность импорта карточки или значение по умолчанию для типа, если фильтрация не выполняется.</param>
        /// <param name="getMergeOptionsFuncAsync">Функция возвращающая параметры объединения для файла с заданным именем или значение по умолчанию для типа, если используются параметры по умолчанию. Параметры: имя файла для которого запрашиваются параметры объединения.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Асинхронная задача.</returns>
        public static Task ImportWorkflowCardsAsync(
            Assembly assembly,
            ICardManager cardManager,
            ICardRepository cardRepository,
            Func<Card, CancellationToken, ValueTask<bool>>? cardPredicateAsync = null,
            Func<string, CancellationToken, ValueTask<ICardMergeOptions>>? getMergeOptionsFuncAsync = null,
            CancellationToken cancellationToken = default) =>
            TestCardHelper.ImportCardsFromDirectoryAsync(
                assembly,
                cardManager,
                cardRepository,
                "Workflow",
                cardPredicateAsync: cardPredicateAsync,
                getMergeOptionsFuncAsync: getMergeOptionsFuncAsync,
                cancellationToken: cancellationToken);

        /// <summary>
        /// Импортирует указанную карточку Workflow из встроенных ресурсов указанной сборки.
        /// </summary>
        /// <param name="assembly">Сборка, содержащая ресурсы.</param>
        /// <param name="cardManager">Объект, управляющий операциями с карточками.</param>
        /// <param name="cardRepository">Репозиторий для управления типами карточек.</param>
        /// <param name="cardName">Имя карточки с расширением в папке сборки <paramref name="assembly"/>\Cards\Workflow.</param>
        /// <param name="mergeOptions">Опции слияния или <see langword="null"/>, если слияние выполняется с настройками по умолчанию.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Асинхронная задача.</returns>
        public static Task ImportWorkflowCardAsync(
            Assembly assembly,
            ICardManager cardManager,
            ICardRepository cardRepository,
            string cardName,
            ICardMergeOptions? mergeOptions = default,
            CancellationToken cancellationToken = default) =>
            TestCardHelper.ImportCardFromTestResourcesAsync(
                assembly,
                Path.Combine("Workflow", cardName),
                cardManager,
                cardRepository,
                mergeOptions: mergeOptions,
                throwOnFailure: true,
                cancellationToken: cancellationToken);

        /// <summary>
        /// Возвращает основную информацию о процессе.
        /// </summary>
        /// <param name="process"><inheritdoc cref="WorkflowProcessStorage" path="/summary"/></param>
        /// <returns>Основная информацию о процессе.</returns>
        public static string GetProcessInfo(
            WorkflowProcessStorage process)
        {
            ThrowIfNull(process);

            return $"Process name: \"{process.Name}\". Process ID: {process.ID:B}. Template ID: {process.TemplateCardID:B}.";
        }

        #endregion

        #region Extensions

        /// <summary>
        /// Возвращает объект с заданным названием.
        /// </summary>
        /// <typeparam name="T">Тип объекта.</typeparam>
        /// <param name="source">Коллекция, в которой выполняется поиск.</param>
        /// <param name="name">Название объекта.</param>
        /// <returns>Найденный объект.</returns>
        /// <exception cref="InvalidOperationException">Not found <typeparamref name="T"/> with name "<paramref name="name"/>".</exception>
        public static T GetByName<T>(
            this IEnumerable<T> source,
            string name)
            where T : WorkflowTemplateStorageBase
        {
            ThrowIfNull(source);
            ThrowIfNullOrEmpty(name);

            return source.TryGetByName(name) ?? throw new InvalidOperationException($"Not found {typeof(T).Name} with name \"{name}\".");
        }

        /// <summary>
        /// Возвращает объект с заданным названием.
        /// </summary>
        /// <typeparam name="T">Тип объекта.</typeparam>
        /// <param name="source">Коллекция, в которой выполняется поиск.</param>
        /// <param name="name">Название объекта.</param>
        /// <returns>Найденный объект или значение <see langword="null"/>, если он не найден.</returns>
        public static T? TryGetByName<T>(
            this IEnumerable<T> source,
            string name)
            where T : WorkflowTemplateStorageBase
        {
            ThrowIfNull(source);
            ThrowIfNullOrEmpty(name);

            return source.FirstOrDefault(n => n.Name == name);
        }

        /// <summary>
        /// Возвращает действие с заданным названием.
        /// </summary>
        /// <param name="process"><inheritdoc cref="WorkflowProcessStorage" path="/summary"/></param>
        /// <param name="name">Название действия.</param>
        /// <returns>Найденное действие или значение <see langword="null"/>, если его не удалось найти.</returns>
        public static WorkflowActionStorage GetAction(
            this WorkflowProcessStorage process,
            string name) =>
            process.TryGetAction(name) ?? throw new InvalidOperationException($"Not found action with name \"{name}\". {GetProcessInfo(process)}");

        /// <summary>
        /// Возвращает действие с заданным идентификатором.
        /// </summary>
        /// <param name="process"><inheritdoc cref="WorkflowProcessStorage" path="/summary"/></param>
        /// <param name="id">Идентификатор действия.</param>
        /// <returns>Найденное действие или значение <see langword="null"/>, если его не удалось найти.</returns>
        public static WorkflowActionStorage GetAction(
            this WorkflowProcessStorage process,
            Guid id) =>
            process.TryGetAction(id) ?? throw new InvalidOperationException($"Not found action with ID={id:B}. {GetProcessInfo(process)}");

        /// <summary>
        /// Возвращает действие с заданным названием.
        /// </summary>
        /// <param name="process"><inheritdoc cref="WorkflowProcessStorage" path="/summary"/></param>
        /// <param name="name">Название действия.</param>
        /// <returns>Найденное действие или значение <see langword="null"/>, если его не удалось найти.</returns>
        public static WorkflowActionStorage? TryGetAction(
            this WorkflowProcessStorage process,
            string name)
        {
            ThrowIfNull(process);
            ThrowIfNullOrEmpty(name);

            foreach (var node in process.Nodes)
            {
                var action = node.OrderedActions.TryGetByName(name);

                if (action is not null)
                {
                    return action;
                }
            }

            return null;
        }

        /// <summary>
        /// Возвращает действие с заданным идентификатором.
        /// </summary>
        /// <param name="process"><inheritdoc cref="WorkflowProcessStorage" path="/summary"/></param>
        /// <param name="actionID">Идентификатор действия.</param>
        /// <returns>Найденное действие или значение <see langword="null"/>, если его не удалось найти.</returns>
        public static WorkflowActionStorage? TryGetAction(
            this WorkflowProcessStorage process,
            Guid actionID)
        {
            ThrowIfNull(process);

            foreach (var node in process.Nodes)
            {
                if (node.ActionsByID.TryGetValue(actionID, out var action))
                {
                    return action;
                }
            }

            return null;
        }

        /// <summary>
        /// Возвращает действие с заданным названием.
        /// </summary>
        /// <param name="process"><inheritdoc cref="WorkflowProcessStorage" path="/summary"/></param>
        /// <param name="nodeName">Название узла.</param>
        /// <param name="actionName">Название действия.</param>
        /// <returns>Найденное действие.</returns>
        public static WorkflowActionStorage GetAction(
            this WorkflowProcessStorage process,
            string nodeName,
            string actionName)
        {
            ThrowIfNull(process);
            ThrowIfNullOrEmpty(actionName);

            var node = process.Nodes.GetByName(nodeName);
            return node.OrderedActions.GetByName(actionName);
        }

        /// <summary>
        /// Возвращает действие с заданным идентификатором.
        /// </summary>
        /// <param name="process"><inheritdoc cref="WorkflowProcessStorage" path="/summary"/></param>
        /// <param name="nodeID">Идентификатор узла.</param>
        /// <param name="actionID">Идентификатор действия.</param>
        /// <returns>Найденное действие.</returns>
        public static WorkflowActionStorage GetAction(
            this WorkflowProcessStorage process,
            Guid nodeID,
            Guid actionID)
        {
            ThrowIfNull(process);

            if (!process.NodesByID.TryGetValue(nodeID, out var node))
            {
                throw new InvalidOperationException($"Not found node with ID={nodeID:B}. {GetProcessInfo(process)}");
            }

            if (node.ActionsByID.TryGetValue(actionID, out var action))
            {
                return action;
            }

            throw new InvalidOperationException($"Not found action with ID={actionID:B}. {GetProcessInfo(process)}");
        }

        /// <summary>
        /// Добавляет узел в шаблон процесса.
        /// </summary>
        /// <param name="process"><inheritdoc cref="WorkflowProcessStorage" path="/summary"/></param>
        /// <param name="name">Название узла или значение <see langword="null"/>, если оно должно быть создано автоматически.</param>
        /// <returns>Созданный узел.</returns>
        public static WorkflowNodeStorage AddNode(
            this WorkflowProcessStorage process,
            string? name = null)
        {
            ThrowIfNull(process);

            var newNode = process.Nodes.Add();
            newNode.SetName(
                name ?? "Node",
                true);
            newNode.ID = Guid.NewGuid();

            return newNode;
        }

        /// <summary>
        /// Добавляет действие в шаблон процесса.
        /// </summary>
        /// <param name="node"><inheritdoc cref="WorkflowNodeStorage" path="/summary"/></param>
        /// <param name="descriptor"><inheritdoc cref="WorkflowActionDescriptor" path="/summary"/></param>
        /// <param name="name">Название действия или значение <see langword="null"/>, если оно должно быть создано автоматически.</param>
        /// <returns>Созданное действие.</returns>
        public static WorkflowActionStorage AddAction(
            this WorkflowNodeStorage node,
            WorkflowActionDescriptor descriptor,
            string? name = null)
        {
            ThrowIfNull(node);
            ThrowIfNull(descriptor);

            var newOrder = node.Actions.Count;
            var newAction = node.Actions.Add();
            newAction.SetName(
                name ?? $"Action. ActionTypeID: {descriptor.ID:B}. Node: {node.Name} (ID={node.ID:B})",
                true);
            newAction.ID = Guid.NewGuid();
            newAction.ActionTypeID = descriptor.ID;
            newAction.Order = newOrder;
            newAction.Version = descriptor.Version;

            return newAction;
        }

        /// <summary>
        /// Добавляет действие в шаблон процесса.
        /// </summary>
        /// <param name="process"><inheritdoc cref="WorkflowProcessStorage" path="/summary"/></param>
        /// <param name="descriptor"><inheritdoc cref="WorkflowActionDescriptor" path="/summary"/></param>
        /// <param name="name">Название действия или значение <see langword="null"/>, если оно должно быть создано автоматически.</param>
        /// <returns>Созданное действие.</returns>
        /// <remarks>Метод создаёт узел и добавляет в него действие.</remarks>
        public static WorkflowActionStorage AddAction(
            this WorkflowProcessStorage process,
            WorkflowActionDescriptor descriptor,
            string? name = null)
        {
            ThrowIfNull(process);
            ThrowIfNull(descriptor);

            var node = process.AddNode($"Node for action with type ID={descriptor.ID:B}");
            return node.AddAction(descriptor, name);
        }

        /// <summary>
        /// Добавляет связь между узлами, содержащими по действия <paramref name="fromActionNode"/> и <paramref name="toActionNode"/>, в шаблон процесса.
        /// </summary>
        /// <param name="process"><inheritdoc cref="WorkflowProcessStorage" path="/summary"/></param>
        /// <param name="fromActionNode">Действие, расположенное в узле, откуда начинается связь.</param>
        /// <param name="toActionNode">Действие, расположенное в узле, где заканчивается связь.</param>
        /// <param name="signalMode"><inheritdoc cref="WorkflowSignalProcessingMode" path="/summary"/></param>
        /// <param name="linkMode"><inheritdoc cref="WorkflowLinkMode" path="/summary"/></param>
        /// <param name="lockProcess">Значение <see langword="true"/>, если процесс должен блокироваться при выполнении асинхронной операции, иначе - <see langword="false"/>.</param>
        /// <param name="retryAllowed">Значение <see langword="true"/>, если разрешена повторная асинхронная обработка сигнала при возникновении ошибки его обработки, иначе - <see langword="false"/>.</param>
        /// <param name="inCondition">Условия для входа в узел по связи.</param>
        /// <param name="outDescription">Условие для выхода из узла по связи.</param>
        /// <returns>Созданная связь.</returns>
        public static WorkflowLinkStorage AddLink(
            this WorkflowProcessStorage process,
            WorkflowActionStorage fromActionNode,
            WorkflowActionStorage toActionNode,
            WorkflowSignalProcessingMode signalMode = WorkflowSignalProcessingMode.Default,
            WorkflowLinkMode linkMode = WorkflowLinkMode.Default,
            bool lockProcess = true,
            bool retryAllowed = false,
            string? inCondition = null,
            string? outDescription = null)
        {
            ThrowIfNull(process);
            ThrowIfNull(fromActionNode);
            ThrowIfNull(toActionNode);

            return process.AddLink(
                NotNullOrThrow(fromActionNode.ParentObject).ID,
                NotNullOrThrow(toActionNode.ParentObject).ID,
                signalMode,
                linkMode,
                lockProcess,
                retryAllowed,
                inCondition,
                outDescription);
        }

        /// <summary>
        /// Добавляет связь между узлами с идентификаторами <paramref name="fromNodeID"/> и <paramref name="toNodeID"/>, в шаблон процесса.
        /// </summary>
        /// <param name="process"><inheritdoc cref="WorkflowProcessStorage" path="/summary"/></param>
        /// <param name="fromNodeID">Идентификатор узла, откуда начинается связь.</param>
        /// <param name="toNodeID">Идентификатор узла, где заканчивается связь.</param>
        /// <param name="signalMode"><inheritdoc cref="WorkflowSignalProcessingMode" path="/summary"/></param>
        /// <param name="linkMode"><inheritdoc cref="WorkflowLinkMode" path="/summary"/></param>
        /// <param name="retryAllowed">Значение <see langword="true"/>, если разрешена повторная асинхронная обработка сигнала при возникновении ошибки его обработки, иначе - <see langword="false"/>.</param>
        /// <param name="lockProcess">Значение <see langword="true"/>, если процесс должен блокироваться при выполнении асинхронной операции, иначе - <see langword="false"/>.</param>
        /// <param name="inCondition">Условия для входа в узел по связи.</param>
        /// <param name="outDescription">Условие для выхода из узла по связи.</param>
        /// <returns>Созданная связь.</returns>
        public static WorkflowLinkStorage AddLink(
            this WorkflowProcessStorage process,
            Guid fromNodeID,
            Guid toNodeID,
            WorkflowSignalProcessingMode signalMode = WorkflowSignalProcessingMode.Default,
            WorkflowLinkMode linkMode = WorkflowLinkMode.Default,
            bool lockProcess = true,
            bool retryAllowed = false,
            string? inCondition = null,
            string? outDescription = null)
        {
            ThrowIfNull(process);

            var newLink = process.Links.Add();
            newLink.SetName($"Link: {fromNodeID:B} -> {toNodeID:B}", true);
            newLink.ID = Guid.NewGuid();
            newLink.FromNode = fromNodeID;
            newLink.ToNode = toNodeID;

            newLink.SignalProcessingMode = signalMode;
            newLink.LinkMode = linkMode;
            newLink.LockProcess = lockProcess;
            newLink.RetryAllowed = retryAllowed;

            newLink.InCondition = inCondition;
            newLink.OutCondition = outDescription;

            return newLink;
        }

        #endregion
    }
}
