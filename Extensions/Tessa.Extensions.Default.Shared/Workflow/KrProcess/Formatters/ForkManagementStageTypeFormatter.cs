#nullable enable

using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tessa.Platform;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Shared.Workflow.KrProcess.Formatters
{
    /// <summary>
    /// Форматтер типа этапа <see cref="StageTypeDescriptors.ForkManagementDescriptor"/>.
    /// </summary>
    public sealed class ForkManagementStageTypeFormatter :
        ForkStageTypeFormatterBase
    {
        #region Base Overrides

        /// <inheritdoc />
        public override ValueTask FormatClientAsync(
            IStageTypeFormatterContext context)
        {
            var settingsRows = context.Card.Sections
                .TryGet(KrConstants.KrForkSecondaryProcessesSettingsVirtual.Synthetic)
                ?.TryGetRows();

            FormatInternal(
                context,
                context.StageRow.Fields,
                settingsRows,
                true);

            return ValueTask.CompletedTask;
        }

        /// <inheritdoc />
        public override ValueTask FormatServerAsync(
            IStageTypeFormatterContext context)
        {
            var settingsRows = context.Settings
                !.TryGet<IList>(KrConstants.KrForkSecondaryProcessesSettingsVirtual.Synthetic);

            FormatInternal(
                context,
                context.Settings!,
                settingsRows,
                false);

            return ValueTask.CompletedTask;
        }

        #endregion

        #region Private Methods

        private static void FormatInternal(
            IStageTypeFormatterContext context,
            IDictionary<string, object?> settings,
            IList? settingsRows,
            bool isClient)
        {
            var sb = StringBuilderHelper.Acquire();

            AppendString(
                sb,
                settings.TryGet<string>(KrConstants.KrForkManagementSettingsVirtual.ModeName),
                null,
                true);

            AppendSecondaryProcessesNames(
                sb,
                context,
                settingsRows,
                isClient);

            context.DisplaySettings = sb.ToStringAndRelease();
        }

        #endregion
    }
}
