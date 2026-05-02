import { ExtensionRegistrator, ExtensionStage } from '@tessa/application';

import { KrDontSkipEditModeGetExtension } from './krDontSkipEditModeGetExtension';
import { KrKeepReadCardPermissionGetExtension } from './krKeepReadCardPermissionGetExtension';
import { KrKeepReadCardPermissionStoreExtension } from './krKeepReadCardPermissionStoreExtension';
import { KrTokenToTaskHistoryViewUIExtension } from './krTokenToTaskHistoryViewUIExtension';
import { KrTokenToTaskHistoryUIExtension } from './krTokenToTaskHistoryUIExtension';

export const KrPermissionsRegistrator: ExtensionRegistrator = {
  async registerTypes() {},
  async registerExtensions(container) {
    container
      .registerExtension({
        extension: KrDontSkipEditModeGetExtension,
        stage: ExtensionStage.BeforePlatform,
        order: 1,
        singleton: true
      })
      .registerExtension({
        extension: KrKeepReadCardPermissionGetExtension,
        stage: ExtensionStage.BeforePlatform,
        order: 2,
        singleton: true
      })
      .registerExtension({
        extension: KrKeepReadCardPermissionStoreExtension,
        stage: ExtensionStage.BeforePlatform,
        order: 3,
        singleton: true
      })
      .registerExtension({
        extension: KrTokenToTaskHistoryViewUIExtension,
        stage: ExtensionStage.BeforePlatform,
        order: 4,
        singleton: true
      })
      .registerExtension({
        extension: KrTokenToTaskHistoryUIExtension,
        stage: ExtensionStage.AfterPlatform,
        order: 5
      });
  }
};
