import { ExtensionRegistrator, ExtensionStage } from '@tessa/application';
import { whenCardTypeIdIs } from '@tessa/platform';
import { ComponentsRegistry } from '@tessa/ui';
import { FilterViewDialogDescriptorRegistry } from '../views/filterViewDialogDescriptorRegistry';
import { AbExternalFileExtension } from './abExternalFileExtension';
import { AbExternalFilesFileControlExtension } from './abExternalFilesFileControlExtension';
import { AbSettingsTileExtension } from './abSettingsTileExtension';
import { AbTestProcessTileExtension } from './abTestProcessTileExtension';
import { AbCarUIExtension } from './abCarUIExtension';
import { AbPdfAnnotationsCarFileExtension } from './abPdfAnnotationsCarFileExtension';
import {
  AbCustomFolderViewExtension,
  AbCustomFolderViewModel,
  AbCustomViewContentComponent
} from './abCustomFolderExtension';
import { AbFilterViewDialogDescriptors } from './abFilterViewDialogDescriptors';
import { AbTreeViewItemExtension } from './abTreeViewItemExtension';
import { AbTestServiceClient, AbTestServiceClient$ } from './abTestServiceClient';
import { AbCarFileControlExtension } from './abCarFileControlExtension';

const AbCarCardTypeID = 'd0006e40-a342-4797-8d77-6501c4b7c4ac';

export const AbTestRegistrator: ExtensionRegistrator = {
  async registerTypes(container) {
    container.bind(AbTestServiceClient$).to(AbTestServiceClient);
    ComponentsRegistry.instance.register(AbCustomFolderViewModel, AbCustomViewContentComponent);
  },
  async registerExtensions(container) {
    // AfterPlatform
    container
      .registerExtension({
        extension: AbCustomFolderViewExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true
      })
      .registerExtension({
        extension: AbTreeViewItemExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true
      })
      .registerExtension({
        extension: AbExternalFileExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true
      })
      .registerExtension({
        extension: AbExternalFilesFileControlExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true
      })
      .registerExtension({
        extension: AbTestProcessTileExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true,
        order: 51
      })
      .registerExtension({
        extension: AbSettingsTileExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true,
        order: 52
      })
      .registerExtension({
        extension: AbCarUIExtension,
        stage: ExtensionStage.AfterPlatform,
        when: whenCardTypeIdIs(AbCarCardTypeID)
      })
      .registerExtension({
        extension: AbPdfAnnotationsCarFileExtension,
        stage: ExtensionStage.AfterPlatform,
        order: 55
      })
      .registerExtension({
        extension: AbCarFileControlExtension,
        stage: ExtensionStage.AfterPlatform,
        order: 55
      });

    FilterViewDialogDescriptorRegistry.instance.register(
      // идентификатор узла дерева в рабочем месте:
      '193496fb-e9a1-49a1-b9f4-8dbf73d56bd4',
      AbFilterViewDialogDescriptors.cars
    );
  }
};
