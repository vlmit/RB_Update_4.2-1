import { ExtensionRegistrator, ExtensionStage } from '@tessa/application';

import {
  DeskiExtension,
  DeskiFileExtension,
  DeskiUIExtension,
  DeskiFileControlExtension,
  DeskiFileVersionExtension,
  DeskiViewFileControlExtension
} from './deskiExtension';
import { DeskiFileContentExtension } from './deskiFileContentExtension';
import { DeskiInvalidateFileContentExtension } from './deskiInvalidateFileContentExtension';

export const DeskiRegistrator: ExtensionRegistrator = {
  async registerExtensions(container) {
    container
      .registerExtension({
        extension: DeskiExtension,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: DeskiFileExtension,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: DeskiUIExtension,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: DeskiFileContentExtension,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: DeskiInvalidateFileContentExtension,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: DeskiFileControlExtension,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: DeskiViewFileControlExtension,
        stage: ExtensionStage.AfterPlatform,
        order: 100
      })
      .registerExtension({
        extension: DeskiFileVersionExtension,
        stage: ExtensionStage.AfterPlatform
      });
  }
};
