#nullable enable

using System;
using System.Linq;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Extensions.Default.Shared;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform;
using Tessa.Platform.Storage;
using Tessa.UI.Cards;

namespace Tessa.Extensions.Default.Client.UI.KrProcess.StageHandlers
{
    /// <summary>
    /// UI обработчик типа этапа <see cref="StageTypeDescriptors.ApprovalProcessDescriptor"/>.
    /// </summary>
    public sealed class ApprovalProcessUIHandler :
        StageTypeUIHandlerBase
    {
        #region Constants

        private const string ProcessTemplateControlName = "ProcessTemplate";
        private const string UseProcessFromCardControlName = "UseProcessFromCard";

        #endregion

        #region Fields

        private CardRow? settings;
        private IControlViewModel? returnAfterDisapprovalFlagControl;
        private IControlViewModel? notReturnEditFlagControl;
        private IControlViewModel? processTemplateControl;
        private IControlViewModel? useProcessFromCardControl;

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override Task Initialize(IKrStageTypeUIHandlerContext context)
        {
            if (context.SettingsForms.FirstOrDefault(static i => i.Name == DefaultCardTypes.KrApprovalProcessStageTypeSettingsTypeName) is { } form
                && form.Blocks.FirstOrDefault(static i => i.Name == "CommonSettings") is { } commonBlock)
            {
                this.returnAfterDisapprovalFlagControl = commonBlock
                    .Controls
                    .FirstOrDefault(static i => i.Name == KrConstants.Ui.ReturnAfterDisapproval);

                this.notReturnEditFlagControl = commonBlock
                    .Controls
                    .FirstOrDefault(static i => i.Name == KrConstants.Ui.NotReturnEdit);

                this.processTemplateControl = commonBlock
                    .Controls
                    .FirstOrDefault(static i => i.Name == ProcessTemplateControlName);

                this.useProcessFromCardControl = commonBlock
                    .Controls
                    .FirstOrDefault(static i => i.Name == UseProcessFromCardControlName);
            }

            this.settings = context.Row;
            this.settings.FieldChanged += this.OnSettingsFieldChanged;

            this.SetRealOnlyIfEditable(
                this.returnAfterDisapprovalFlagControl,
                static row => row.TryGet<bool>(
                    KrConstants.KrApprovalProcessSettingsVirtual.NotReturnEdit));

            this.SetRealOnlyIfEditable(
                this.notReturnEditFlagControl,
                static row => row.TryGet<bool>(
                    KrConstants.KrApprovalProcessSettingsVirtual.ReturnAfterDisapproval));

            this.SetRealOnlyIfEditable(
                this.processTemplateControl,
                static row => row.TryGet<bool>(
                    KrConstants.KrApprovalProcessSettingsVirtual.UseProcessFromCard));

            this.SetRealOnlyIfEditable(
                this.useProcessFromCardControl,
                static row => row.TryGet<Guid?>(
                    KrConstants.KrApprovalProcessSettingsVirtual.TemplateID).HasValue);

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

            return Task.CompletedTask;
        }

        #endregion

        #region Private Methods

        private void OnSettingsFieldChanged(
            object? sender,
            CardFieldChangedEventArgs e)
        {
            if (e.FieldName == KrConstants.KrApprovalProcessSettingsVirtual.NotReturnEdit
                && e.FieldValue is bool notReturnEdit)
            {
                this.SetRealOnly(
                    notReturnEdit,
                    this.returnAfterDisapprovalFlagControl,
                    static row =>
                        row.Fields[KrConstants.KrApprovalProcessSettingsVirtual.ReturnAfterDisapproval] = BooleanBoxes.False);
            }
            else if (e.FieldName == KrConstants.KrApprovalProcessSettingsVirtual.ReturnAfterDisapproval
                && e.FieldValue is bool returnAfterDisapproval)
            {
                this.SetRealOnly(
                    returnAfterDisapproval,
                    this.notReturnEditFlagControl,
                    static row =>
                        row.Fields[KrConstants.KrApprovalProcessSettingsVirtual.NotReturnEdit] = BooleanBoxes.False);
            }
            else if (e.FieldName == KrConstants.KrApprovalProcessSettingsVirtual.UseProcessFromCard
                && e.FieldValue is bool useProcessFromCard)
            {
                this.SetRealOnly(
                    useProcessFromCard,
                    this.processTemplateControl,
                    static row =>
                    {
                        row.Fields[KrConstants.KrApprovalProcessSettingsVirtual.TemplateID] = null;
                        row.Fields[KrConstants.KrApprovalProcessSettingsVirtual.TemplateName] = null;
                    });
            }
            else if (e.FieldName == KrConstants.KrApprovalProcessSettingsVirtual.TemplateID)
            {
                this.SetRealOnly(
                    e.FieldValue is not null,
                    this.useProcessFromCardControl,
                    static row =>
                        row.Fields[KrConstants.KrApprovalProcessSettingsVirtual.UseProcessFromCard] = BooleanBoxes.False);
            }
        }

        private void SetRealOnly(
            bool isReadOnly,
            IControlViewModel? control,
            Action<CardRow> resetFieldAction)
        {
            if (isReadOnly && this.settings is not null)
            {
                resetFieldAction(this.settings);
            }

            if (control is not null)
            {
                control.IsReadOnly = isReadOnly;
            }
        }

        private void SetRealOnlyIfEditable(
            IControlViewModel? controlViewModel,
            Func<CardRow, bool> getIsReadOnly)
        {
            if (this.settings is not null
                && controlViewModel is not null
                && !controlViewModel.IsReadOnly)
            {
                controlViewModel.IsReadOnly = getIsReadOnly(this.settings);
            }
        }

        #endregion
    }
}
