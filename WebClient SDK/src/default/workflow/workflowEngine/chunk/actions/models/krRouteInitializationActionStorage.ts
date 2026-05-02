import { IStorage } from '@tessa/core';
import {
  WorkflowActionStateStorage,
  WorkflowActionStorage,
  WorkflowActionWithSettingsBase,
  WorkflowHistoryMessageInfo
} from 'tessa/ui/workflow/chunk';
import { KrRouteInitializationActionSettings } from './krRouteInitializationActionSettings';

/** Объект, содержащий информацию по действию "Инициализация маршрута". */
export class KrRouteInitializationActionStorage extends WorkflowActionWithSettingsBase {
  //#region fields

  private static readonly _fieldToCaptionMap = new Map<
    keyof KrRouteInitializationActionSettings,
    WorkflowHistoryMessageInfo
  >([
    ['initiator', { controlCaption: '$CardTypes_Controls_TheInitiatorOfTheApproval' }],
    ['initiatorComment', { controlCaption: '$CardTypes_Controls_CommentToApprovalCycle' }]
  ]);

  //#endregion

  //#region base overrides

  override get fieldToCaptionMap(): Map<string, WorkflowHistoryMessageInfo> {
    return KrRouteInitializationActionStorage._fieldToCaptionMap;
  }

  protected override settingsFactory(
    action: WorkflowActionStorage,
    actionState: WorkflowActionStateStorage | undefined,
    storage: IStorage
  ): KrRouteInitializationActionSettings {
    return new KrRouteInitializationActionSettings(action, actionState, storage);
  }

  //#endregion
}
