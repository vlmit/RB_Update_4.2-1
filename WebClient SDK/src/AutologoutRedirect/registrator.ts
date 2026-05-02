import { ExtensionRegistrator, ExtensionStage } from '@tessa/application';
import { AutoLogoutRedirect } from './AutologoutRedirect';

export const Registrator: ExtensionRegistrator = {
  async registerTypes() { },
  async registerExtensions(container) {
    container.registerExtension({
      extension: AutoLogoutRedirect,
      stage: ExtensionStage.AfterPlatform,
    });
  }
}
