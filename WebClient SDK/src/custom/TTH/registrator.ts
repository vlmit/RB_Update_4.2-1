import { ExtensionRegistrator, ExtensionStage } from '@tessa/application';
import { TaskTreeHierarchy } from './TaskTreeHierarchy';
import './TaskTreeHierarchy.css';

export const Registrator: ExtensionRegistrator = {
  async registerTypes() { },
  async registerExtensions(container) {
    container.registerExtension({
      extension: TaskTreeHierarchy,
      stage: ExtensionStage.AfterPlatform
    });
  }
}
