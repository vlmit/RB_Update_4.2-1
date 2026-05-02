import { FileControlUIExtension } from './FileControlExtension';
import { ExtensionRegistrator, ExtensionStage } from '@tessa/application';

export const Registrator: ExtensionRegistrator = {
  async registerTypes() { },
  async registerExtensions(container) {
    container.registerExtension({
      extension: FileControlUIExtension,
      stage: ExtensionStage.AfterPlatform
    });
  }
}
