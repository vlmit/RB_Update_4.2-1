#nullable enable

using System;
using System.Collections.Generic;
using Tessa.Platform.Collections;

namespace Tessa.Extensions.Default.Server.Workflow.KrObjectModel
{
    /// <summary>
    /// Объект, предоставляющий информацию по текущему и основному процессу.
    /// </summary>
    public sealed class ProcessHolder
    {
        #region Properties

        /// <summary>
        /// Название типа родительского процесса.
        /// </summary>
        /// <remarks>Например, если текущий вложенный процесс был запущен из другого вложенного процесса который, в свою очередь, был запущен из основного процесса, то родительским процессом будет основной процесс.</remarks>
        public string? MainProcessType { get; init; }

        /// <summary>
        /// Значение, показывающее, что текущий объект является персистентным.
        /// </summary>
        public bool Persistent { get; init; }

        /// <summary>
        /// Идентификатор, по которому можно получить процессный сателлит.
        /// </summary>
        /// <remarks>
        /// <list type="table">
        /// <listheader>
        ///     <description>Тип процесса</description>
        ///     <description>Описание значения</description>
        /// </listheader>
        /// <item>
        ///     <description>Основной процесс</description>
        ///     <description>Идентификатор карточки, в которой запущен процесс</description>
        /// </item>
        /// <item>
        ///     <description>Вторичный асинхронный процесс</description>
        ///     <description>Идентификатор вторичного процесса</description>
        /// </item>
        /// <item>
        ///     <description>Вторичный синхронный процесс</description>
        ///     <description>Произвольный идентификатор</description>
        /// </item>
        /// <item>
        ///     <description>Вложенный процесс</description>
        ///     <description>Если родительский процесс основной, то идентификатор основной карточки, если вторичный - идентификатор вторичного процесса</description>
        /// </item>
        /// </list>
        /// </remarks>
        public Guid ProcessHolderID { get; init; }

        /// <summary>
        /// Объектная модель текущего и/или основного процесса.
        /// </summary>
        public WorkflowProcess? MainWorkflowProcess { get; set; }

        /// <summary>
        /// Информация по вложенным процессам. Ключ - идентификатор процесса, значение - объектная модель процесса.
        /// </summary>
        public Dictionary<Guid, WorkflowProcess> NestedWorkflowProcesses { get; } = [];

        /// <summary>
        /// Информация об основном процессе.
        /// </summary>
        public MainProcessCommonInfo? PrimaryProcessCommonInfo { get; set; }

        /// <summary>
        /// Информация о текущем процессе.
        /// </summary>
        /// <remarks>Содержит информацию из контекстуального сателлита. Если процесс является основным (<see cref="MainProcessType"/> равно <see cref="Shared.Workflow.KrProcess.KrConstants.KrProcessName"/>), то возвращает <see cref="PrimaryProcessCommonInfo"/>.</remarks>
        public MainProcessCommonInfo? MainProcessCommonInfo { get; set; }

        /// <summary>
        /// Информация о вложенных процессах. Ключ - идентификатор вложенного процесса, значение - информация о вложенном процессе.
        /// </summary>
        /// <remarks>Для задания новой коллекции объектов <see cref="NestedProcessCommonInfo"/> используйте метод <see cref="SetNestedProcessCommonInfosList"/>.</remarks>
        public HashSet<Guid, NestedProcessCommonInfo>? NestedProcessCommonInfos { get; set; }

        /// <summary>
        /// Инициализирует значение свойства <see cref="NestedProcessCommonInfos"/>.
        /// </summary>
        public void SetNestedProcessCommonInfosList(IEnumerable<NestedProcessCommonInfo>? collection) =>
            this.NestedProcessCommonInfos = collection is null
                ? null
                : new HashSet<Guid, NestedProcessCommonInfo>(static x => x.NestedProcessID, collection);

        #endregion
    }
}
