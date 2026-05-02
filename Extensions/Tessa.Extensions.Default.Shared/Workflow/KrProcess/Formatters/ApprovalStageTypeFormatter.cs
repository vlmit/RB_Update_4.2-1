#nullable enable

using System.Collections.Generic;
using System.Threading.Tasks;
using Tessa.Platform;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Shared.Workflow.KrProcess.Formatters
{
    /// <summary>
    /// Форматтер типа этапа <see cref="StageTypeDescriptors.ApprovalDescriptor"/>.
    /// </summary>
    public sealed class ApprovalStageTypeFormatter :
        StageTypeFormatterBase
    {
        #region Base Overrides

        /// <inheritdoc />
        public override async ValueTask FormatClientAsync(
            IStageTypeFormatterContext context)
        {
            await base.FormatClientAsync(context);

            FormatInternal(
                context,
                context.StageRow);
        }

        /// <inheritdoc />
        public override async ValueTask FormatServerAsync(
            IStageTypeFormatterContext context)
        {
            await base.FormatServerAsync(context);

            FormatInternal(
                context,
                context.Settings!);
        }

        #endregion

        #region Private Methods

        private static void FormatInternal(
            IStageTypeFormatterContext context,
            IDictionary<string, object?> storage)
        {
            var sb = StringBuilderHelper.Acquire(128);

            if (storage.TryGet<bool>(KrConstants.KrApprovalSettingsVirtual.Advisory))
            {
                sb.AppendLine("{$UI_KrApproval_Advisory}");
            }

            sb.Append(storage.TryGet<bool>(KrConstants.KrApprovalSettingsVirtual.IsParallel)
                ? "{$UI_KrApproval_Parallel}"
                : "{$UI_KrApproval_Sequential}");

            context.DisplaySettings = sb.ToStringAndRelease();
        }

        #endregion
    }
}
