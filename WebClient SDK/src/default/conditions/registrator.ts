import { ExtensionRegistrator, ExtensionStage } from '@tessa/application';
import { ConditionTypesClientInitializationExtension } from './conditionTypesClientInitializationExtension';

export const ConditionsRegistrator: ExtensionRegistrator = {
  async registerExtensions(container) {
    container.registerExtension({
      extension: ConditionTypesClientInitializationExtension,
      stage: ExtensionStage.Platform,
      singleton: true,
      order: 1
    });
  }
};
