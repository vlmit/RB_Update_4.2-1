#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Tessa.BusinessCalendar;
using Tessa.Cards;
using Tessa.Platform;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;

namespace Tessa.Test.Default.Shared.Kr
{
    /// <summary>
    /// Предоставляет методы, выполняющие настройку временных зон (Настройки -&gt; Временные зоны).
    /// </summary>
    public sealed class TimeZonesBuilder :
        CardLifecycleCompanion<TimeZonesBuilder>
    {
        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="TimeZonesBuilder"/>.
        /// </summary>
        /// <param name="deps">Зависимости, используемые при взаимодействии с карточками.</param>
        public TimeZonesBuilder(
            ICardLifecycleCompanionDependencies deps)
            : base(
                  CardHelper.TimeZonesTypeID,
                  CardHelper.TimeZonesTypeName,
                  deps)
        { }

        #endregion

        #region Public Methods

        /// <summary>
        /// Добавляет временную зону.
        /// </summary>
        /// <param name="zoneID">Идентификатор зоны.</param>
        /// <param name="offsetMinutes">Смещение (минуты).</param>
        /// <param name="code">Код зоны или значение <see langword="null"/>, если он будет создан автоматически.</param>
        /// <param name="displayName">Отображаемое значение или значение <see langword="null"/>, если оно будет создано автоматически.</param>
        /// <returns>Объект <see cref="TimeZonesBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public TimeZonesBuilder AddTimeZone(
            short zoneID,
            int offsetMinutes,
            string? code = null,
            string? displayName = null)
        {
            this.AddPendingAction(
                new PendingAction(
                    TestHelper.GetCallerMemberFullName(this),
                    (_, _) =>
                    {
                        var rows = this.GetTimeZonesRows();
                        var newRow = rows.Add();
                        newRow.RowID = Guid.NewGuid();
                        newRow.Fields["CodeName"] = code ?? $"Test {offsetMinutes} m";
                        newRow.Fields["IsNegativeOffsetDirection"] = BooleanBoxes.Box(offsetMinutes < 0);
                        newRow.Fields["OffsetTime"] = CardHelper.DefaultDateTime.Date.Add(new TimeSpan(0, 0, Math.Abs(offsetMinutes), 0));
                        newRow.Fields["DisplayName"] = displayName ?? $"Test {offsetMinutes} Display name";

                        // Значения не используются.
                        // Правильные значения вычисляются на сервере при сохранении.
                        newRow.Fields["UtcOffsetMinutes"] = null;
                        newRow.Fields["ShortName"] = null;
                        newRow.Fields["ZoneID"] = Int32Boxes.Box(zoneID);
                        newRow.State = CardRowState.Inserted;

                        return ValueTask.FromResult(ValidationResult.Empty);
                    }));

            return this;
        }

        /// <summary>
        /// Добавляет в карточку временной зоны информацию о часовом поясе по умолчанию.
        /// </summary>
        /// <returns>Объект <see cref="TimeZonesBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public TimeZonesBuilder WithDefaultZone()
        {
            this.AddPendingAction(
                new PendingAction(
                    TestHelper.GetCallerMemberFullName(this),
                    (_, _) =>
                    {
                        var card = this.GetCardOrThrow();

                        // основная секция
                        var defaultTimeZoneFields = card.Sections[TimeZonesHelper.DefaultTimeZoneSection].Fields;
                        defaultTimeZoneFields["CodeName"] = TimeZonesHelper.DefaultCodeName;
                        defaultTimeZoneFields["UtcOffsetMinutes"] = TimeZonesHelper.DefaultUtcOffsetMinutes;
                        defaultTimeZoneFields["DisplayName"] = TimeZonesHelper.DefaultDisplayName;
                        defaultTimeZoneFields["ShortName"] = TimeZonesHelper.DefaultShortName;
                        defaultTimeZoneFields["IsNegativeOffsetDirection"] = BooleanBoxes.Box(TimeZonesHelper.DefaultIsNegativeOffsetDirection);
                        defaultTimeZoneFields["OffsetTime"] = TimeZonesHelper.DefaultOffsetTime;
                        defaultTimeZoneFields["ZoneID"] = Int32Boxes.Box(TimeZonesHelper.DefaultZoneID);

                        return new ValueTask<ValidationResult>(ValidationResult.Empty);
                    }));

            return this;
        }

        /// <summary>
        /// Устанавливает значение параметра "Разрешить менять зоны в ролях".
        /// </summary>
        /// <param name="value">Значение параметра "Разрешить менять зоны в ролях".</param>
        /// <returns>Объект <see cref="TimeZonesBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public TimeZonesBuilder SetAllowToModify(bool value) =>
            this.SetTimeZonesSettingsField("AllowToModify", BooleanBoxes.Box(value));

        /// <summary>
        /// Возвращает значение параметра "Разрешить менять зоны в ролях".
        /// </summary>
        /// <returns>Значение параметра "Разрешить менять зоны в ролях".</returns>
        public bool GetAllowToModify() =>
            this.TryGetTimeZonesSettingsField<bool>("AllowToModify");

        /// <summary>
        /// Возвращает строки с информацией о временных зонах.
        /// </summary>
        /// <returns>Строки с информацией о временных зонах.</returns>
        public ListStorage<CardRow> GetTimeZonesRows() => this.GetCardOrThrow().Sections[TimeZonesHelper.TimeZonesVirtualSection].Rows;

        #endregion

        #region Private Methods

        /// <summary>
        /// Задаёт значение указанного поля секции <see cref="TimeZonesHelper.TimeZonesSettingsSection"/>.
        /// </summary>
        /// <param name="field">Имя поля.</param>
        /// <param name="value">Значение.</param>
        /// <returns>Объект <see cref="TimeZonesBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        private TimeZonesBuilder SetTimeZonesSettingsField(
            string field,
            object? value) =>
            this.SetValue(TimeZonesHelper.TimeZonesSettingsSection, field, value);

        /// <summary>
        /// Возвращает значение указанного поля секции <see cref="TimeZonesHelper.TimeZonesSettingsSection"/>.
        /// </summary>
        /// <typeparam name="T">Тип возвращаемого значения.</typeparam>
        /// <param name="field">Имя поля.</param>
        /// <returns>Возвращаемое значение.</returns>
        private T? TryGetTimeZonesSettingsField<T>(
            string field) =>
            this.TryGetValue<T>(TimeZonesHelper.TimeZonesSettingsSection, field);

        #endregion
    }
}
