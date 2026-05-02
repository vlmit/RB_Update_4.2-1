import { IStorage } from '@tessa/core';
import { KrChangeStateActionSettings } from './krChangeStateActionSettings';
import {
  WorkflowActionStateStorage,
  WorkflowActionStorage,
  WorkflowActionWithSettingsBase,
  WorkflowHistoryMessageInfo
} from 'tessa/ui/workflow/chunk';

/** Объект, содержащий информацию по действию "Смена состояния". */
export class KrChangeStateActionStorage extends WorkflowActionWithSettingsBase {
  //#region fields

  private static readonly _fieldToCaptionMap = new Map<
    keyof KrChangeStateActionSettings,
    WorkflowHistoryMessageInfo
  >([['state', { controlCaption: '$UI_KrChangeState_State' }]]);

  //#endregion

  //#region base overrides

  override get fieldToCaptionMap(): Map<string, WorkflowHistoryMessageInfo> {
    return KrChangeStateActionStorage._fieldToCaptionMap;
  }

  protected override settingsFactory(
    action: WorkflowActionStorage,
    actionState: WorkflowActionStateStorage | undefined,
    storage: IStorage
  ): KrChangeStateActionSettings {
    return new KrChangeStateActionSettings(action, actionState, storage);
  }

  //#endregion
}
