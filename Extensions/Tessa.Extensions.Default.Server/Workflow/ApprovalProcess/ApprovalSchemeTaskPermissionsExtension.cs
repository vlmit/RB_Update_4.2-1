#nullable enable

using System.Collections.Generic;
using System.Threading.Tasks;
using Tessa.Extensions.Default.Server.Workflow.KrPermissions;
using Tessa.Extensions.Default.Shared;
using Tessa.Extensions.Default.Shared.Workflow.KrPermissions;
using Tessa.Platform.Data;
using Tessa.Platform.Storage;
using Tessa.Scheme;
using static Tessa.Extensions.Default.Shared.Workflow.WorkflowEngine.WorkflowConstants;

namespace Tessa.Extensions.Default.Server.Workflow.ApprovalProcess
{
    /// <summary>
    /// Расширение, добавлеяющее право доступа <see cref="KrPermissionFlagDescriptors.EditApprovalScheme"/>,
    /// если в настройках задания был передан флаг <see cref="NamesKeys.CanEditApprovalScheme"/>.
    /// </summary>
    public class ApprovalSchemeTaskPermissionsExtension
        : ITaskPermissionsExtension
    {
        #region ITaskPermissionsExtension Members

        /// <inheritdoc/>
        public async Task ExtendPermissionsAsync(ITaskPermissionsExtensionContext context)
        {
            var task = context.Task;
            var taskType = context.TaskType;
            var taskTypeID = taskType.ID;

            if (taskTypeID != DefaultTaskTypes.KrEditTypeID &&
                taskTypeID != DefaultTaskTypes.KrEditInterjectTypeID)
            {
                return;
            }

            bool isCanEditApprovalScheme = default;
            if (context.Mode == KrPermissionsCheckMode.WithCard)
            {
                var settings = task.Settings;
                if (settings is not null &&
                    settings.TryGet<bool>(NamesKeys.CanEditApprovalScheme))
                {
                    isCanEditApprovalScheme = true;
                }
            }
            else
            {
                await using (context.DbScope.Create())
                {
                    var db = context.DbScope.Db;
                    var settingsString = await db
                        .SetCommand(
                            context.DbScope.BuilderFactory
                                .Select().C("Settings")
                                .From(Names.Tasks).NoLock()
                                .Where().C(Names.Table_RowID).Equals().P("TaskID")
                                .Build(),
                            db.Parameter("TaskID", task.RowID))
                        .LogCommand()
                        .ExecuteStringAsync(context.CancellationToken);

                    Dictionary<string, object?>? settings;
                    if (!string.IsNullOrEmpty(settingsString) &&
                        (settings = StorageHelper.DeserializeFromTypedJson(settingsString)) is not null)
                    {
                        isCanEditApprovalScheme = settings.TryGet<bool>(NamesKeys.CanEditApprovalScheme);
                    }
                }
            }

            if (isCanEditApprovalScheme)
            {
                context.Descriptor.Set(KrPermissionFlagDescriptors.EditApprovalScheme, true);
            }
        }

        #endregion
    }
}
