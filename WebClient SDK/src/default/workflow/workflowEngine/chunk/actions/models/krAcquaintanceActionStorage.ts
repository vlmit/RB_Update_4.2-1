import { IStorage } from '@tessa/core';
import { KrAcquaintanceActionSettings } from './krAcquaintanceActionSettings';
import {
  WorkflowActionStateStorage,
  WorkflowActionStorage,
  WorkflowActionWithSettingsBase,
  WorkflowHistoryMessageInfo
} from 'tessa/ui/workflow/chunk';

/** Объект, содержащий информацию по действию "Ознакомление". */
export class KrAcquaintanceActionStorage extends WorkflowActionWithSettingsBase {
  //#region fields

  private static readonly _fieldToCaptionMap = new Map<
    keyof KrAcquaintanceActionSettings,
    WorkflowHistoryMessageInfo
  >([
    ['recipients', { controlCaption: '$CardTypes_Controls_Recipients' }],
    ['sender', { controlCaption: '$CardTypes_Controls_SenderUserOrContextRole' }],
    ['comment', { controlCaption: '$CardTypes_Controls_Comment' }],
    ['aliasMetadata', { controlCaption: '$CardTypes_Blocks_Controls_PlaceholderAliases' }],
    ['notification', { controlCaption: '$CardTypes_Controls_Notification' }],
    ['excludeDeputies', { controlCaption: '$CardTypes_Controls_ExcludeDeputies' }]
  ]);

  //#endregion

  //#region base overrides

  override get fieldToCaptionMap(): Map<string, WorkflowHistoryMessageInfo> {
    return KrAcquaintanceActionStorage._fieldToCaptionMap;
  }

  protected override settingsFactory(
    action: WorkflowActionStorage,
    actionState: WorkflowActionStateStorage | undefined,
    storage: IStorage
  ): KrAcquaintanceActionSettings {
    return new KrAcquaintanceActionSettings(action, actionState, storage);
  }

  //#endregion
}
