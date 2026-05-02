import { IStorage } from '@tessa/core';
import {
  WorkflowActionStateStorage,
  WorkflowActionStorage,
  WorkflowActionWithSettingsBase,
  WorkflowHistoryMessageInfo
} from 'tessa/ui/workflow/chunk';
import { KrApprovalActionSettings } from './krApprovalActionSettings';
import { KrAdditionalApproversSettings } from './krAdditionalApproversSettings';

/** Объект, содержащий информацию по действию "Согласование". */
export class KrApprovalActionStorage extends WorkflowActionWithSettingsBase {
  //#region fields

  private static readonly _fieldToCaptionMap = new Map<
    keyof KrApprovalActionSettings | keyof KrAdditionalApproversSettings,
    WorkflowHistoryMessageInfo
  >([
    ['performers', { controlCaption: '$CardTypes_Controls_Approvers' }],
    ['additionalApprovers', { controlCaption: '$CardTypes_Controls_PerformersForCurrentApprover' }],
    ['isResponsible', { controlCaption: '$CardTypes_Controls_FirstIsResponsible' }],
    ['author', { controlCaption: '$CardTypes_Controls_AuthorUserOrContextRole' }],
    ['digest', { controlCaption: '$CardTypes_Controls_TaskDescription' }],
    ['kind', { controlCaption: '$CardTypes_Controls_Kind' }],
    ['result', { controlCaption: '$CardTypes_Controls_Result' }],
    ['period', { controlCaption: '$CardTypes_Controls_DurationDays' }],
    ['planned', { controlCaption: '$CardTypes_Controls_Planned' }],
    ['isParallel', { controlCaption: '$CardTypes_Columns_Controls_ApprovalAction_IsParallel' }],
    ['isAdvisory', { controlCaption: '$UI_KrApproval_Advisory' }],
    [
      'isDisableAutoApproval',
      { controlCaption: '$CardTypes_Columns_Controls_DisableAutoApproval' }
    ],
    ['returnWhenApproved', { controlCaption: '$CardTypes_Columns_Controls_ReturnAfterApproval' }],
    ['expectAllApprovers', { controlCaption: '$CardTypes_Controls_ExpectAllApprovers' }],
    ['changeStateOnStart', { controlCaption: '$UI_KrApproval_ChangeStateOnStart' }],
    ['changeStateOnEnd', { controlCaption: '$UI_KrApproval_ChangeStateOnEnd' }],
    [
      'notCreateReturnEditTaskHistoryRecord',
      { controlCaption: '$CardTypes_Columns_Controls_NotCreateReturnEditTaskHistoryRecord' }
    ],
    ['canEditCard', { controlCaption: '$CardTypes_Columns_Controls_EditCard' }],
    ['canEditAnyFiles', { controlCaption: '$CardTypes_Columns_Controls_EditAnyFiles' }],
    ['sqlPerformersScript', { controlCaption: '$CardTypes_SQLPerformers' }],
    ['initTaskScript', { controlCaption: '$CardTypes_Controls_TaskInitializationScenario' }],
    ['notification', { controlCaption: '$CardTypes_Controls_Notification' }],
    ['excludeDeputies', { controlCaption: '$CardTypes_Controls_ExcludeDeputies' }],
    ['excludeSubscribers', { controlCaption: '$CardTypes_Controls_ExcludeSubscribers' }],
    ['notificationScript', { controlCaption: '$CardTypes_Controls_EmailModifyScenario' }],
    ['editInterjectRole', { controlCaption: '$CardTypes_Controls_Role' }],
    ['editInterjectAuthor', { controlCaption: '$CardTypes_Controls_AuthorUserOrContextRole' }],
    ['editInterjectKind', { controlCaption: '$CardTypes_Controls_Kind' }],
    ['editInterjectDigest', { controlCaption: '$CardTypes_Controls_TaskDescription' }],
    ['editInterjectPeriod', { controlCaption: '$CardTypes_Controls_DurationDays' }],
    ['editInterjectPlanned', { controlCaption: '$CardTypes_Controls_Planned' }],
    [
      'editInterjectInitTaskScript',
      { controlCaption: '$CardTypes_Controls_TaskInitializationScenario' }
    ],
    ['editInterjectNotification', { controlCaption: '$CardTypes_Controls_Notification' }],
    ['editInterjectExcludeDeputies', { controlCaption: '$CardTypes_Controls_ExcludeDeputies' }],
    [
      'editInterjectExcludeSubscribers',
      { controlCaption: '$CardTypes_Controls_ExcludeSubscribers' }
    ],
    [
      'editInterjectNotificationScript',
      { controlCaption: '$CardTypes_Controls_EmailModifyScenario' }
    ],
    [
      'additionalApprovalInitTaskScript',
      { controlCaption: '$CardTypes_Controls_TaskInitializationScenario' }
    ],
    ['additionalApprovalNotification', { controlCaption: '$CardTypes_Controls_Notification' }],
    [
      'additionalApprovalExcludeDeputies',
      { controlCaption: '$CardTypes_Controls_ExcludeDeputies' }
    ],
    [
      'additionalApprovalExcludeSubscribers',
      { controlCaption: '$CardTypes_Controls_ExcludeSubscribers' }
    ],
    [
      'additionalApprovalNotificationScript',
      { controlCaption: '$CardTypes_Controls_EmailModifyScenario' }
    ],
    [
      'requestCommentInitTaskScript',
      { controlCaption: '$CardTypes_Controls_TaskInitializationScenario' }
    ],
    ['requestCommentNotification', { controlCaption: '$CardTypes_Controls_Notification' }],
    ['requestCommentExcludeDeputies', { controlCaption: '$CardTypes_Controls_ExcludeDeputies' }],
    [
      'requestCommentExcludeSubscribers',
      { controlCaption: '$CardTypes_Controls_ExcludeSubscribers' }
    ],
    [
      'requestCommentNotificationScript',
      { controlCaption: '$CardTypes_Controls_EmailModifyScenario' }
    ],
    ['completeOptions', { controlCaption: '$CardTypes_Controls_CompletionOptions', isTable: true }],
    [
      'actionCompleteOptions',
      { controlCaption: '$CardTypes_Controls_CompletionOptionsAction', isTable: true }
    ],
    ['events', { controlCaption: '$CardTypes_Controls_HandleEvents', isTable: true }]
  ]);

  //#endregion

  //#region base overrides

  override get fieldToCaptionMap(): Map<string, WorkflowHistoryMessageInfo> {
    return KrApprovalActionStorage._fieldToCaptionMap;
  }

  protected override settingsFactory(
    action: WorkflowActionStorage,
    actionState: WorkflowActionStateStorage | undefined,
    storage: IStorage
  ): KrApprovalActionSettings {
    return new KrApprovalActionSettings(action, actionState, storage);
  }

  //#endregion
}
