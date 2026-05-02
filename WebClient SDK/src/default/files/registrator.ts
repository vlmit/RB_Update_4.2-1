import { ExtensionRegistrator, ExtensionStage } from '@tessa/application';

import { ClientKrPermissionsGetFileContentExtension } from './clientKrPermissionsGetFileContentExtension';
import { ClientKrPermissionsGetFileVersionsExtension } from './clientKrPermissionsGetFileVersionsExtension';
import { WfCardFileExtension } from './wfCardFileExtension';
import { KrAddCycleGroupingFileControlExtension } from './krAddCycleGroupingFileControlExtension';
import { KrCurrentCycleFileControlExtension } from './krCurrentCycleFileControlExtension';
import { ClientFileTemplatePermissionsGetFileContentExtension } from './clientFileTemplatePermissionsGetFileContentExtension';
import { RestoreFileControlExtension } from './restoreFileControlExtension';

export const FilesRegistrator: ExtensionRegistrator = {
  async registerTypes() {},
  async registerExtensions(container) {
    container
      .registerExtension({
        extension: KrAddCycleGroupingFileControlExtension,
        stage: ExtensionStage.BeforePlatform
      })
      .registerExtension({
        extension: ClientFileTemplatePermissionsGetFileContentExtension,
        stage: ExtensionStage.BeforePlatform,
        singleton: true
      })
      .registerExtension({
        extension: ClientKrPermissionsGetFileContentExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true
      })
      .registerExtension({
        extension: ClientKrPermissionsGetFileVersionsExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true
      })
      .registerExtension({
        extension: WfCardFileExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true
      })
      .registerExtension({
        extension: KrCurrentCycleFileControlExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true
      })
      .registerExtension({
        extension: RestoreFileControlExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true
      });
  }
};
