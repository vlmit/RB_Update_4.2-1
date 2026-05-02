import { IStorage } from '@tessa/core';
import {
  WorkflowActionStateStorage,
  WorkflowActionStorage,
  WorkflowActionWithSettingsBase,
  WorkflowHistoryMessageInfo
} from 'tessa/ui/workflow/chunk';
import { KrAmendingActionSettings } from './krAmendingActionSettings';

/** Объект, содержащий информацию по действию "Доработка". */
export class KrAmendingActionStorage extends WorkflowActionWithSettingsBase {
  //#region fields

  private static readonly _fieldToCaptionMap = new Map<
    keyof KrAmendingActionSettings,
    WorkflowHistoryMessageInfo
  >([
    ['role', { controlCaption: '$CardTypes_Controls_Role' }],
    ['author', { controlCaption: '$CardTypes_Controls_AuthorUserOrContextRole' }],
    ['digest', { controlCaption: '$CardTypes_Controls_TaskDescription' }],
    ['kind', { controlCaption: '$CardTypes_Controls_Kind' }],
    ['result', { controlCaption: '$CardTypes_Controls_Result' }],
    ['period', { controlCaption: '$CardTypes_Controls_DurationDays' }],
    ['planned', { controlCaption: '$CardTypes_Controls_Planned' }],
    ['isIncrementCycle', { controlCaption: '$CardTypes_Controls_AmendingAction_IncrementCycle' }],
    ['isChangeState', { controlCaption: '$CardTypes_Controls_AmendingAction_ChangeState' }],
    [
      'hasEditApprovalSchemeAccess',
      { controlCaption: '$CardTypes_Controls_AmendingAction_EditApprovalSchemeAccess' }
    ],
    ['initTaskScript', { controlCaption: '$CardTypes_Controls_TaskInitializationScenario' }],
    ['completeOptionTaskScript', { controlCaption: '$CardTypes_Controls_TaskCompletionScenario' }],
    ['notification', { controlCaption: '$CardTypes_Controls_Notification' }],
    ['excludeDeputies', { controlCaption: '$CardTypes_Controls_ExcludeDeputies' }],
    ['excludeSubscribers', { controlCaption: '$CardTypes_Controls_ExcludeSubscribers' }],
    ['notificationScript', { controlCaption: '$CardTypes_Controls_EmailModifyScenario' }],
    ['completeOptionNotification', { controlCaption: '$CardTypes_Controls_Notification' }],
    ['recipients', { controlCaption: '$CardTypes_Controls_Recipients' }],
    ['completeOptionSendToPerformer', { controlCaption: '$CardTypes_Controls_SendToPerformer' }],
    ['completeOptionSendToAuthor', { controlCaption: '$CardTypes_Controls_SendToAuthor' }],
    ['completeOptionExcludeDeputies', { controlCaption: '$CardTypes_Controls_ExcludeDeputies' }],
    [
      'completeOptionExcludeSubscribers',
      { controlCaption: '$CardTypes_Controls_ExcludeSubscribers' }
    ],
    [
      'completeOptionNotificationScript',
      { controlCaption: '$CardTypes_Controls_EmailModifyScenario' }
    ],
    ['events', { controlCaption: '$CardTypes_Controls_HandleEvents', isTable: true }]
  ]);

  //#endregion

  //#region base overrides

  override get fieldToCaptionMap(): Map<string, WorkflowHistoryMessageInfo> {
    return KrAmendingActionStorage._fieldToCaptionMap;
  }

  protected override settingsFactory(
    action: WorkflowActionStorage,
    actionState: WorkflowActionStateStorage | undefined,
    storage: IStorage
  ): KrAmendingActionSettings {
    return new KrAmendingActionSettings(action, actionState, storage);
  }

  //#endregion
}
