#nullable enable

using System.Collections.Generic;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Serialization
{
    /// <inheritdoc cref="IKrStageSerializerSettings"/>
    public sealed class KrStageSerializerSettings :
        IKrStageSerializerSettings
    {
        #region Properties

        /// <inheritdoc cref="IKrStageSerializerSettings.SettingsSectionNames"/>
        public HashSet<string> SettingsSectionNames { get; } = new HashSet<string>();

        /// <inheritdoc cref="IKrStageSerializerSettings.SettingsFieldNames"/>
        public HashSet<string> SettingsFieldNames { get; } = new HashSet<string>();

        /// <inheritdoc cref="IKrStageSerializerSettings.ReferencesToStages"/>
        public HashSet<ReferenceToStage> ReferencesToStages { get; } = new HashSet<ReferenceToStage>();

        #endregion

        #region IKrStageSerializerData Members

        /// <inheritdoc cref="IKrStageSerializerSettings.OrderColumns"/>
        public HashSet<OrderColumn> OrderColumns { get; } = new HashSet<OrderColumn>();

        /// <inheritdoc/>
        IReadOnlySet<string> IKrStageSerializerSettings.SettingsSectionNames => this.SettingsSectionNames;

        /// <inheritdoc/>
        IReadOnlySet<string> IKrStageSerializerSettings.SettingsFieldNames => this.SettingsFieldNames;

        /// <inheritdoc/>
        IReadOnlySet<ReferenceToStage> IKrStageSerializerSettings.ReferencesToStages => this.ReferencesToStages;

        /// <inheritdoc/>
        IReadOnlySet<OrderColumn> IKrStageSerializerSettings.OrderColumns => this.OrderColumns;

        #endregion
    }
}
