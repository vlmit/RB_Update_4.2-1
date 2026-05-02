import { ExtensionRegistrator, ExtensionStage } from '@tessa/application';
import { ODIncomingStamp } from './ODIncomingStampFileExtension';
import { ODOutgoingStamp } from './ODOutgoingStampFileExtension';

export const Registrator: ExtensionRegistrator = {
  async registerTypes() { },
  async registerExtensions(container) {
    container.registerExtension({
      extension: ODIncomingStamp,
      stage: ExtensionStage.BeforePlatform
    }).registerExtension({
      extension: ODOutgoingStamp,
      stage: ExtensionStage.BeforePlatform
    });
  }
}
