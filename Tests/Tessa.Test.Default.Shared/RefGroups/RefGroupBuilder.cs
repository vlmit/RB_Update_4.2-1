#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Platform;
using Tessa.Platform.Collections;
using Tessa.Platform.RefGroups;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.Test.Default.Shared.Kr;

namespace Tessa.Test.Default.Shared.RefGroups
{
    /// <summary>
    /// Предоставляет методы для создания и модификации карточки группы ссылок.
    /// </summary>
    public sealed class RefGroupBuilder :
        CardLifecycleCompanion<RefGroupBuilder>,
        INamedEntry
    {
        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="RefGroupBuilder"/>.
        /// </summary>
        /// <param name="deps"><inheritdoc cref="ICardLifecycleCompanionDependencies" path="/summary"/>.</param>
        public RefGroupBuilder(
            ICardLifecycleCompanionDependencies deps)
            : this(Guid.NewGuid(), deps)
        {
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="RefGroupBuilder"/>.
        /// </summary>
        /// <param name="cardID">Идентификатор карточки группы ссылок.</param>
        /// <param name="deps"><inheritdoc cref="ICardLifecycleCompanionDependencies" path="/summary"/>.</param>
        public RefGroupBuilder(
            Guid cardID,
            ICardLifecycleCompanionDependencies deps)
            : base(
                  cardID,
                  CardHelper.RefGroupTypeID,
                  CardHelper.RefGroupTypeName,
                  deps)
        {
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Устанавливает название группы ссылок.
        /// </summary>
        /// <param name="value">Название группы ссылок.</param>
        /// <returns>Объект <see cref="RefGroupBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public RefGroupBuilder SetName(string value) =>
            this.SetField(RefGroupsHelper.RefGroupsName, value);

        /// <summary>
        /// Возвращает название группы ссылок.
        /// </summary>
        /// <returns>Название группы ссылок.</returns>
        public string? GetName() =>
            this.TryGetField<string>(RefGroupsHelper.RefGroupsName);

        /// <summary>
        /// Устанавливает тип группы ссылок.
        /// </summary>
        /// <param name="typeID">Идентификатор типа группы ссылок.</param>
        /// <param name="typeName">Название типа группы ссылок.</param>
        /// <param name="keyType"><inheritdoc cref="RefGroupKeyType" path="/summary"/></param>
        /// <returns>Объект <see cref="RefGroupBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public RefGroupBuilder SetRefGroupType(
            Guid typeID,
            string typeName,
            RefGroupKeyType keyType)
        {
            return this
                .SetField(RefGroupsHelper.RefGroupsRefGroupTypeID, typeID)
                .SetField(RefGroupsHelper.RefGroupsRefGroupTypeName, typeName)
                .SetField(RefGroupsHelper.RefGroupsRefGroupTypeKeyTypeID, Int32Boxes.Box((int) keyType));
        }

        /// <summary>
        /// Устанавливает тип группы ссылок.
        /// </summary>
        /// <param name="refGroupType"><inheritdoc cref="RefGroupTypeBuilder" path="/summary"/></param>
        /// <returns>Объект <see cref="RefGroupBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public RefGroupBuilder SetRefGroupType(
            RefGroupTypeBuilder refGroupType)
        {
            ThrowIfNull(refGroupType);

            return this.SetRefGroupType(
                refGroupType.CardID,
                refGroupType.GetName() ?? string.Empty,
                refGroupType.GetKeyType());
        }

        /// <summary>
        /// Устанавливает тип группы ссылок.
        /// </summary>
        /// <param name="refGroupTypesManager"><inheritdoc cref="IRefGroupTypesManager" path="/summary"/></param>
        /// <param name="typeID">Идентификатор типа группы ссылок.</param>
        /// <returns>Объект <see cref="RefGroupBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Метод запрашивает недостающую информацию о типе группы ссылок из <see cref="IRefGroupTypesManager"/>.<para/>
        ///
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public RefGroupBuilder SetRefGroupType(
            IRefGroupTypesManager refGroupTypesManager,
            Guid typeID)
        {
            ThrowIfNull(refGroupTypesManager);

            this.AddPendingAction(
                new PendingAction(
                    TestHelper.GetCallerMemberFullName(this),
                    async (_, ct) =>
                    {
                        var refGroupType = await refGroupTypesManager.GetGroupTypeAsync(typeID, ct);
                        if (refGroupType is null)
                        {
                            return ValidationResult.FromText($"Not reference group type found with ID={typeID:B}.", ValidationResultType.Error);
                        }

                        var card = this.GetCardOrThrow();
                        var fields = card.Sections[RefGroupsHelper.RefGroupsMainSectionName].Fields;

                        fields[RefGroupsHelper.RefGroupsRefGroupTypeID] = refGroupType.ID;
                        fields[RefGroupsHelper.RefGroupsRefGroupTypeName] = refGroupType.Name;
                        fields[RefGroupsHelper.RefGroupsRefGroupTypeKeyTypeID] = Int32Boxes.Box((int) refGroupType.KeyType);

                        return ValidationResult.Empty;
                    }));

            return this;
        }

        /// <summary>
        /// Возвращает идентификатор типа группы ссылок.
        /// </summary>
        /// <returns>Идентификатор типа группы ссылок.</returns>
        public Guid? GetRefGroupTypeID() =>
            this.TryGetField<Guid>(RefGroupsHelper.RefGroupsRefGroupTypeID);

        /// <summary>
        /// Возвращает название типа группы ссылок.
        /// </summary>
        /// <returns>Название типа группы ссылок.</returns>
        public string? GetRefGroupTypeNameD() =>
            this.TryGetField<string>(RefGroupsHelper.RefGroupsRefGroupTypeName);

        /// <summary>
        /// Устанавливает целочисленный идентификатор.
        /// </summary>
        /// <param name="value">Целочисленный идентификатор.</param>
        /// <returns>Объект <see cref="RefGroupBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// "Целочисленный идентификатор" – это число, определяющее числовой идентификатор данной группы. Используется для типов групп, которые имеют в качестве идентификатора – число (например состояния).
        /// <para/>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public RefGroupBuilder SetIntID(int value) =>
            this.SetField(RefGroupsHelper.RefGroupsIntID, value);

        /// <summary>
        /// Возвращает целочисленный идентификатор.
        /// </summary>
        /// <returns>Целочисленный идентификатор.</returns>
        public int? GetIntID() =>
            this.TryGetField<int?>(RefGroupsHelper.RefGroupsIntID);

        /// <summary>
        /// Устанавливает режим расчёта значений.
        /// </summary>
        /// <param name="value"><inheritdoc cref="RefGroupMode" path="/sumamry"/></param>
        /// <returns>Объект <see cref="RefGroupBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public RefGroupBuilder SetMode(RefGroupMode value) =>
            this
                .SetField(RefGroupsHelper.RefGroupsModeID, Int32Boxes.Box((int) value))
                .SetField(RefGroupsHelper.RefGroupsModeName, value.ToString());

        /// <summary>
        /// Возвращает режим расчёта значений.
        /// </summary>
        /// <returns><inheritdoc cref="RefGroupMode" path="/sumamry"/></returns>
        public RefGroupMode GetMode() =>
            (RefGroupMode) this.TryGetField<int>(RefGroupsHelper.RefGroupsModeID);

        /// <summary>
        /// Устанавливает SQL-скрипт для расчёта значений.
        /// </summary>
        /// <param name="value">SQL-скрипт для расчёта значений.</param>
        /// <returns>Объект <see cref="RefGroupBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public RefGroupBuilder SetSqlScript(string? value) =>
            this.SetField(RefGroupsHelper.RefGroupsSQL, value);

        /// <summary>
        /// Возвращает SQL-скрипт для расчёта значений.
        /// </summary>
        /// <returns>SQL-скрипт для расчёта значений.</returns>
        public string? GetSqlScript() =>
            this.TryGetField<string>(RefGroupsHelper.RefGroupsSQL);

        /// <summary>
        /// Устанавливает C#-скрипт для расчёта значений.
        /// </summary>
        /// <param name="value">C#-скрипт для расчёта значений.</param>
        /// <returns>Объект <see cref="RefGroupBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public RefGroupBuilder SetCSharpScript(string? value) =>
            this.SetField(RefGroupsHelper.RefGroupsScript, value);

        /// <summary>
        /// Возвращает C#-скрипт для расчёта значений.
        /// </summary>
        /// <returns>C#-скрипт для расчёта значений.</returns>
        public string? GetCSharpScript() =>
            this.TryGetField<string>(RefGroupsHelper.RefGroupsScript);

        /// <summary>
        /// Устанавливает список включаемых значений в группу ссылок.
        /// </summary>
        /// <param name="objectIDs">Список включаемых значений.</param>
        /// <returns>Объект <see cref="RefGroupBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public RefGroupBuilder SetIncludedValues(
            IEnumerable<Guid> objectIDs) =>
            this.SetIncludedOrExcludedValues(objectIDs, true);

        /// <summary>
        /// Устанавливает список включаемых значений в группу ссылок.
        /// </summary>
        /// <param name="objectIDs">Список включаемых значений.</param>
        /// <returns>Объект <see cref="RefGroupBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public RefGroupBuilder SetIncludedValues(
            IEnumerable<int> objectIDs) =>
            this.SetIncludedOrExcludedValues(objectIDs, true);

        /// <summary>
        /// Удаляет все включаемые в группу значения.
        /// </summary>
        /// <returns>Объект <see cref="RefGroupBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public RefGroupBuilder RemoveAllIncludedValues() =>
            this.RemoveAllIncludedOrExcludedValues(
                true,
                TestHelper.GetCallerMemberFullName(this));

        /// <summary>
        /// Устанавливает список исключаемых значений для группы ссылок.
        /// </summary>
        /// <param name="objectIDs">Список исключаемых значений.</param>
        /// <returns>Объект <see cref="RefGroupBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public RefGroupBuilder SetExcludedValues(
            IEnumerable<Guid> objectIDs) =>
            this.SetIncludedOrExcludedValues(objectIDs, false);

        /// <summary>
        /// Устанавливает список исключаемых значений для группы ссылок.
        /// </summary>
        /// <param name="objectIDs">Список исключаемых значений.</param>
        /// <returns>Объект <see cref="RefGroupBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public RefGroupBuilder SetExcludedValues(
            IEnumerable<int> objectIDs) =>
            this.SetIncludedOrExcludedValues(objectIDs, false);

        /// <summary>
        /// Удаляет все исключаемые из группы значения.
        /// </summary>
        /// <returns>Объект <see cref="RefGroupBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public RefGroupBuilder RemoveAllExcludedValues() =>
            this.RemoveAllIncludedOrExcludedValues(
                false,
                TestHelper.GetCallerMemberFullName(this));

        #endregion

        #region INamedEntry Members

        /// <summary>
        /// Возвращает идентификатор объекта.
        /// </summary>
        /// <exception cref="NotSupportedException">Установка значения не поддерживается.</exception>
        Guid INamedEntry.ID
        {
            get => this.CardID;
            set => throw new NotSupportedException("Setting the value is not supported.");
        }

        /// <summary>
        /// Возвращает название объекта.
        /// </summary>
        /// <exception cref="NotSupportedException">Установка значения не поддерживается.</exception>
        string? INamedEntry.Name
        {
            get => this.GetName();
            set => throw new NotSupportedException("Setting the value is not supported.");
        }

        #endregion

        #region INamedItem Members

        /// <inheritdoc/>
        string INamedItem.Name => this.GetName() ?? string.Empty;

        #endregion

        #region Private methods

        /// <summary>
        /// Задаёт значение указанного поля секции <see cref="RefGroupsHelper.RefGroupsTable"/>.
        /// </summary>
        /// <param name="field">Имя поля.</param>
        /// <param name="value">Значение.</param>
        /// <returns>Объект <see cref="RefGroupBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        private RefGroupBuilder SetField(
            string field,
            object? value) =>
            this.SetValue(RefGroupsHelper.RefGroupsTable, field, value);

        /// <summary>
        /// Возвращает значение указанного поля секции <see cref="RefGroupsHelper.RefGroupsTable"/>.
        /// </summary>
        /// <typeparam name="T">Тип возвращаемого значения.</typeparam>
        /// <param name="field">Имя поля.</param>
        /// <returns>Возвращаемое значение.</returns>
        private T? TryGetField<T>(
            string field) =>
            this.TryGetValue<T>(RefGroupsHelper.RefGroupsTable, field);

        private (RefGroupKeyType? KeyType, ValidationResult Result) GetKeyType()
        {
            var card = this.GetCardOrThrow();
            var refGroupsSection = card.Sections[RefGroupsHelper.RefGroupsMainSectionName];
            var keyType = (RefGroupKeyType?) refGroupsSection.RawFields.TryGet<int?>(RefGroupsHelper.RefGroupsRefGroupTypeKeyTypeID);

            return keyType.HasValue
                ? (keyType, ValidationResult.Empty)
                : (null, ValidationResult.FromText($"The key type is not specified in field \"{RefGroupsHelper.RefGroupsMainSectionName}.{RefGroupsHelper.RefGroupsRefGroupTypeKeyTypeID}\".",
                    ValidationResultType.Error));
        }

        private static string GetIncludedSectionNameByKeyType(
            RefGroupKeyType keyType)
        {
            if (keyType == RefGroupKeyType.Guid)
            {
                return RefGroupsHelper.IncludedValuesGuidSectionName;
            }

            if (keyType == RefGroupKeyType.Int)
            {
                return RefGroupsHelper.IncludedValuesIntSectionName;
            }

            throw ArgumentOutOfRange(keyType);
        }

        private static string GetExcludedSectionNameByKeyType(
            RefGroupKeyType keyType)
        {
            if (keyType == RefGroupKeyType.Guid)
            {
                return RefGroupsHelper.ExcludedValuesGuidSectionName;
            }

            if (keyType == RefGroupKeyType.Int)
            {
                return RefGroupsHelper.ExcludedValuesIntSectionName;
            }

            throw ArgumentOutOfRange(keyType);
        }

        private RefGroupBuilder SetIncludedOrExcludedValues<T>(
            IEnumerable<T> objectIDs,
            bool isIncluded)
            where T : struct
        {
            ThrowIfNull(objectIDs);

            this.AddPendingAction(
                new PendingAction(
                    TestHelper.GetCallerMemberFullName(this),
                    (_, _) =>
                    {
                        var (keyType, result) = this.GetKeyType();

                        if (!keyType.HasValue)
                        {
                            return ValueTask.FromResult(result);
                        }

                        var sectionName = isIncluded
                            ? GetIncludedSectionNameByKeyType(keyType.Value)
                            : GetExcludedSectionNameByKeyType(keyType.Value);

                        var card = this.GetCardOrThrow();
                        var sectionRows = card.Sections[sectionName].Rows;

                        foreach (var objectID in objectIDs)
                        {
                            var newRow = sectionRows.Add();
                            newRow.State = CardRowState.Inserted;
                            newRow.RowID = Guid.NewGuid();
                            newRow.Fields[RefGroupsHelper.RefGroupsVirtualValuesValueID] = objectID;
                            newRow.Fields[RefGroupsHelper.RefGroupsVirtualValuesValueName] = objectID.ToString();
                        }

                        return ValueTask.FromResult(ValidationResult.Empty);
                    }));

            return this;
        }

        private RefGroupBuilder RemoveAllIncludedOrExcludedValues(
            bool isInclude,
            string actionName)
        {
            this.AddPendingAction(
                new PendingAction(
                    actionName,
                    (_, _) =>
                    {
                        var (keyType, result) = this.GetKeyType();

                        if (!keyType.HasValue)
                        {
                            return ValueTask.FromResult(result);
                        }

                        var sectionName = isInclude
                            ? GetIncludedSectionNameByKeyType(keyType.Value)
                            : GetExcludedSectionNameByKeyType(keyType.Value);

                        var card = this.GetCardOrThrow();
                        var sectionRows = card.Sections[sectionName].Rows;

                        for (var i = sectionRows.Count - 1; i >= 0; i--)
                        {
                            var row = sectionRows[i];
                            if (row.State == CardRowState.Inserted)
                            {
                                sectionRows.RemoveAt(i);
                            }
                            else
                            {
                                row.State = CardRowState.Deleted;
                            }
                        }

                        return ValueTask.FromResult(ValidationResult.Empty);
                    }));

            return this;
        }

        #endregion
    }
}
