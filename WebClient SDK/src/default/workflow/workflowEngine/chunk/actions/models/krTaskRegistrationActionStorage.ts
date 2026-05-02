import { IStorage } from '@tessa/core';
import {
  WorkflowActionStateStorage,
  WorkflowActionStorage,
  WorkflowActionWithSettingsBase,
  WorkflowHistoryMessageInfo
} from 'tessa/ui/workflow/chunk';
import { KrTaskRegistrationActionSettings } from './krTaskRegistrationActionSettings';

/** Объект, содержащий информацию по действию "Задание регистрации". */
export class KrTaskRegistrationActionStorage extends WorkflowActionWithSettingsBase {
  //#region fields

  private static readonly _fieldToCaptionMap = new Map<
    keyof KrTaskRegistrationActionSettings,
    WorkflowHistoryMessageInfo
  >([
    ['performer', { controlCaption: '$CardTypes_Controls_Role' }],
    ['author', { controlCaption: '$CardTypes_Controls_AuthorUserOrContextRole' }],
    ['digest', { controlCaption: '$CardTypes_Controls_TaskDescription' }],
    ['kind', { controlCaption: '$CardTypes_Controls_Kind' }],
    ['result', { controlCaption: '$CardTypes_Controls_Result' }],
    ['period', { controlCaption: '$CardTypes_Controls_DurationDays' }],
    ['planned', { controlCaption: '$CardTypes_Controls_Planned' }],
    ['canEditCard', { controlCaption: '$CardTypes_Columns_Controls_EditCard' }],
    ['canEditAnyFiles', { controlCaption: '$CardTypes_Columns_Controls_EditAnyFiles' }],
    ['initTaskScript', { controlCaption: '$CardTypes_Controls_TaskInitializationScenario' }],
    ['notification', { controlCaption: '$CardTypes_Controls_Notification' }],
    ['excludeDeputies', { controlCaption: '$CardTypes_Controls_ExcludeDeputies' }],
    ['excludeSubscribers', { controlCaption: '$CardTypes_Controls_ExcludeSubscribers' }],
    ['notificationScript', { controlCaption: '$CardTypes_Controls_EmailModifyScenario' }],
    ['completeOptions', { controlCaption: '$CardTypes_Controls_CompletionOptions', isTable: true }],
    ['events', { controlCaption: '$CardTypes_Controls_HandleEvents', isTable: true }]
  ]);

  //#endregion

  //#region base overrides

  override get fieldToCaptionMap(): Map<string, WorkflowHistoryMessageInfo> {
    return KrTaskRegistrationActionStorage._fieldToCaptionMap;
  }

  protected override settingsFactory(
    action: WorkflowActionStorage,
    actionState: WorkflowActionStateStorage | undefined,
    storage: IStorage
  ): KrTaskRegistrationActionSettings {
    return new KrTaskRegistrationActionSettings(action, actionState, storage);
  }

  //#endregion
}
