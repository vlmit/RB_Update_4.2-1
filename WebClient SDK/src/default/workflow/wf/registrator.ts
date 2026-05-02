import { ExtensionRegistrator, ExtensionStage } from '@tessa/application';

import { WfCardUIExtension } from './wfCardUIExtension';
import { WfTasksClientGetExtension } from './wfTasksClientGetExtension';
import { WfTaskSatelliteUIExtension } from './wfTaskSatelliteUIExtension';
import { WfTypeSettingsUIExtension } from './wfTypeSettingsUIExtension';
import { WfTileExtension } from './wfTileExtension';
import { WfTaskSatelliteClientGetFileContentExtension } from './wfTaskSatelliteClientGetFileContentExtension';
import { WfTaskHistoryViewUIExtension } from './wfTaskHistoryViewUIExtension';

export const WFRegistrator: ExtensionRegistrator = {
  async registerTypes() {},
  async registerExtensions(container) {
    container
      .registerExtension({
        extension: WfCardUIExtension,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: WfTasksClientGetExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true
      })
      .registerExtension({
        extension: WfTaskSatelliteUIExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true
      })
      .registerExtension({
        extension: WfTypeSettingsUIExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true
      })
      .registerExtension({
        extension: WfTileExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true
      })
      .registerExtension({
        extension: WfTaskSatelliteClientGetFileContentExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true
      })
      .registerExtension({
        extension: WfTaskHistoryViewUIExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true
      });
  }
};
