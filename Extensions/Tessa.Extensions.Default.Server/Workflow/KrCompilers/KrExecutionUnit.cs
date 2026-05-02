#nullable enable

using System;
using System.Diagnostics;
using Tessa.Extensions.Default.Server.Workflow.KrCompilers.UserAPI;
using Tessa.Platform;

namespace Tessa.Extensions.Default.Server.Workflow.KrCompilers
{
    /// <inheritdoc cref="IKrExecutionUnit" />
    [DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
    public sealed class KrExecutionUnit :
        IKrExecutionUnit
    {
        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса.
        /// </summary>
        /// <param name="stageTemplate"><inheritdoc cref="IKrStageTemplate" path="/summary"/></param>
        /// <param name="instance"><inheritdoc cref="IKrScript" path="/summary"/></param>
        public KrExecutionUnit(
            IKrStageTemplate stageTemplate,
            IKrScript instance)
        {
            this.StageTemplate = NotNullOrThrow(stageTemplate);
            this.Instance = NotNullOrThrow(instance);

            this.ID = stageTemplate.ID;
            this.Name = stageTemplate.Name;
            this.DesignTimeSources = stageTemplate;
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса.
        /// </summary>
        /// <param name="runtimeStage"><inheritdoc cref="IKrRuntimeStage" path="/summary"/></param>
        /// <param name="instance"><inheritdoc cref="IKrScript" path="/summary"/></param>
        public KrExecutionUnit(
            IKrRuntimeStage runtimeStage,
            IKrScript instance)
        {
            this.RuntimeStage = NotNullOrThrow(runtimeStage);
            this.Instance = NotNullOrThrow(instance);

            this.ID = runtimeStage.StageID;
            this.Name = runtimeStage.StageName;
            this.RuntimeSources = runtimeStage;
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса.
        /// </summary>
        /// <param name="stageGroup"><inheritdoc cref="IKrStageGroup" path="/summary"/></param>
        /// <param name="instance"><inheritdoc cref="IKrScript" path="/summary"/></param>
        public KrExecutionUnit(
            IKrStageGroup stageGroup,
            IKrScript instance)
        {
            this.StageGroup = NotNullOrThrow(stageGroup);
            this.Instance = NotNullOrThrow(instance);

            this.ID = stageGroup.ID;
            this.Name = stageGroup.Name;
            this.RuntimeSources = stageGroup;
            this.DesignTimeSources = stageGroup;
        }

        #endregion

        #region IKrExecutionUnit Members

        /// <inheritdoc />
        public Guid ID { get; }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public IKrStageTemplate? StageTemplate { get; }

        /// <inheritdoc />
        public IKrStageGroup? StageGroup { get; }

        /// <inheritdoc />
        public IKrRuntimeStage? RuntimeStage { get; }

        /// <inheritdoc />
        public IRuntimeSources? RuntimeSources { get; }

        /// <inheritdoc />
        public IDesignTimeSources? DesignTimeSources { get; }

        /// <inheritdoc />
        public IKrScript Instance { get; }

        #endregion

        #region Private Methods

        private string GetDebuggerDisplay() =>
            $"{DebugHelper.GetTypeName(this)}: "
            + $"{nameof(this.ID)}: {this.ID}"
            + $"{nameof(this.Name)}: \"{this.Name}\"";

        #endregion
    }
}
