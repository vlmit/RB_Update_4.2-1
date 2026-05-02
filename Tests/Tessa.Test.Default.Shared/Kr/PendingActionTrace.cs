#nullable enable

using System.Collections.Generic;
using System.Text;
using Tessa.Platform;

namespace Tessa.Test.Default.Shared.Kr
{
    /// <summary>
    /// Выполняет построение трассировочной информации.
    /// </summary>
    public sealed class PendingActionTrace
    {
        #region Constants

        private const int DefaultCapacity = 5;

        private const int DefaultLineLength = 20;

        #endregion

        #region Fields

        private readonly IList<IPendingAction> pendingActions;

        #endregion

        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр объекта c начальной ёмкостью внутреннего хранилища по умолчанию.
        /// </summary>
        public PendingActionTrace()
            : this(DefaultCapacity)
        {
        }

        /// <summary>
        /// Инициализирует новый экземпляр объекта с заданной начальной ёмкостью.
        /// </summary>
        /// <param name="capacity">Начальная ёмкость внутреннего хранилища.</param>
        public PendingActionTrace(int capacity) =>
            this.pendingActions = new List<IPendingAction>(capacity);

        #endregion

        #region Public Methods

        /// <summary>
        /// Добавляет новое отложенное действие для включения его в трассировочную информацию.
        /// </summary>
        /// <param name="pendingAction">Добавляемое отложенное действие.</param>
        public void Add(IPendingAction pendingAction) =>
            this.pendingActions.Add(NotNullOrThrow(pendingAction));

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override string ToString()
        {
            var pendingActionsCount = this.pendingActions.Count;

            if (pendingActionsCount == 0)
            {
                return string.Empty;
            }

            var sb = StringBuilderHelper.Acquire(pendingActionsCount * DefaultLineLength);

            AppendActions(sb, this.pendingActions);

            return sb.ToString();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Добавляет информацию о действиях.
        /// </summary>
        /// <param name="sb">Объект, выполняющий построение строки с трассировкой.</param>
        /// <param name="pendingActions">Перечисление содержащее отложенные действия информацию о которых требуется добавить.</param>
        private static void AppendActions(
            StringBuilder sb,
            IList<IPendingAction> pendingActions)
        {
            const string formatDisplayName = "Serial number {0}: {1}";

            for (var i = 0; i < pendingActions.Count; i++)
            {
                sb
                    .AppendFormat(
                        formatDisplayName,
                        (i + 1).ToString(),
                        pendingActions[i])
                    .AppendLine();
            }
        }

        #endregion
    }
}
