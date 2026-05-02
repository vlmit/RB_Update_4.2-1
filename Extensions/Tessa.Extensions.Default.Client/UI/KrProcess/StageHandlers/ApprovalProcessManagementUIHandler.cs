#nullable enable

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Tessa.Cards;
using Tessa.Extensions.Default.Shared;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform;
using Tessa.Platform.Storage;
using Tessa.UI.Cards;
using Tessa.Workflow.ApprovalProcess;

namespace Tessa.Extensions.Default.Client.UI.KrProcess.StageHandlers
{
    /// <summary>
    /// UI обработчик типа этапа <see cref="StageTypeDescriptors.ApprovalProcessManagementDescriptor"/>.
    /// </summary>
    public sealed class ApprovalProcessManagementUIHandler :
        StageTypeUIHandlerBase
    {
        #region Constants

        private const string ChangeStateBlockName = "ChangeStateBlock";

        #endregion

        #region Fields

        private CardRow? settings;
        private IBlockViewModel? changeStateBlock;

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override Task Initialize(IKrStageTypeUIHandlerContext context)
        {
            if (context.SettingsForms.FirstOrDefault(static i => i.Name == DefaultCardTypes.KrApprovalProcessManagementStageTypeSettingsTypeName) is { } form
                && form.Blocks.FirstOrDefault(static i => i.Name == ChangeStateBlockName) is { } changeStateBlock)
            {
                this.changeStateBlock = changeStateBlock;
            }

            this.settings = context.Row;
            this.settings.FieldChanged += this.OnSettingsFieldChanged;

            this.UpdateBlockVisibility(this.settings.TryGet<Guid?>(KrConstants.KrApprovalProcessManagementSettingsVirtual.ControlTypeID), true);

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public override Task Finalize(IKrStageTypeUIHandlerContext context)
        {
            if (this.settings is not null)
            {
                this.settings.FieldChanged -= this.OnSettingsFieldChanged;
                this.settings = null;
            }

            this.changeStateBlock = null;

            return Task.CompletedTask;
        }

        #endregion

        #region Private Methods

        private void OnSettingsFieldChanged(
            object? sender,
            CardFieldChangedEventArgs e)
        {
            if (e.FieldName == KrConstants.KrApprovalProcessManagementSettingsVirtual.ControlTypeID)
            {
                this.UpdateBlockVisibility(e.FieldValue as Guid?);
            }
        }

        private void UpdateBlockVisibility(
            Guid? controlTypeID,
            bool isInit = false)
        {
            if (this.changeStateBlock is null
                || this.settings is null)
            {
                return;
            }

            if (controlTypeID == ApprovalProcessHelper.ChangeStateControlTypeID)
            {
                this.changeStateBlock.BlockVisibility = Visibility.Visible;
            }
            else
            {
                this.changeStateBlock.BlockVisibility = Visibility.Collapsed;
                if (!isInit)
                {
                    this.settings.Fields[KrConstants.KrApprovalProcessManagementSettingsVirtual.StateID] = null;
                    this.settings.Fields[KrConstants.KrApprovalProcessManagementSettingsVirtual.StateName] = null;
                    this.settings.Fields[KrConstants.KrApprovalProcessManagementSettingsVirtual.ShowRevokeButton] = BooleanBoxes.False;
                    this.settings.Fields[KrConstants.KrApprovalProcessManagementSettingsVirtual.UpdateHistoryGroup] = BooleanBoxes.False;
                }
            }
        }

        #endregion
    }
}
