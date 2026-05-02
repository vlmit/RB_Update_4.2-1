#nullable enable

using System.Collections;
using System.Threading.Tasks;
using Tessa.Platform;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Shared.Workflow.KrProcess.Formatters
{
    /// <summary>
    /// Форматтер типа этапа <see cref="StageTypeDescriptors.ForkDescriptor"/>.
    /// </summary>
    public sealed class ForkStageTypeFormatter :
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
                settingsRows,
                false);

            return ValueTask.CompletedTask;
        }

        #endregion

        #region Private Methods

        private static void FormatInternal(
            IStageTypeFormatterContext context,
            IList? settingsRows,
            bool isClient)
        {
            var sb = StringBuilderHelper.Acquire();

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
