#nullable enable

using System;
using Tessa.Extensions.Default.Server.Workflow.KrCompilers.UserAPI;

namespace Tessa.Extensions.Default.Server.Workflow.KrCompilers
{
    /// <summary>
    /// Единица выполнения объекта маршрута.
    /// </summary>
    /// <remarks>
    /// Представляет собой агрегацию метаинформации о шаблоне этапа или группе этапов (название, позиция, sql)
    /// и объекта, сгенерированного на основе карточки, скомпилированного и инстанцированного.
    /// </remarks>
    public interface IKrExecutionUnit
    {
        /// <summary>
        /// Идентификатор единицы выполнения.
        /// </summary>
        Guid ID { get; }

        /// <summary>
        /// Название единицы выполнения.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Информация о шаблоне этапов.
        /// </summary>
        IKrStageTemplate? StageTemplate { get; }

        /// <summary>
        /// Информация о группе этапов.
        /// </summary>
        IKrStageGroup? StageGroup { get; }

        /// <summary>
        /// Информация об этапе в процессе выполнения.
        /// </summary>
        IKrRuntimeStage? RuntimeStage { get; }

        /// <summary>
        /// Исходные коды на момент выполнения процесса.
        /// </summary>
        IRuntimeSources? RuntimeSources { get; }

        /// <summary>
        /// Исходные коды для этапа перерасчета.
        /// </summary>
        IDesignTimeSources? DesignTimeSources { get; }

        /// <summary>
        /// Сгенерированный на основе карточки, скомпилированный и созданный объект.
        /// </summary>
        IKrScript Instance { get; }
    }
}
