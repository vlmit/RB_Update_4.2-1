#nullable enable
using System;
using System.Collections.Generic;
using Tessa.Platform;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Shared.Workflow.KrProcess
{
    [StorageObjectGenerator]
    public sealed partial class KrCardType : StorageObject, IKrType
    {
        #region Constructors

        public KrCardType(Dictionary<string, object?> storage)
            : base(storage)
        {
        }

        #endregion

        #region Storage Properties

        private const string IDKey = "ID";

        public Guid ID
        {
            get => this.Get<Guid>(IDKey);
            set => this.Set(IDKey, value);
        }

        private const string NameKey = "Name";

        public string? Name
        {
            get => this.Get<string>(NameKey);
            set => this.Set(NameKey, value);
        }

        private const string CaptionKey = "Caption";

        public string? Caption
        {
            get => this.Get<string>(CaptionKey);
            set => this.Set(CaptionKey, value);
        }

        private const string UseDocTypesKey = "UseDocTypes";

        public bool UseDocTypes
        {
            get => this.Get<bool>(UseDocTypesKey);
            set => this.Set(UseDocTypesKey, BooleanBoxes.Box(value));
        }

        private const string UseApprovingKey = "UseApproving";

        public bool UseApproving
        {
            get => this.Get<bool>(UseApprovingKey);
            set => this.Set(UseApprovingKey, BooleanBoxes.Box(value));
        }

        private const string UseRegistrationKey = "UseRegistration";

        public bool UseRegistration
        {
            get => this.Get<bool>(UseRegistrationKey);
            set => this.Set(UseRegistrationKey, BooleanBoxes.Box(value));
        }

        private const string UseResolutionsKey = "UseResolutions";

        public bool UseResolutions
        {
            get => this.Get<bool>(UseResolutionsKey);
            set => this.Set(UseResolutionsKey, BooleanBoxes.Box(value));
        }

        private const string DisableChildResolutionDateCheckKey = "DisableChildResolutionDateCheck";

        public bool DisableChildResolutionDateCheck
        {
            get => this.Get<bool>(DisableChildResolutionDateCheckKey);
            set => this.Set(DisableChildResolutionDateCheckKey, BooleanBoxes.Box(value));
        }

        private const string DocNumberRegularAutoAssignmentIDKey = "DocNumberRegularAutoAssignmentID";

        public KrDocNumberRegularAutoAssignmentID DocNumberRegularAutoAssignmentID
        {
            get => (KrDocNumberRegularAutoAssignmentID) this.Get<int>(DocNumberRegularAutoAssignmentIDKey);
            set => this.Set(DocNumberRegularAutoAssignmentIDKey, Int32Boxes.Box((int) value));
        }

        private const string DocNumberRegularSequenceKey = "DocNumberRegularSequence";

        public string? DocNumberRegularSequence
        {
            get => this.Get<string>(DocNumberRegularSequenceKey);
            set => this.Set(DocNumberRegularSequenceKey, value);
        }

        private const string DocNumberRegularFormatKey = "DocNumberRegularFormat";

        public string? DocNumberRegularFormat
        {
            get => this.Get<string>(DocNumberRegularFormatKey);
            set => this.Set(DocNumberRegularFormatKey, value);
        }

        private const string AllowManualRegularDocNumberAssignmentKey = "AllowManualRegularDocNumberAssignment";

        public bool AllowManualRegularDocNumberAssignment
        {
            get => this.Get<bool>(AllowManualRegularDocNumberAssignmentKey);
            set => this.Set(AllowManualRegularDocNumberAssignmentKey, BooleanBoxes.Box(value));
        }

        private const string DocNumberRegistrationAutoAssignmentIDKey = "DocNumberRegistrationAutoAssignmentID";

        public KrDocNumberRegistrationAutoAssignmentID DocNumberRegistrationAutoAssignmentID
        {
            get => (KrDocNumberRegistrationAutoAssignmentID) this.Get<int>(DocNumberRegistrationAutoAssignmentIDKey);
            set => this.Set(DocNumberRegistrationAutoAssignmentIDKey, Int32Boxes.Box((int) value));
        }

        private const string DocNumberRegistrationSequenceKey = "DocNumberRegistrationSequence";

        public string? DocNumberRegistrationSequence
        {
            get => this.Get<string>(DocNumberRegistrationSequenceKey);
            set => this.Set(DocNumberRegistrationSequenceKey, value);
        }

        private const string DocNumberRegistrationFormatKey = "DocNumberRegistrationFormat";

        public string? DocNumberRegistrationFormat
        {
            get => this.Get<string>(DocNumberRegistrationFormatKey);
            set => this.Set(DocNumberRegistrationFormatKey, value);
        }

        private const string AllowManualRegistrationDocNumberAssignmentKey =
            "AllowManualRegistrationDocNumberAssignment";

        public bool AllowManualRegistrationDocNumberAssignment
        {
            get => this.Get<bool>(AllowManualRegistrationDocNumberAssignmentKey);
            set => this.Set(AllowManualRegistrationDocNumberAssignmentKey, BooleanBoxes.Box(value));
        }

        private const string ReleaseRegistrationNumberOnFinalDeletionKey = "ReleaseRegistrationNumberOnFinalDeletion";

        public bool ReleaseRegistrationNumberOnFinalDeletion
        {
            get => this.Get<bool>(ReleaseRegistrationNumberOnFinalDeletionKey);
            set => this.Set(ReleaseRegistrationNumberOnFinalDeletionKey, BooleanBoxes.Box(value));
        }

        private const string ReleaseRegularNumberOnFinalDeletionKey = "ReleaseRegularNumberOnFinalDeletion";

        public bool ReleaseRegularNumberOnFinalDeletion
        {
            get => this.Get<bool>(ReleaseRegularNumberOnFinalDeletionKey);
            set => this.Set(ReleaseRegularNumberOnFinalDeletionKey, BooleanBoxes.Box(value));
        }

        private const string HideCreationButtonKey = "HideCreationButton";

        public bool HideCreationButton
        {
            get => this.Get<bool>(HideCreationButtonKey);
            set => this.Set(HideCreationButtonKey, BooleanBoxes.Box(value));
        }

        private const string HideRouteTabKey = "HideRouteTab";

        public bool HideRouteTab
        {
            get => this.Get<bool>(HideRouteTabKey);
            set => this.Set(HideRouteTabKey, BooleanBoxes.Box(value));
        }

        private const string UseForumKey = "UseForum";
        public bool UseForum
        {
            get => this.Get<bool>(UseForumKey);
            set => this.Set(UseForumKey, BooleanBoxes.Box(value));
        }

        private const string UseDefaultDiscussionTabKey = "UseDefaultDiscussionTab";
        public bool UseDefaultDiscussionTab
        {
            get => this.Get<bool>(UseDefaultDiscussionTabKey);
            set => this.Set(UseDefaultDiscussionTabKey, BooleanBoxes.Box(value));
        }

        private const string UseRoutesInWorkflowEngineKey = "UseRoutesInWorkflowEngine";

        /// <summary>
        /// Возвращает или задаёт значение, показывающее, используются ли маршруты в бизнес процессах или нет.
        /// </summary>
        public bool UseRoutesInWorkflowEngine
        {
            get => this.Get<bool>(UseRoutesInWorkflowEngineKey);
            set => this.Set(UseRoutesInWorkflowEngineKey, BooleanBoxes.Box(value));
        }

        #endregion
    }
}
