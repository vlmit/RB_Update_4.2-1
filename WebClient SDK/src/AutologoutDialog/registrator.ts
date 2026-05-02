import { ExtensionRegistrator, ExtensionStage } from '@tessa/application';
import { AutoLogoutDialog, LogExtension } from './AutoLogout';

export const Registrator: ExtensionRegistrator = {
  async registerTypes() { },
  async registerExtensions(container) {
    container.registerExtension({
      extension: AutoLogoutDialog,
      stage: ExtensionStage.AfterPlatform,
    }).registerExtension({
      extension: LogExtension,
      stage: ExtensionStage.AfterPlatform,
    });
  }
}
