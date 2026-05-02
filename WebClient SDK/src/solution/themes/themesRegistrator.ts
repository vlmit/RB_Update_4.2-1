import { ExtensionRegistrator, ExtensionStage } from '@tessa/application';
import { ThemesInitializationApplicationExtension } from './themesInitializationApplicationExtension';

export const ThemesRegistrator: ExtensionRegistrator = {
  async registerTypes() {},
  async registerExtensions(container) {
    container.registerExtension({
      extension: ThemesInitializationApplicationExtension,
      stage: ExtensionStage.Initialize,
      order: -1,
      singleton: true
    });
  }
};
