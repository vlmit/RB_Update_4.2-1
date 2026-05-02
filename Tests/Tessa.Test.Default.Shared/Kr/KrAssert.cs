#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using Tessa.BusinessCalendar;
using Tessa.Cards;
using Tessa.Cards.Metadata;
using Tessa.Extensions.Default.Shared;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform;
using Tessa.Platform.Data;
using Tessa.Platform.Storage;
using Tessa.Test.Default.Shared.Kr.Routes;

namespace Tessa.Test.Default.Shared.Kr
{
    /// <summary>
    /// Предоставляет статические методы для выражения утверждений.
    /// </summary>
    /// <seealso href="https://github.com/nunit/docs/wiki/Assertions">NUnit assertions</seealso>
    public static class KrAssert
    {
        #region Static Fields

        /// <inheritdoc cref="KrConstants.KrTaskTypeIDList"/>
        /// <remarks>Оптимизация, для использования при проверке условий.</remarks>
        private static readonly object[] KrTaskTypeIDObjList = KrConstants.KrTaskTypeIDList.Cast<object>().ToArray();

        #endregion

        #region Public Methods

        /// <summary>
        /// Проверяет, что в карточке, которой управляет указанный объект, есть хотя бы один этап находящийся в состоянии <see cref="KrStageState.Active"/>.
        /// </summary>
        /// <param name="clc"><inheritdoc cref="ICardLifecycleCompanion" path="/summary"/></param>
        public static void IsAtLeastOneStageActive(
            ICardLifecycleCompanion clc)
        {
            ThrowIfNull(clc);

            IsAtLeastOneStageActive(clc.GetCardOrThrow());
        }

        /// <summary>
        /// Проверяет, что в карточке есть хотя бы один этап находящийся в состоянии <see cref="KrStageState.Active"/>.
        /// </summary>
        /// <param name="card">Проверяемая карточка.</param>
        public static void IsAtLeastOneStageActive(Card card)
        {
            ThrowIfNull(card);

            var states = card
                .GetStagesSection()
                .Rows
                .Select(p => p.Get<int>(KrConstants.KrStages.StateID));

            Assert.That(states, Has.Some.EqualTo(Int32Boxes.Box(KrStageState.Active.ID)));
        }

        /// <summary>
        /// Проверяет, что в карточке, которой управляет указанный объект, ни один из этапов не находится в состоянии <see cref="KrStageState.Active"/>.
        /// </summary>
        /// <param name="clc"><inheritdoc cref="ICardLifecycleCompanion" path="/summary"/></param>
        public static void IsNoneStageActive(
            ICardLifecycleCompanion clc)
        {
            ThrowIfNull(clc);

            IsNoneStageActive(clc.GetCardOrThrow());
        }

        /// <summary>
        /// Проверяет, что в карточке ни один из этапов не находится в состоянии <see cref="KrStageState.Active"/>.
        /// </summary>
        /// <param name="card">Проверяемая карточка.</param>
        public static void IsNoneStageActive(Card card)
        {
            ThrowIfNull(card);

            var rows = card
                .GetStagesSection()
                .Rows;

            Assert.That(
                rows.Select(p => p.Get<int>(KrConstants.KrStages.StateID)),
                Has.None.EqualTo(KrStageState.Active.ID),
                () =>
                {
                    var sb = StringBuilderHelper
                        .Acquire()
                        .AppendLine($"Has not stage is state ID = {KrStageState.Active.ID}:");

                    KrTestHelper.AddStageRowsInfo(
                        sb,
                        rows,
                        true);

                    return sb.ToStringAndRelease();
                });
        }

        /// <summary>
        /// Проверяет, что этап, информация о котором содержится в указанной строке, имеет заданное состояние.
        /// </summary>
        /// <param name="row">Строка, содержащая информацию об этапе. Строка должна соответствовать строке секции <see cref="KrConstants.KrStages.Name"/>.</param>
        /// <param name="expectedState">Ожидаемое состояние этапа.</param>
        public static void StateIs(
            CardRow row,
            KrStageState expectedState)
        {
            ThrowIfNull(row);

            var state = (KrStageState) row.Get<int>(KrConstants.KrStages.StateID);

            Assert.That(
                state,
                Is.EqualTo(expectedState),
                () => $"Expected: {expectedState.TryGetDefaultName()}{Environment.NewLine}"
                + $"But got: {state.TryGetDefaultName()}");
        }

        /// <summary>
        /// Проверяет, что карточка имеет заданное состояние.
        /// </summary>
        /// <param name="card">Проверяемая карточка.</param>
        /// <param name="expectedState">Ожидаемое состояние проверяемой карточки.</param>
        public static void StateIs(
            Card card,
            KrState expectedState)
        {
            // Значение параметра card будет проверено в KrTestHelper.GetState.

            var state = KrTestHelper.GetState(card);

            const string defaultNameStr = "<The card state does not have a default name. See ID.>";

            Assert.That(
                state,
                Is.EqualTo(expectedState),
                () => $"Expected: {expectedState.TryGetDefaultName() ?? defaultNameStr}{Environment.NewLine}"
                + $"But got: {state.TryGetDefaultName() ?? defaultNameStr}");

            if (card.Sections.TryGetValue(KrConstants.DocumentCommonInfo.Name, out var dci)
                && dci.RawFields.TryGetValue(KrConstants.DocumentCommonInfo.StateID, out var dciStateIDObj))
            {
                var dciState = (KrState) (int) dciStateIDObj!;

                Assert.That(
                    dciState,
                    Is.EqualTo(expectedState),
                    () => $"Expected: {expectedState.TryGetDefaultName() ?? defaultNameStr}{Environment.NewLine}"
                    + $"But got from section {KrConstants.DocumentCommonInfo.Name}: {dciState.TryGetDefaultName() ?? defaultNameStr}");
            }
        }

        /// <summary>
        /// Проверяет, что карточка, которой управляет указанный объект, имеет заданное состояние.
        /// </summary>
        /// <param name="clc"><inheritdoc cref="ICardLifecycleCompanion" path="/summary"/></param>
        /// <param name="expectedState">Ожидаемое состояние проверяемой карточки.</param>
        public static void StateIs(
            ICardLifecycleCompanion clc,
            KrState expectedState)
        {
            ThrowIfNull(clc);

            StateIs(clc.GetCardOrThrow(), expectedState);
        }

        /// <summary>
        /// Проверяет, что этап, информация о котором содержится в указанной строке, имеет заданный тип.
        /// </summary>
        /// <param name="row">Строка, содержащая информацию об этапе. Строка должна соответствовать строке секции <see cref="KrConstants.KrStages.Name"/>.</param>
        /// <param name="descriptor">Дескриптор типа этапа.</param>
        public static void StageTypeIs(
            CardRow row,
            StageTypeDescriptor descriptor)
        {
            ThrowIfNull(row);

            Assert.That(
                row[KrConstants.KrStages.StageTypeID],
                Is.EqualTo(descriptor.ID),
                $"Expected: {descriptor.ID} ({descriptor.Caption}){Environment.NewLine}But got: {row[KrConstants.KrStages.StageTypeID]} ({row[KrConstants.KrStages.StageTypeCaption]})");
        }

        /// <summary>
        /// Проверяет, что указанная карточка содержит хотя бы одно задание отправленное типовым процессом согласования.
        /// </summary>
        /// <param name="card">Проверяемая карточка.</param>
        public static void HasKrProcessTasks(
            Card card)
        {
            ThrowIfNull(card);

            Assert.That(
                card.TryGetTasks()?.Select(p => p.TypeID),
                Is.Not.Null.And.Not.Empty.And.Some.AnyOf(KrTaskTypeIDObjList),
                () => $"{nameof(Card)}.{nameof(Card.Tasks)} does not contain any task with type, associated with kr process." +
                $"{Environment.NewLine}" +
                $"{nameof(KrConstants)}.{nameof(KrConstants.KrTaskTypeIDList)}: {string.Join(", ", KrConstants.KrTaskTypeIDList)}");
        }

        /// <summary>
        /// Проверяет, что указанная карточка содержит хотя бы одно задание отправленное типовым процессом согласования.
        /// </summary>
        /// <param name="clc"><inheritdoc cref="ICardLifecycleCompanion" path="/summary"/></param>
        public static void HasKrProcessTasks(
            ICardLifecycleCompanion clc)
        {
            ThrowIfNull(clc);

            HasKrProcessTasks(clc.GetCardOrThrow());
        }

        /// <summary>
        /// Проверяет, что указанная карточка не содержит заданий отправленных типовым процессом согласования.
        /// </summary>
        /// <param name="card">Проверяемая карточка.</param>
        public static void HasNoKrProcessTasks(
            Card card)
        {
            ThrowIfNull(card);

            Assert.That(
                card.TryGetTasks()?.Select(p => p.TypeID),
                Is.Null.Or.Empty.Or.Not.Some.AnyOf(KrTaskTypeIDObjList),
                () => $"{nameof(Card)}.{nameof(Card.Tasks)} contain task with type, associated with kr process." +
                $"{Environment.NewLine}" +
                $"{nameof(KrConstants)}.{nameof(KrConstants.KrTaskTypeIDList)}: {string.Join(", ", KrConstants.KrTaskTypeIDList)}." +
                $"{Environment.NewLine}" +
                $"Identifiers of task types that violate the condition: {string.Join(", ", card.Tasks.Select(p => p.TypeID).Intersect(KrConstants.KrTaskTypeIDList))}");
        }

        /// <summary>
        /// Проверяет, что карточка, которой управляет указанный объект, не содержит заданий отправленных типовым процессом согласования.
        /// </summary>
        /// <param name="clc"><inheritdoc cref="ICardLifecycleCompanion" path="/summary"/></param>
        public static void HasNoKrProcessTasks(
            ICardLifecycleCompanion clc)
        {
            ThrowIfNull(clc);

            HasNoKrProcessTasks(clc.GetCardOrThrow());
        }

        /// <summary>
        /// Проверяет, что маршрут, в указанной карточке, содержит хотя бы один активный этап заданного типа.
        /// </summary>
        /// <param name="card">Карточка, содержащая проверяемый маршрут.</param>
        /// <param name="descriptor">Дескриптор типа этапа.</param>
        public static void IsStageTypeActive(
            Card card,
            StageTypeDescriptor descriptor)
        {
            // Проверка корректности значения card выполняется в GetStagesSection.

            var stages = card.GetStagesSection();
            var row = stages
                .Rows
                .FirstOrDefault(p => p.Get<int>(KrConstants.KrStages.StateID) == KrStageState.Active.ID);

            if (row is null)
            {
                Assert.Fail("Route has no active stage.");
            }

            var actualStageTypeID = row![KrConstants.KrStages.StageTypeID] as Guid?;

            Assert.That(
                actualStageTypeID,
                Is.EqualTo(descriptor.ID),
                () => $"Expected stage type id is \"{descriptor.Caption}\" but got \"{row[KrConstants.KrStages.StageTypeCaption]}\".");
        }

        /// <summary>
        /// Проверяет, что маршрут в карточке, которой управляет указанный объект, содержит хотя бы один активный этап заданного типа.
        /// </summary>
        /// <param name="clc"><inheritdoc cref="ICardLifecycleCompanion" path="/summary"/></param>
        /// <param name="descriptor">Дескриптор типа этапа.</param>
        public static void IsStageTypeActive(
            ICardLifecycleCompanion clc,
            StageTypeDescriptor descriptor)
        {
            ThrowIfNull(clc);

            IsStageTypeActive(clc.GetCardOrThrow(), descriptor);
        }

        /// <summary>
        /// Проверяет, что маршрут в карточке, которой управляет указанный объект, содержит хотя бы один этап имеющий заданное имя и состояние.
        /// </summary>
        /// <param name="clc"><inheritdoc cref="ICardLifecycleCompanion" path="/summary"/></param>
        /// <param name="stageName">Имя этапа.</param>
        /// <param name="state">Состояние этапа.</param>
        public static void StageHasState(
            ICardLifecycleCompanion clc,
            string stageName,
            KrStageState state)
        {
            ThrowIfNull(clc);

            StageHasState(clc.GetCardOrThrow(), stageName, state);
        }

        /// <summary>
        /// Проверяет, что маршрут в указанной карточке содержит хотя бы один этап имеющий заданное имя и состояние.
        /// </summary>
        /// <param name="card">Карточка, содержащая маршрут с искомым этапом.</param>
        /// <param name="stageName">Имя этапа.</param>
        /// <param name="state">Состояние этапа.</param>
        public static void StageHasState(
            Card card,
            string stageName,
            KrStageState state)
        {
            // Проверка корректности значения card выполняется в GetStagesSection.

            var stages = card.GetStagesSection();
            var row = stages
                .Rows
                .FirstOrDefault(p => string.Equals(p.Get<string>(KrConstants.KrStages.NameField), stageName, StringComparison.Ordinal));

            if (row is null)
            {
                var sb = StringBuilderHelper
                    .Acquire()
                    .AppendLine($"Route has no stage with name \"{stageName}\".")
                    .AppendLine("Actual stages:");

                KrTestHelper.AddStageRowsInfo(
                    sb,
                    stages.Rows,
                    true);

                Assert.Fail(sb.ToStringAndRelease());
            }

            Assert.That(
                row![KrConstants.KrStages.StateID],
                Is.EqualTo(Int32Boxes.Box(state.ID)),
                () => $"Expected state ID is \"{state.ID}\" at stage \"{stageName}\" but got \"{row[KrConstants.KrStages.StateID]}\".");
        }

        /// <summary>
        /// Проверяет, что маршрут во вторичном процессе в карточке, которой управляет указанный объект, содержит хотя бы один этап имеющий заданное имя и состояние.
        /// </summary>
        /// <param name="deps"><inheritdoc cref="ICardLifecycleCompanionDependencies" path="/summary"/></param>
        /// <param name="secondaryProcessID">Идентификатор вторичного процесса.</param>
        /// <param name="stageName">Имя этапа.</param>
        /// <param name="state">Состояние этапа.</param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns><inheritdoc cref="Task" path="/summary"/></returns>
        public static async Task StageHasStateFromSecondaryProcessAsync(
            ICardLifecycleCompanionDependencies deps,
            Guid secondaryProcessID,
            string stageName,
            KrStageState state,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(deps);

            var card = await KrTestHelper.GetKrSatelliteAsync(
                deps.DbScope,
                deps.CardRepository,
                secondaryProcessID,
                DefaultCardTypes.KrSecondarySatelliteTypeID,
                cancellationToken);

            StageHasState(
                card,
                stageName,
                state);
        }

        /// <summary>
        /// Проверяет, что маршрут в карточке содержит один этап с указанным именем.
        /// </summary>
        /// <param name="card">Проверяемая карточка.</param>
        /// <param name="name">Имя этапа.</param>
        public static void HasStage(
            Card card,
            string name)
        {
            ThrowIfNull(card);

            Assert.That(
                card.GetStagesSection().Rows.Select(r => r.Get<string>(KrConstants.KrStages.NameField)).ToArray(),
                Has.One.EqualTo(name));
        }

        /// <summary>
        /// Проверяет, что маршрут в карточке, которой управляет указанный объект, содержит один этап с указанным именем.
        /// </summary>
        /// <param name="clc"><inheritdoc cref="ICardLifecycleCompanion" path="/summary"/></param>
        /// <param name="name">Имя этапа.</param>
        public static void HasStage(
            ICardLifecycleCompanion clc,
            string name)
        {
            ThrowIfNull(clc);

            HasStage(clc.GetCardOrThrow(), name);
        }

        /// <summary>
        /// Проверяет, что маршрут в карточке не содержит этапа с указанным именем.
        /// </summary>
        /// <param name="card">Проверяемая карточка.</param>
        /// <param name="name">Имя этапа.</param>
        public static void HasNoStage(
            Card card,
            string name)
        {
            ThrowIfNull(card);

            Assert.That(
                card.GetStagesSection().Rows.Select(r => r.Get<string>(KrConstants.KrStages.NameField)).ToArray(),
                Has.None.EqualTo(name));
        }

        /// <summary>
        /// Проверяет, что маршрут в карточке, которой управляет указанный объект, не содержит этапа с указанным именем.
        /// </summary>
        /// <param name="clc">Объект, содержащий проверяемую карточку.</param>
        /// <param name="name">Имя этапа.</param>
        public static void HasNoStage(
            ICardLifecycleCompanion clc,
            string name)
        {
            ThrowIfNull(clc);

            HasNoStage(clc.GetCardOrThrow(), name);
        }

        /// <summary>
        /// Проверяет наличие в карточке задания указанного типа.
        /// </summary>
        /// <param name="clc"><inheritdoc cref="ICardLifecycleCompanion" path="/summary"/></param>
        /// <param name="typeID">Тип задания.</param>
        /// <param name="exactly">Число ожидаемых экземпляров заданий. Если не задано, то проверка считается успешной, если карточка содержит хотя бы одно задание указанного типа.</param>
        /// <param name="message">Сообщение, отображаемое при не выполнении проверки.</param>
        public static void HasTask(
            ICardLifecycleCompanion clc,
            Guid typeID,
            int? exactly = null,
            string? message = null)
        {
            ThrowIfNull(clc);

            HasTask(clc.GetCardOrThrow(), typeID, exactly, message);
        }

        /// <summary>
        /// Проверяет наличие в карточке задания указанного типа.
        /// </summary>
        /// <param name="card">Проверяемая карточка.</param>
        /// <param name="typeID">Тип задания.</param>
        /// <param name="exactly">Число ожидаемых заданий. Если не задано, то проверка считается успешной, если карточка содержит хотя бы одно задание указанного типа.</param>
        /// <param name="message">Сообщение, отображаемое при не выполнении проверки.</param>
        public static void HasTask(
            Card card,
            Guid typeID,
            int? exactly = default,
            string? message = null)
        {
            ThrowIfNull(card);

            var tasks = card.Tasks;

            if (exactly.HasValue)
            {
                Assert.That(
                    tasks.Select(i => i.TypeID),
                    Has.Exactly(exactly.Value).EqualTo(typeID),
                    () => GetMessageActualTasks(tasks, message));
            }
            else
            {
                Assert.That(
                    tasks.Select(i => i.TypeID),
                    Has.Some.EqualTo(typeID),
                    () => GetMessageActualTasks(tasks, message));
            }
        }

        /// <summary>
        /// Проверяет наличие в карточке указанного числа заданий.
        /// </summary>
        /// <param name="clc"><inheritdoc cref="ICardLifecycleCompanion" path="/summary"/></param>
        /// <param name="expectedCount">Ожидаемое число заданий.</param>
        /// <param name="message">Сообщение, отображаемое при не выполнении проверки.</param>
        public static void HasTask(
            ICardLifecycleCompanion clc,
            int expectedCount,
            string? message = null)
        {
            ThrowIfNull(clc);

            HasTask(clc.GetCardOrThrow(), expectedCount, message);
        }

        /// <summary>
        /// Проверяет наличие в карточке задания указанного типа.
        /// </summary>
        /// <param name="card">Проверяемая карточка.</param>
        /// <param name="expectedCount">Ожидаемое число заданий.</param>
        /// <param name="message">Сообщение, отображаемое при не выполнении проверки.</param>
        public static void HasTask(
            Card card,
            int expectedCount,
            string? message = null)
        {
            ThrowIfNull(card);

            Assert.That(
                card.Tasks.Count,
                Is.EqualTo(expectedCount),
                () => GetMessageActualTasks(card.Tasks, message));
        }

        /// <summary>
        /// Проверяет, что в карточке не содержится заданий указанного типа.
        /// </summary>
        /// <param name="card">Проверяемая карточка.</param>
        /// <param name="typeID">Тип задания, которого не должно быть в карточке.</param>
        public static void HasNoTask(
            Card card,
            Guid typeID)
        {
            ThrowIfNull(card);

            Assert.That(
                card.Tasks.Select(i => i.TypeID),
                Is.Not.Contains(typeID),
                () => $"{nameof(Card)}.{nameof(Card.Tasks)} contains task with type ID = {typeID:B}.");
        }

        /// <summary>
        /// Проверяет, что в карточке, которой управляет указанный объект, не содержится заданий указанного типа.
        /// </summary>
        /// <param name="clc"><inheritdoc cref="ICardLifecycleCompanion" path="/summary"/></param>
        /// <param name="typeID">Тип задания, которого не должно быть в карточке.</param>
        public static void HasNoTask(
            ICardLifecycleCompanion clc,
            Guid typeID)
        {
            ThrowIfNull(clc);

            HasNoTask(clc.GetCardOrThrow(), typeID);
        }

        /// <summary>
        /// Проверяет, что в карточке не содержится заданий.
        /// </summary>
        /// <param name="card">Проверяемая карточка.</param>
        public static void HasNoTask(
            Card card)
        {
            ThrowIfNull(card);

            var tasks = card.TryGetTasks();

            Assert.That(
                tasks,
                Is.Null.Or.Empty,
                () => GetMessageActualTasks(tasks));
        }

        /// <summary>
        /// Проверяет, что в карточке, которой управляет указанный объект, не содержится заданий.
        /// </summary>
        /// <param name="clc"><inheritdoc cref="ICardLifecycleCompanion" path="/summary"/></param>
        public static void HasNoTask(
            ICardLifecycleCompanion clc)
        {
            ThrowIfNull(clc);

            HasNoTask(clc.GetCardOrThrow());
        }

        /// <summary>
        /// Проверяет, что маршрут в карточке содержит этап с указанным названием и порядковым номером.
        /// </summary>
        /// <param name="card">Проверяемая карточка.</param>
        /// <param name="name">Имя этапа.</param>
        /// <param name="order">Порядковый номер этапа.</param>
        public static void StageHasOrder(
            Card card,
            string name,
            int order)
        {
            ThrowIfNull(card);

            var rb = new RouteBuilder(card);
            var stage = rb.TryGetStage(name);

            if (stage is null)
            {
                var sb = StringBuilderHelper
                    .Acquire()
                    .AppendLine($"Route doesn't contain stage \"{ name}\".");

                KrTestHelper.AddStageRowsInfo(
                    sb,
                    rb.GetStages(),
                    true);

                Assert.Fail(sb.ToStringAndRelease());
            }

            var actual = stage!.Get<int>(KrConstants.KrStages.Order);

            Assert.That(
                actual,
                Is.EqualTo(order),
                () =>
                {
                    var sb = StringBuilderHelper
                        .Acquire()
                        .AppendLine($"Stage \"{name}\" has {actual} order actual but expected {order}.");

                    KrTestHelper.AddStageRowsInfo(
                        sb,
                        rb.GetStages(),
                        true);

                    return sb.ToStringAndRelease();
                });
        }

        /// <summary>
        /// Проверяет, что маршрут в карточке, которой управляет указанный объект, содержит этап с указанным названием и порядковым номером.
        /// </summary>
        /// <param name="clc"><inheritdoc cref="ICardLifecycleCompanion" path="/summary"/></param>
        /// <param name="name">Имя этапа.</param>
        /// <param name="order">Порядковый номер этапа.</param>
        public static void StageHasOrder(
            ICardLifecycleCompanion clc,
            string name,
            int order)
        {
            ThrowIfNull(clc);

            StageHasOrder(clc.GetCardOrThrow(), name, order);
        }

        /// <summary>
        /// Проверяет, что в карточке маршрут содержит приведенные этапы в указанном порядке.
        /// </summary>
        /// <param name="card">Проверяемая карточка.</param>
        /// <param name="stageNames">Имена этапов, перечисленные в порядке в котором они должны быть в маршруте.</param>
        public static void SequenceOfStagesIs(
            Card card,
            IEnumerable<string> stageNames)
        {
            ThrowIfNull(card);
            ThrowIfNull(stageNames);
            var rb = new RouteBuilder(card);

            var i = 0;
            foreach (var stageName in stageNames)
            {
                var stage = rb.TryGetStage(stageName);

                if (stage is null)
                {
                    var sb = StringBuilderHelper
                        .Acquire()
                        .AppendLine($"Route doesn't contain stage \"{stageName}\".");

                    KrTestHelper.AddStageRowsInfo(
                        sb,
                        rb.GetStages(),
                        true);

                    Assert.Fail(sb.ToStringAndRelease());
                }

                var actual = stage!.Get<int>(KrConstants.KrStages.Order);

                Assert.That(
                    actual,
                    Is.EqualTo(i),
                    () =>
                    {
                        var sb = new StringBuilder()
                            .AppendLine($"Stage \"{stageName}\" has {actual} order actual but expected {i}.");

                        var actualStages = rb
                            .GetStages()
                            .OrderBy(static i => i.TryGet<int>(KrConstants.KrStages.Order))
                            .Select(static i => FormatNullable(i.Get<string>(KrConstants.KrStages.NameField)));

                        List<string?[]> rows = [];

                        using (var stageNamesEnumerator = stageNames.GetEnumerator())
                        {
                            using var actualStagesEnumerator = actualStages.GetEnumerator();

                            var hasStageNamesValue = true;
                            var hasActualStagesValue = true;

                            while (hasStageNamesValue && (hasStageNamesValue = stageNamesEnumerator.MoveNext())
                                | hasActualStagesValue && (hasActualStagesValue = actualStagesEnumerator.MoveNext()))
                            {
                                rows.Add([
                                    rows.Count.ToString(), // Нумерация с нуля, из-за сравнения по KrStages.Order.
                                    hasStageNamesValue ? stageNamesEnumerator.Current : null,
                                    hasActualStagesValue ? actualStagesEnumerator.Current : null]);
                            }
                        }

                        TestTextHelper.PrintTable(
                            sb,
                            rows,
                            [
                                "Serial number",
                                "Expected orders",
                                "Actual orders"
                            ]);

                        sb
                            .AppendLine()
                            .AppendLine()
                            .AppendLine("Detailed stage rows:");

                        sb.AppendElements(
                            rb.GetStages(),
                            Environment.NewLine,
                            static (sb, value) =>
                                StorageHelper.Print(sb, value));

                        return sb.ToString();
                    });
                i++;
            }

            VisibleStagesCount(card, i);
        }

        /// <summary>
        /// Проверяет, что в карточке маршрут содержит приведенные этапы в указанном порядке.
        /// </summary>
        /// <param name="card">Проверяемая карточка.</param>
        /// <param name="stageNames">Имена этапов, перечисленные в порядке в котором они должны быть в маршруте.</param>
        public static void SequenceOfStagesIs(
            Card card,
            params string[] stageNames) =>
            // Параметры будут проверены в SequenceOfStagesIs.
            SequenceOfStagesIs(card, (IEnumerable<string>) stageNames);

        /// <summary>
        /// Проверяет, что в карточке, которой управляет указанный объект, маршрут содержит приведенные этапы в указанном порядке.
        /// </summary>
        /// <param name="clc"><inheritdoc cref="ICardLifecycleCompanion" path="/summary"/></param>
        /// <param name="stageNames">Имена этапов, перечисленные в порядке в котором они должны быть в маршруте.</param>
        public static void SequenceOfStagesIs(
            ICardLifecycleCompanion clc,
            params string[] stageNames)
        {
            ThrowIfNull(clc);
            // Параметр stageNames будет проверен в SequenceOfStagesIs.

            SequenceOfStagesIs(clc.GetCardOrThrow(), stageNames);
        }

        /// <summary>
        /// Проверяет, что в карточке, которой управляет указанный объект, маршрут содержит приведенные этапы в указанном порядке.
        /// </summary>
        /// <param name="clc"><inheritdoc cref="ICardLifecycleCompanion" path="/summary"/></param>
        /// <param name="stageNames">Имена этапов, перечисленные в порядке в котором они должны быть в маршруте.</param>
        public static void SequenceOfStagesIs(
            ICardLifecycleCompanion clc,
            IEnumerable<string> stageNames)
        {
            ThrowIfNull(clc);
            // Параметр stageNames будет проверен в SequenceOfStagesIs.

            SequenceOfStagesIs(clc.GetCardOrThrow(), stageNames);
        }

        /// <summary>
        /// Проверяет, что маршрут в карточке содержит указанное число видимых этапов.
        /// </summary>
        /// <param name="card">Проверяемая карточка.</param>
        /// <param name="expectedCnt">Ожидаемое число этапов.</param>
        public static void VisibleStagesCount(
            Card card,
            int expectedCnt)
        {
            ThrowIfNull(card);

            var rows = card
                .GetStagesSection()
                .Rows;

            var cnt = rows.Count;

            Assert.That(
                cnt,
                Is.EqualTo(expectedCnt),
                () =>
                {
                    var sb = StringBuilderHelper
                        .Acquire()
                        .AppendLine("Visible stages:");

                    KrTestHelper.AddStageRowsInfo(
                        sb,
                        rows,
                        true);

                    return sb.ToStringAndRelease();
                });
        }

        /// <summary>
        /// Проверяет, что маршрут в карточке, которой управляет указанный объект, содержит указанное число видимых этапов.
        /// </summary>
        /// <param name="clc"><inheritdoc cref="ICardLifecycleCompanion" path="/summary"/></param>
        /// <param name="expectedCnt">Ожидаемое число этапов.</param>
        public static void VisibleStagesCount(
            ICardLifecycleCompanion clc,
            int expectedCnt)
        {
            ThrowIfNull(clc);

            VisibleStagesCount(clc.GetCardOrThrow(), expectedCnt);
        }

        /// <summary>
        /// Проверяет, что маршрут в карточке содержит указанное число этапов.
        /// </summary>
        /// <param name="card">Проверяемая карточка.</param>
        /// <param name="expectedCnt">Ожидаемое число этапов.</param>
        public static void StagesCount(
            Card card,
            int expectedCnt)
        {
            ThrowIfNull(card);

            Assert.That(
                card.GetStagePositions(),
                Has.Count.EqualTo(expectedCnt));
        }

        /// <summary>
        /// Проверяет, что маршрут в карточке, которой управляет указанный объект, содержит указанное число этапов.
        /// </summary>
        /// <param name="clc"><inheritdoc cref="ICardLifecycleCompanion" path="/summary"/></param>
        /// <param name="expectedCnt">Ожидаемое число этапов.</param>
        public static void StagesCount(
            ICardLifecycleCompanion clc,
            int expectedCnt)
        {
            ThrowIfNull(clc);

            StagesCount(clc.GetCardOrThrow(), expectedCnt);
        }

        /// <summary>
        /// Проверяет, что по карточке, с указанным идентификатором, есть хотя бы один запущенный типовой процесс согласования указанного типа.
        /// </summary>
        /// <param name="dbScope"><inheritdoc cref="IDbScope" path="/summary"/></param>
        /// <param name="mainCardID">Идентификатор проверяемой карточки.</param>
        /// <param name="processType">Тип проверяемого процесса. Если не указано значение по умолчанию для типа, то проверяется наличие любого процесса по карточке с указанным идентификатором.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>\
        /// <returns>Асинхронная задача.</returns>
        public static async Task HasWorkflowProcessAsync(
            IDbScope dbScope,
            Guid mainCardID,
            string? processType = null,
            CancellationToken cancellationToken = default)
        {
            // Параметр dbScope будет проверен в GetWorkflowProcessAsync.

            var workflowProcesses = await KrTestHelper.GetWorkflowProcessAsync(
                dbScope,
                mainCardID,
                cancellationToken);
            var expression = processType is null
                ? (IResolveConstraint) Is.Not.Empty
                : Does.Contain(processType);

            Assert.That(
                workflowProcesses,
                expression,
                $"Card with ID = {mainCardID:B} has no workflow API process.");
        }

        /// <summary>
        /// Проверяет, что по карточке, с указанным идентификатором, есть запущенный типовой процесс c указанным идентификатором.
        /// </summary>
        /// <param name="dbScope"><inheritdoc cref="IDbScope" path="/summary"/></param>
        /// <param name="mainCardID">Идентификатор проверяемой карточки.</param>
        /// <param name="processID">Идентификатор проверяемого процесса.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>\
        /// <returns>Асинхронная задача.</returns>
        public static async Task HasWorkflowProcessAsync(
            IDbScope dbScope,
            Guid mainCardID,
            Guid processID,
            CancellationToken cancellationToken = default)
        {
            // Параметр dbScope будет проверен в GetWorkflowProcessAsync.

            var workflowProcesses = await KrTestHelper.GetWorkflowProcessAsync(
                dbScope,
                mainCardID,
                cancellationToken);

            Assert.That(
                workflowProcesses.FirstOrDefault(i => i.ID == processID),
                Is.Not.Null,
                () => $"Card with ID = {mainCardID:B} has no workflow API process."
                    + Environment.NewLine
                    + string.Join(Environment.NewLine, workflowProcesses));
        }

        /// <summary>
        /// Проверяет, что по карточке, с указанным идентификатором, нет запущенных процессов.
        /// </summary>
        /// <param name="dbScope"><inheritdoc cref="IDbScope" path="/summary"/></param>
        /// <param name="mainCardID">Идентификатор проверяемой карточки.</param>
        /// <param name="processType">Тип проверяемого процесса или значение <see langword="null"/>, если по карточке не должно быть запущено никаких процессов.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Асинхронная задача.</returns>
        public static async Task HasNoWorkflowProcessAsync(
            IDbScope dbScope,
            Guid mainCardID,
            string? processType = null,
            CancellationToken cancellationToken = default)
        {
            // Параметр dbScope будет проверен в GetWorkflowProcessAsync.

            var workflowProcesses = await KrTestHelper.GetWorkflowProcessAsync(
                dbScope,
                mainCardID,
                cancellationToken);
            var expression = processType is null
                ? (IResolveConstraint) Is.Empty
                : Is.Not.Contains(processType);

            Assert.That(
                workflowProcesses,
                expression,
                $"Card with ID = {mainCardID:B} has workflow API process.");
        }

        /// <summary>
        /// Проверяет, что по карточке, с указанным идентификатором, нет запущенных процессов.
        /// </summary>
        /// <param name="dbScope"><inheritdoc cref="IDbScope" path="/summary"/></param>
        /// <param name="mainCardID">Идентификатор проверяемой карточки.</param>
        /// <param name="processID">Идентификатор проверяемого процесса.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Асинхронная задача.</returns>
        public static async Task HasNoWorkflowProcessAsync(
            IDbScope dbScope,
            Guid mainCardID,
            Guid processID,
            CancellationToken cancellationToken = default)
        {
            // Параметр dbScope будет проверен в GetWorkflowProcessAsync.

            var workflowProcesses = await KrTestHelper.GetWorkflowProcessAsync(
                dbScope,
                mainCardID,
                cancellationToken);

            Assert.That(
                workflowProcesses.FirstOrDefault(i => i.ID == processID),
                Is.Null,
                () => $"Card with ID = {mainCardID:B} has workflow API process."
                    + Environment.NewLine
                    + string.Join(Environment.NewLine, workflowProcesses));
        }

        /// <summary>
        /// Проверяет число строк в коллекционной или древовидной секции.<para/>
        /// Отсутствие секции эквивалентно отсутствию строк.<para/>
        /// Создаёт исключение <see cref="AssertionException"/>, если проверка не пройдена.
        /// </summary>
        /// <param name="sections">Секции.</param>
        /// <param name="sectionName">Название секции.</param>
        /// <param name="expectedRowCount">Ожидаемое число строк.</param>
        public static void RowsCount(
            IReadOnlyDictionary<string, CardSection> sections,
            string sectionName,
            int expectedRowCount)
        {
            ThrowIfNull(sections);

            if (sections.TryGetValue(sectionName, out var section)
                && section.Type == CardSectionType.Entry)
            {
                throw new ArgumentException(
                    $"Unexpected card section type: {section.Type}. Section name: \"{sectionName}\". Expected section type: {CardSectionType.Table}.",
                    nameof(sectionName));
            }

            Assert.That(
                section?.TryGetRows(),
                expectedRowCount == 0
                    ? Is.Null.Or.Count.EqualTo(expectedRowCount)
                    : Is.Not.Null.And.Count.EqualTo(expectedRowCount));
        }

        /// <summary>
        /// Проверяет, что все присутствующие в заданной строковой секции карточки поля, имеют значения по умолчанию.
        /// </summary>
        /// <param name="card">Карточка, содержащая проверяемую секцию.</param>
        /// <param name="sectionName">Название проверяемой секции.</param>
        /// <param name="cardMetadataSections"><inheritdoc cref="CardMetadataSectionCollection" path="/summary"/></param>
        /// <param name="checkedFields">Коллекция имён проверяемых полей. Если не задана, то проверяются все поля.</param>
        /// <exception cref="InvalidOperationException">Секция является коллекционной или древовидной.</exception>
        public static void HasDefaultValue(
            Card card,
            string sectionName,
            CardMetadataSectionCollection cardMetadataSections,
            IReadOnlyCollection<string>? checkedFields = null)
        {
            ThrowIfNull(card);
            ThrowIfNull(cardMetadataSections);

            var fields = card.Sections[sectionName].Fields;
            var columnsMetadata = cardMetadataSections[sectionName].Columns;

            foreach (var field in fields)
            {
                if (checkedFields?.Contains(field.Key) != true)
                {
                    continue;
                }

                if (columnsMetadata.TryGetValue(field.Key, out var columnMetadata))
                {
                    Assert.That(
                        field.Value,
                        Is.EqualTo(columnMetadata.DefaultValue),
                        "The value in the \"{0}.{1}\" field is not the default value.",
                        sectionName,
                        field.Key);
                }
            }
        }

        /// <summary>
        /// Проверяет корректность указанного в задании значения <see cref="CardTask.Planned"/>.
        /// </summary>
        /// <param name="businessCalendarService">Бизнес календарь.</param>
        /// <param name="task">Проверяемое задание.</param>
        /// <param name="timeLimitation">Смещение в рабочих днях от даты создания задания.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Асинхронная задача.</returns>
        public static async Task CheckTaskPlannedAsync(
            IBusinessCalendarService businessCalendarService,
            CardTask task,
            double timeLimitation,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(businessCalendarService);
            ThrowIfNull(task);

            ThrowIf(
                task,
                !task.Planned.HasValue,
                static task =>
                    GetMessage(task, nameof(task.Planned)));

            ThrowIf(
                task,
                !task.Card.Created.HasValue,
                static task =>
                    GetMessage(task, nameof(task.Card.Created)));

            ThrowIf(
                task,
                !task.CalendarID.HasValue,
                static task =>
                    GetMessage(task, nameof(task.CalendarID)));

            var additionalApprovalTaskPlannedExpected = await businessCalendarService.AddWorkingDaysToDateAsync(
                    task.Card.Created.Value,
                    timeLimitation,
                    task.CalendarID.Value,
                    task.TimeZoneUtcOffsetMinutes.HasValue
                    ? TimeSpan.FromMinutes(task.TimeZoneUtcOffsetMinutes.Value)
                    : null,
                    cancellationToken);

            Assert.That(
                task.Planned,
                Is.EqualTo(additionalApprovalTaskPlannedExpected));

            static string GetMessage(
                CardTask task,
                string parameterName)
            {
                var sb = StringBuilderHelper
                    .Acquire()
                    .AppendLine($"{parameterName} is required.")
                    .AppendLine("Task:");

                TestHelper.AddTasksInfo(
                    sb,
                    [task]);

                return sb.ToStringAndRelease();
            }
        }

        /// <summary>
        /// Проверяет содержит ли объект, управляющий жизненным циклом карточки, карточку указанного типа.
        /// </summary>
        /// <param name="clc"><inheritdoc cref="ICardLifecycleCompanion" path="/summary"/></param>
        /// <param name="expectedTypeID">Идентификатор ожидаемого типа.</param>
        /// <param name="expectedTypeName">Имя ожидаемого типа.</param>
        /// <exception cref="ArgumentException">The <see cref="ICardLifecycleCompanion"/> object must contain a card of type <paramref name="expectedTypeName"/> (ID = "<paramref name="expectedTypeID"/>"). Actual type <see cref="Card.TypeCaption"/> (ID = "<see cref="Card.TypeID"/>").</exception>
        public static void CheckCardType(
            ICardLifecycleCompanion clc,
            Guid expectedTypeID,
            string expectedTypeName)
        {
            ThrowIfNull(clc);

            var card = clc.GetCardOrThrow();

            CheckCardType(card, expectedTypeID, expectedTypeName);
        }

        /// <summary>
        /// Проверяет, является ли карточка указанного типа.
        /// </summary>
        /// <param name="card"><inheritdoc cref="Card" path="/summary"/></param>
        /// <param name="expectedTypeID">Идентификатор ожидаемого типа.</param>
        /// <param name="expectedTypeName">Имя ожидаемого типа.</param>
        /// <exception cref="ArgumentException">
        /// Expected a card of type <paramref name="expectedTypeName"/> (ID = "<paramref name="expectedTypeID"/>").
        /// Actual type <see cref="Card.TypeCaption"/> (ID = "<see cref="Card.TypeID"/>").
        /// </exception>
        public static void CheckCardType(
            Card card,
            Guid expectedTypeID,
            string expectedTypeName)
        {
            ThrowIfNull(card);

            if (card.TypeID != expectedTypeID)
            {
                throw new ArgumentException($"The {nameof(ICardLifecycleCompanion)} object must contain a card of type {expectedTypeName} (ID = \"{expectedTypeID}\"). Actual type {card.TypeName} (ID = \"{card.TypeID}\").", nameof(card));
            }
        }

        /// <summary>
        /// Проверяет, что запись в истории заданий содержит указанное описание результата завершения.
        /// </summary>
        /// <param name="clc"><inheritdoc cref="ICardLifecycleCompanion" path="/summary"/></param>
        /// <param name="taskRowID">Идентификатор задания.</param>
        /// <param name="expectedResult">Ожидаемое описание результата завершения.</param>
        public static void CheckTaskHistoryResult(
            ICardLifecycleCompanion clc,
            Guid taskRowID,
            string? expectedResult)
        {
            // Значение clc будет проверено в GetCardTaskHistoryItemOrThrow.

            clc
                .GetCardTaskHistoryItemOrThrow(
                    taskRowID,
                    out var cardTaskHistoryItem);

            Assert.That(
                cardTaskHistoryItem.Result,
                Is.EqualTo(expectedResult));
        }

        /// <summary>
        /// Проверяет корректность строк этапов.
        /// </summary>
        /// <param name="clc"><inheritdoc cref="ICardLifecycleCompanion" path="/summary"/></param>
        /// <param name="validateStageRowFuncAsync">
        /// Функция, выполняющая валидацию строки этапа.<para/>
        ///
        /// Параметры:<br/>
        /// <list type="bullet">
        /// <item>
        ///     <description>
        ///         Шаблон этапов из которого был создан проверяемый этап или значение <see langword="null"/>, если в проверяемой строке этапа не содержится <see cref="KrConstants.KrStages.BasedOnStageTemplateID"/>.
        ///     </description>
        /// </item>
        /// <item>
        ///     <description>
        ///         Строка проверяемого этапа из шаблона этапов или значение <see langword="null"/>, если в проверяемой строке этапа не содержится <see cref="KrConstants.KrStages.BasedOnStageTemplateID"/>.<para/>
        ///
        ///         Определение строки проверяемого этапа из шаблона этапов, осуществляется по имени этапа, а не по <see cref="KrConstants.KrStages.BasedOnStageRowID"/>.
        ///     </description>
        /// </item>
        /// <item>
        ///     <description>
        ///         Проверяемая строка этапа.
        ///     </description>
        /// </item>
        /// <item>
        ///     <description>
        ///         Объект, посредством которого можно отменить асинхронную задачу.
        ///     </description>
        /// </item>
        /// </list>
        /// </param>
        /// <param name="stageNames">Список имён проверяемых этапов или значение <see langword="null"/>, если необходимо проверить все этапы находящиеся в карточке.</param>
        /// <param name="templates">Список шаблонов этапов. Если не задан, то будет создан автоматически. Используется для уменьшения числа загрузок карточек шаблонов этапов.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Список загруженных шаблонов этапов.</returns>
        public static async ValueTask<ICollection<KrStageTemplateBuilder>> ValidateStageRowsAsync(
            ICardLifecycleCompanion clc,
            Func<KrStageTemplateBuilder?, CardRow?, CardRow, CancellationToken, ValueTask> validateStageRowFuncAsync,
            IEnumerable<string>? stageNames = null,
            ICollection<KrStageTemplateBuilder>? templates = null,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(clc);
            ThrowIfNull(validateStageRowFuncAsync);

            templates ??= [];

            var oldID = Guid.Empty;

            foreach (var templateID in templates
                .OrderBy(static i => i.CardID))
            {
                oldID = oldID == templateID.CardID
                    ? throw new InvalidOperationException($"A duplicate stage template has been detected. ID = \"{oldID:B}\".")
                    : templateID.CardID;
            }

            var rb = clc.GetRouteBuilder();

            foreach (var stageRow in rb.GetStages())
            {
                var stageName = stageRow.Get<string>(KrConstants.KrStages.NameField)
                    ?? throw new InvalidOperationException($"{KrConstants.KrStages.NameField} is null. Stage row:{Environment.NewLine}{StorageHelper.Print(stageRow)}");

                if (stageNames?.Contains(stageName) == false)
                {
                    continue;
                }

                var basedOnStageTemplateID = stageRow.TryGet<Guid?>(KrConstants.KrStages.BasedOnStageTemplateID);

                KrStageTemplateBuilder? template = null;
                CardRow? templateStage = null;

                if (basedOnStageTemplateID.HasValue)
                {
                    template = templates.FirstOrDefault(i => i.CardID == basedOnStageTemplateID.Value);
                    if (template is null)
                    {
                        template = await new KrStageTemplateBuilder(
                                basedOnStageTemplateID.Value,
                                clc.Dependencies)
                            .Load()
                            .GoAsync(
                                ValidationAssert.IsSuccessful,
                                cancellationToken: cancellationToken);
                        templates.Add(template);
                    }

                    var templateRb = template.GetRouteBuilder();
                    templateStage = templateRb.GetStage(stageName);
                }

                Assert.Multiple(async () =>
                {
                    await validateStageRowFuncAsync(
                        template,
                        templateStage,
                        stageRow,
                        cancellationToken);
                });
            }

            return templates;
        }

        /// <summary>
        /// Проверяет наличие разрешений у строки.
        /// </summary>
        /// <param name="card">Карточка, содержащая проверяемую строку.</param>
        /// <param name="sectionName">Название секции, содержащей проверяемую строку.</param>
        /// <param name="rowID">Идентификатор проверяемой строки.</param>
        /// <param name="expectedPermissions">Ожидаемые права на строку с <paramref name="rowID"/>.</param>
        /// <param name="getMessageFunc">Функция, возвращающая дополнительное сообщение, выводимое при ошибке проверки. Сообщение будет выведено с новой строки. Сообщение не выводится, если оно равно <see langword="null"/>, <see cref="string.Empty"/> или состоит только из пробелов.</param>
        public static void CheckRowPermissions(
            Card card,
            string sectionName,
            Guid rowID,
            CardPermissionFlags expectedPermissions,
            Func<string?>? getMessageFunc = null)
        {
            ThrowIfNull(card);

            var actualPermissions = card
                .Permissions
                .CreateResolver()
                .GetRowPermissions(
                    sectionName,
                    rowID);

            Assert.That(
                actualPermissions,
                Is.EqualTo(expectedPermissions),
                () =>
                {
                    var sb = StringBuilderHelper.Acquire();

                    AppendCardSectionRowInfo(
                        sb,
                        card,
                        sectionName,
                        rowID);

                    AppendIfNotWhiteSpace(
                        sb,
                        getMessageFunc,
                        true);

                    return sb.ToStringAndRelease();
                });
        }

        /// <summary>
        /// Проверяет наличие разрешений у поля строки.
        /// </summary>
        /// <param name="card">Карточка, содержащая проверяемую строку.</param>
        /// <param name="sectionName">Имя коллекционной секции.</param>
        /// <param name="rowID">Идентификатор проверяемой строки.</param>
        /// <param name="fieldName">Название поля строки, для которого проверяются разрешения.</param>
        /// <param name="expectedPermissions">Ожидаемые права на поле <paramref name="fieldName"/>.</param>
        /// <param name="getMessageFunc">Функция, возвращающая дополнительное сообщение, выводимое при ошибке проверки. Сообщение будет выведено с новой строки. Сообщение не выводится, если оно равно <see langword="null"/>, <see cref="string.Empty"/> или состоит только из пробелов.</param>
        public static void CheckRowFieldPermissions(
            Card card,
            string sectionName,
            Guid rowID,
            string fieldName,
            CardPermissionFlags expectedPermissions,
            Func<string?>? getMessageFunc = null)
        {
            ThrowIfNull(card);

            var actualPermissions = card
                .Permissions
                .CreateResolver()
                .GetFieldPermissions(
                    sectionName,
                    fieldName,
                    rowID);

            Assert.That(
                actualPermissions,
                Is.EqualTo(expectedPermissions),
                () =>
                {
                    var sb = StringBuilderHelper.Acquire();

                    AppendCardSectionRowInfo(
                        sb,
                        card,
                        sectionName,
                        rowID);

                    sb
                        .AppendLine()
                        .Append($"Field name: \"{fieldName}\".");

                    AppendIfNotWhiteSpace(
                        sb,
                        getMessageFunc,
                        true);

                    return sb.ToStringAndRelease();
                });
        }

        /// <summary>
        /// Проверяет наличие разрешений у строки этапа.
        /// </summary>
        /// <param name="clc"><inheritdoc cref="ICardLifecycleCompanion" path="/summary"/></param>
        /// <param name="stageName">Название этапа.</param>
        /// <param name="expectedPermissions">Ожидаемые права на строку этапа <paramref name="stageName"/>.</param>
        /// <param name="getMessageFunc">Функция, возвращающая дополнительное сообщение, выводимое при ошибке проверки. Сообщение будет выведено с новой строки. Сообщение не выводится, если оно равно <see langword="null"/>, <see cref="string.Empty"/> или состоит только из пробелов.</param>
        public static void CheckStageRowPermissions(
            ICardLifecycleCompanion clc,
            string stageName,
            CardPermissionFlags expectedPermissions,
            Func<string?>? getMessageFunc = null)
        {
            ThrowIfNull(clc);

            var rb = clc.GetRouteBuilder();
            CheckRowPermissions(
                clc.GetCardOrThrow(),
                rb.GetStageSectionName(),
                rb.GetStage(stageName).RowID,
                expectedPermissions,
                () =>
                {
                    var sb = StringBuilderHelper.Acquire();
                    sb.Append($"Stage name: \"{stageName}\".");

                    AppendIfNotWhiteSpace(
                        sb,
                        getMessageFunc,
                        true);

                    return sb.ToStringAndRelease();
                });
        }

        /// <summary>
        /// Проверяет наличие разрешений у поля строки этапа.
        /// </summary>
        /// <param name="clc"><inheritdoc cref="ICardLifecycleCompanion" path="/summary"/></param>
        /// <param name="stageName">Название этапа.</param>
        /// <param name="fieldName">Название поля строки этапа, для которого проверяются разрешения.</param>
        /// <param name="expectedPermissions">Ожидаемые права на поле <paramref name="fieldName"/>.</param>
        /// <param name="getMessageFunc">Функция, возвращающая дополнительное сообщение, выводимое при ошибке проверки. Сообщение будет выведено с новой строки. Сообщение не выводится, если оно равно <see langword="null"/>, <see cref="string.Empty"/> или состоит только из пробелов.</param>
        public static void CheckStageRowFieldPermissions(
            ICardLifecycleCompanion clc,
            string stageName,
            string fieldName,
            CardPermissionFlags expectedPermissions,
            Func<string?>? getMessageFunc = null)
        {
            ThrowIfNull(clc);

            var rb = clc.GetRouteBuilder();
            CheckRowFieldPermissions(
                clc.GetCardOrThrow(),
                rb.GetStageSectionName(),
                rb.GetStage(stageName).RowID,
                fieldName,
                expectedPermissions,
                () =>
                {
                    var sb = StringBuilderHelper.Acquire();
                    sb.Append($"Stage name: \"{stageName}\".");

                    AppendIfNotWhiteSpace(
                        sb,
                        getMessageFunc,
                        true);

                    return sb.ToStringAndRelease();
                });
        }

        /// <summary>
        /// Проверяет значение поля <see cref="KrConstants.KrProcessCommonInfo.CurrentApprovalStageRowID"/>.
        /// </summary>
        /// <param name="card">Карточка, содержащая проверяемое значение.</param>
        /// <param name="expectedCurrentApprovalStageRowID">Ожидаемое значение.</param>
        public static void CheckCurrentApprovalStageRowID(
            Card card,
            Guid? expectedCurrentApprovalStageRowID = null)
        {
            ThrowIfNull(card);

            var approvalInfoSection = card.GetApprovalInfoSection();
            var actual = approvalInfoSection
                .RawFields
                .TryGet<Guid?>(KrConstants.KrProcessCommonInfo.CurrentApprovalStageRowID);

            Assert.That(
                actual,
                Is.EqualTo(expectedCurrentApprovalStageRowID),
                () =>
                {
                    var sb = StringBuilderHelper
                        .Acquire()
                        .AppendLine($"Expected {approvalInfoSection.Name}.{KrConstants.KrProcessCommonInfo.CurrentApprovalStageRowID}={FormatNullable(expectedCurrentApprovalStageRowID, "B")} but got {actual:B}.")
                        .AppendLine("Actual stages:");

                    KrTestHelper.AddStageRowsInfo(
                        sb,
                        card.GetStagesSection().Rows,
                        true);

                    return sb.ToStringAndRelease();
                });
        }

        /// <summary>
        /// Проверяет значение поля <see cref="KrConstants.KrProcessCommonInfo.CurrentApprovalStageRowID"/>.
        /// </summary>
        /// <param name="clc"><inheritdoc cref="ICardLifecycleCompanion" path="/summary"/></param>
        /// <param name="expectedCurrentApprovalStageRowID">Ожидаемое значение.</param>
        public static void CheckCurrentApprovalStageRowID(
            ICardLifecycleCompanion clc,
            Guid? expectedCurrentApprovalStageRowID = null)
        {
            ThrowIfNull(clc);

            CheckCurrentApprovalStageRowID(
                clc.GetCardOrThrow(),
                expectedCurrentApprovalStageRowID);
        }

        #endregion

        #region Private Methods

        private static void AppendCardCommonInfo(
            StringBuilder sb,
            Card card) =>
            sb
                .AppendLine($"Card ID: \"{card.ID:B}\".")
                .AppendLine($"Card type name: \"{card.TypeName}\".")
                .Append($"Card type ID: \"{card.TypeID}\".")
                ;

        private static void AppendCardSectionRowInfo(
            StringBuilder sb,
            Card card,
            string sectionName,
            Guid rowID)
        {
            AppendCardCommonInfo(
                sb,
                card);

            sb
                .AppendLine()
                .AppendLine($"Section name: \"{sectionName}\".")
                .Append($"Row ID: \"{rowID:B}\".")
                ;
        }

        private static void AppendIfNotWhiteSpace(
            StringBuilder sb,
            Func<string?>? getMessageFunc,
            bool isAddNewLineBeforeMessage = false)
        {
            if (getMessageFunc is null)
            {
                return;
            }

            var message = getMessageFunc.Invoke();

            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            if (isAddNewLineBeforeMessage)
            {
                sb.AppendLine();
            }

            sb.Append(message);
        }

        private static string GetMessageActualTasks(
            IEnumerable<CardTask>? tasks,
            string? message = null)
        {
            var sb = StringBuilderHelper.Acquire();

            if (!string.IsNullOrEmpty(message))
            {
                sb
                    .Append(' ')
                    .Append(message);
            }

            sb
                .AppendLine()
                .AppendLine("Actual tasks:");

            TestHelper.AddTasksInfo(
                sb,
                tasks ?? []);

            return sb.ToStringAndRelease();
        }

        #endregion
    }
}
