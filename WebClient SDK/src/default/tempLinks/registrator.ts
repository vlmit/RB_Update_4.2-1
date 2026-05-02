import { ExtensionRegistrator, ExtensionStage } from '@tessa/application';
import { TempLinkFileExtension } from './tempLinkFileExtension';
import { TempLinkFileVersionExtension } from './tempLinkFileVersionExtension';
import { TempLinkTileExtension } from './tempLinkTileExtension';

export const TempLinksRegistrator: ExtensionRegistrator = {
  async registerTypes() {},
  async registerExtensions(container) {
    container
      .registerExtension({
        extension: TempLinkFileExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true
      })
      .registerExtension({
        extension: TempLinkFileVersionExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true
      })
      .registerExtension({
        extension: TempLinkTileExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true
      });
  }
};
