import { ExtensionRegistrator, ExtensionStage } from '@tessa/application';
import { DocumentMovementMapExtension } from './DocumentMovementMapExtension';
import './DocumentMovementMapExtension.css';

export const Registrator: ExtensionRegistrator = {
  async registerTypes() { },
  async registerExtensions(container) {
    container.registerExtension({
      extension: DocumentMovementMapExtension,
      stage: ExtensionStage.AfterPlatform
    });
  }
}
