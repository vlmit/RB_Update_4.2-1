#nullable enable

using System;
using System.Collections.Generic;
using Tessa.Platform.Conditions;
using Tessa.Workflow;

namespace Tessa.Extensions.Default.Server.Workflow.KrCompilers
{
    /// <summary>
    /// Информация о вторичном процессе, работающем в режиме "Кнопка".
    /// </summary>
    public interface IKrProcessButton :
        IKrSecondaryProcess,
        IVisibilitySources
    {
        /// <summary>
        /// Отображаемое название кнопки.
        /// </summary>
        string? Caption { get; }

        /// <summary>
        /// Значок.
        /// </summary>
        string? Icon { get; }

        /// <summary>
        /// Подсказка.
        /// </summary>
        string? Tooltip { get; }

        /// <summary>
        /// Группа кнопки.
        /// </summary>
        string? TileGroup { get; }

        /// <summary>
        /// Спрашивать подтверждение перед выполнением.
        /// </summary>
        bool AskConfirmation { get; }

        /// <summary>
        /// Текст подтверждения перед выполнением.
        /// </summary>
        string? ConfirmationMessage { get; }

        /// <summary>
        /// Значение, показывающее, необходимо ли группировать тайл в группу "Действия".
        /// </summary>
        bool ActionGrouping { get; }

        /// <summary>
        /// Сочетание клавиш.
        /// </summary>
        string? ButtonHotkey { get; }

        /// <summary>
        /// Порядок кнопки.
        /// </summary>
        int Order { get; }

        /// <summary>
        /// Возвращает перечисление параметров условий.
        /// </summary>
        IEnumerable<ConditionSettings> Conditions { get; }

        /// <summary>
        /// Идентификатор обработчика тайла вторичного процесса.
        /// </summary>
        Guid? HandlerID { get; }

        /// <inheritdoc cref="ButtonToolbarVisibilityMode"/>
        public ButtonToolbarVisibilityMode ToolbarVisibilityMode { get; }

        /// <summary>
        /// Алиас кнопки вторичного процесса.
        /// </summary>
        public string? Alias { get; }
        
        /// <summary>
        /// Скрыть из интерфейса.
        /// </summary>
        public bool Hidden { get; }
    }
}
