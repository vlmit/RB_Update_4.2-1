import { IStorage } from '@tessa/core';
import {
  WorkflowActionStateStorage,
  WorkflowActionStorage,
  WorkflowActionWithSettingsBase,
  WorkflowHistoryMessageInfo
} from 'tessa/ui/workflow/chunk';
import { KrResolutionActionSettings } from './krResolutionActionSettings';

/** Объект, содержащий информацию по действию "Типовая задача". */
export class KrResolutionActionStorage extends WorkflowActionWithSettingsBase {
  //#region fields

  private static readonly _fieldToCaptionMap = new Map<
    keyof KrResolutionActionSettings,
    WorkflowHistoryMessageInfo
  >([
    ['performers', { controlCaption: '$CardTypes_Controls_Performers' }],
    ['author', { controlCaption: '$CardTypes_Controls_FromUserOrContextRole' }],
    ['kind', { controlCaption: '$CardTypes_Controls_Kind' }],
    ['digest', { controlCaption: '$CardTypes_Controls_TaskDescription' }],
    ['period', { controlCaption: '$CardTypes_Controls_DurationDays' }],
    ['planned', { controlCaption: '$CardTypes_Controls_Planned' }],
    ['isMajorPerformer', { controlCaption: '$CardTypes_Controls_MajorPerformer' }],
    ['isMassCreation', { controlCaption: '$CardTypes_Controls_SendMassCreation' }],
    ['withControl', { controlCaption: '$CardTypes_Controls_WithControl' }],
    ['controller', { controlCaption: '$CardTypes_Controls_Controller' }],
    ['sqlPerformersScript', { controlCaption: '$CardTypes_SQLPerformers' }],
    ['sender', { controlCaption: '$CardTypes_Controls_SenderUserOrContextRole' }]
  ]);

  //#endregion

  //#region base overrides

  override get fieldToCaptionMap(): Map<string, WorkflowHistoryMessageInfo> {
    return KrResolutionActionStorage._fieldToCaptionMap;
  }

  protected override settingsFactory(
    action: WorkflowActionStorage,
    actionState: WorkflowActionStateStorage | undefined,
    storage: IStorage
  ): KrResolutionActionSettings {
    return new KrResolutionActionSettings(action, actionState, storage);
  }

  //#endregion
}
