import { IStorage } from '@tessa/core';
import {
  WorkflowActionStateStorage,
  WorkflowActionStorage,
  WorkflowActionWithSettingsBase,
  WorkflowHistoryMessageInfo
} from 'tessa/ui/workflow/chunk';
import { KrUniversalTaskActionSettings } from './krUniversalTaskActionSettings';

/** Объект, содержащий информацию по действию "Настраиваемое задание". */
export class KrUniversalTaskActionStorage extends WorkflowActionWithSettingsBase {
  //#region fields

  private static readonly _fieldToCaptionMap = new Map<
    keyof KrUniversalTaskActionSettings,
    WorkflowHistoryMessageInfo
  >([
    ['functionRoles', { controlCaption: '$CardTypes_Controls_TaskFunctionRoles', isTable: true }],
    ['digest', { controlCaption: '$CardTypes_Controls_TaskDescription' }],
    ['kind', { controlCaption: '$CardTypes_Controls_Kind' }],
    ['result', { controlCaption: '$CardTypes_Controls_Result' }],
    ['period', { controlCaption: '$CardTypes_Controls_DurationDays' }],
    ['planned', { controlCaption: '$CardTypes_Controls_Planned' }],
    ['canEditCard', { controlCaption: '$CardTypes_Columns_Controls_EditCard' }],
    ['canEditAnyFiles', { controlCaption: '$CardTypes_Columns_Controls_EditAnyFiles' }],
    ['initTaskScript', { controlCaption: '$CardTypes_Controls_TaskInitializationScenario' }],
    [
      'taskNotifications',
      { controlCaption: '$CardTypes_Blocks_Controls_TaskNotifications', isTable: true }
    ],
    ['completeOptions', { controlCaption: '$CardTypes_Controls_CompletionOptions', isTable: true }],
    ['events', { controlCaption: '$CardTypes_Controls_HandleEvents', isTable: true }]
  ]);

  //#endregion

  //#region base overrides

  override get fieldToCaptionMap(): Map<string, WorkflowHistoryMessageInfo> {
    return KrUniversalTaskActionStorage._fieldToCaptionMap;
  }

  protected override settingsFactory(
    action: WorkflowActionStorage,
    actionState: WorkflowActionStateStorage | undefined,
    storage: IStorage
  ): KrUniversalTaskActionSettings {
    return new KrUniversalTaskActionSettings(action, actionState, storage);
  }

  //#endregion
}
