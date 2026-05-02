import { ExtensionRegistrator, ExtensionStage } from '@tessa/application';

import { HideEmptyIncomingReferencesControl } from './hideEmptyIncomingReferencesControl';

export const DocumentsRegistrator: ExtensionRegistrator = {
  async registerTypes() {},
  async registerExtensions(container) {
    container.registerExtension({
      extension: HideEmptyIncomingReferencesControl,
      stage: ExtensionStage.AfterPlatform,
      singleton: true
    });
  }
};
