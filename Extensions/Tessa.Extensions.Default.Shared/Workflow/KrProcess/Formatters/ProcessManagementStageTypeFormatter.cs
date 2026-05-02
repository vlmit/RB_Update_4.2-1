#nullable enable

using System.Collections.Generic;
using System.Threading.Tasks;
using Tessa.Platform;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Shared.Workflow.KrProcess.Formatters
{
    /// <summary>
    /// Форматтер типа этапа <see cref="StageTypeDescriptors.ProcessManagementDescriptor"/>.
    /// </summary>
    public sealed class ProcessManagementStageTypeFormatter :
        StageTypeFormatterBase
    {
        #region Base Overrides

        /// <inheritdoc />
        public override ValueTask FormatClientAsync(
            IStageTypeFormatterContext context)
        {
            FormatInternal(
                context,
                context.StageRow.Fields,
                true);

            return ValueTask.CompletedTask;
        }

        /// <inheritdoc />
        public override ValueTask FormatServerAsync(
            IStageTypeFormatterContext context)
        {
            FormatInternal(
                context,
                context.Settings!,
                false);

            return ValueTask.CompletedTask;
        }

        #endregion

        #region Private Methods

        private static void FormatInternal(
            IStageTypeFormatterContext context,
            IDictionary<string, object?> settings,
            bool isClient)
        {
            var sb = StringBuilderHelper.Acquire(256);

            AppendString(
                sb,
                settings.TryGet<string>(KrConstants.KrProcessManagementStageSettingsVirtual.ModeName),
                null,
                true,
                limit: isClient ? DefaultSettingMax : -1);

            var modeID = (ProcessManagementStageTypeMode?) settings.TryGet<int?>(KrConstants.KrProcessManagementStageSettingsVirtual.ModeID);
            switch (modeID)
            {
                case ProcessManagementStageTypeMode.StageMode:
                    var stageName = settings.TryGet<string>(KrConstants.KrProcessManagementStageSettingsVirtual.StageName)?.Trim();

                    if (!string.IsNullOrEmpty(stageName))
                    {
                        AppendString(
                            sb,
                            stageName,
                            null,
                            true,
                            limit: isClient ? DefaultSettingMax : -1);

                        var groupRowName = settings.TryGet<string>(KrConstants.KrProcessManagementStageSettingsVirtual.StageRowGroupName)?.Trim();

                        if (!string.IsNullOrEmpty(groupRowName))
                        {
                            sb.Append(" (");
                            AppendString(
                                sb,
                                groupRowName,
                                null,
                                true,
                                limit: isClient ? DefaultSettingMax : -1,
                                appendNewLine: false);
                            sb.Append(')');
                        }
                    }

                    break;

                case ProcessManagementStageTypeMode.GroupMode:
                    AppendString(
                        sb,
                        settings.TryGet<string>(KrConstants.KrProcessManagementStageSettingsVirtual.StageGroupName),
                        null,
                        true,
                        limit: isClient ? DefaultSettingMax : -1);

                    break;

                case ProcessManagementStageTypeMode.SendSignalMode:
                    AppendString(
                        sb,
                        settings.TryGet<string>(KrConstants.KrProcessManagementStageSettingsVirtual.Signal),
                        null,
                        false,
                        limit: isClient ? DefaultSettingMax : -1);

                    break;
            }

            if (settings.TryGet<bool?>(KrConstants.KrProcessManagementStageSettingsVirtual.ManagePrimaryProcess) == true)
            {
                if (sb.Length > 0)
                {
                    sb.AppendLine();
                }

                sb.Append("{$CardTypes_Controls_ManagePrimaryProcess}");
            }

            context.DisplaySettings = sb.ToStringAndRelease();
        }

        #endregion
    }
}
