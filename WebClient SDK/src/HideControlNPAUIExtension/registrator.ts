import { ExtensionRegistrator, ExtensionStage } from '@tessa/application';
import { HideControlNPAUIExtension } from './HideControlNPAUIExtension';
import { HideTableButton } from './HideTableButton';

export const Registrator: ExtensionRegistrator = {
  async registerTypes() { },
  async registerExtensions(container) {
    container.registerExtension({
      extension: HideControlNPAUIExtension,
      stage: ExtensionStage.AfterPlatform,
    }).registerExtension({
      extension: HideTableButton,
      stage: ExtensionStage.AfterPlatform,
    });
  }
}
