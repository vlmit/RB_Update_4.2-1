import { ExtensionRegistrator, ExtensionStage } from '@tessa/application';
import { MEDOPreviewBtn } from './PreviewButton';
import { filePreviewControlUIExtension } from './filePreviewControlUIExtension';

export const Registrator: ExtensionRegistrator = {
  async registerTypes() { },
  async registerExtensions(container) {
    container.registerExtension({
      extension: MEDOPreviewBtn,
      stage: ExtensionStage.AfterPlatform
    }).registerExtension({
      extension: filePreviewControlUIExtension,
      stage: ExtensionStage.BeforePlatform
    });
  }
}
