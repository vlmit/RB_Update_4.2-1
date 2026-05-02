#nullable enable

using System.Collections.Generic;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Serialization
{
    /// <summary>
    /// Информация по сериализуемым секциям и полям, используемая сериализатором <see cref="IKrStageSerializer"/>.
    /// </summary>
    public interface IKrStageSerializerSettings
    {
        /// <summary>
        /// Список секций, содержащих параметры этапов.
        /// </summary>
        IReadOnlySet<string> SettingsSectionNames { get; }

        /// <summary>
        /// Список полей, содержащих параметры этапов.
        /// </summary>
        IReadOnlySet<string> SettingsFieldNames { get; }

        /// <summary>
        /// Список прямых дочерних секций секции <see cref="KrConstants.KrStages.Name"/>.
        /// </summary>
        IReadOnlySet<ReferenceToStage> ReferencesToStages { get; }

        /// <summary>
        /// Список секций и столбцов, по которым выполняется сортировка.
        /// </summary>
        IReadOnlySet<OrderColumn> OrderColumns { get; }
    }
}
