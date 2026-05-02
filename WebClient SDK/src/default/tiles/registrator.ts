import { ExtensionRegistrator, ExtensionStage } from '@tessa/application';

import { AcquaintanceTileExtension } from './acquaintanceTileExtension';
import { CardImportCancelTileExtension } from './cardImportCancelTileExtension';
import { CardImportStartTileExtension } from './cardImportStartTileExtension';
import { CreateMultipleTemplateTileExtension } from './createMultipleTemplateTileExtension';
import { DocLoadPrintBarcodeTileExtension } from './docLoadPrintBarcodeTileExtension';
import { ExtendedDefaultTypeGroupCaptionsTileExtension } from './extendedDefaultTypeGroupCaptionsTileExtension';
import { FilterViewDialogOverrideTileExtension } from './filterViewDialogOverrideTileExtension';
import { HotkeyTilePanelExtension } from './hotkeyTilePanelExtension';
import { KrDocStateTileExtension } from './krDocStateTileExtension';
import { KrEditModeTileExtension } from './krEditModeTileExtension';
import { KrPermissionsTileExtension } from './krPermissionsTileExtension';
import { KrShowHiddenStagesTileExtension } from './krShowHiddenStagesTileExtension';
import { KrTilesExtension } from './krTilesExtension';
import { KrTypesAndCreateBasedOnTileExtension } from './krTypesAndCreateBasedOnTileExtension';
import { ProhibitTilesInViewsTileExtension } from './prohibitTilesInViewsTileExtension';
import { StageSourceBuildTileExtension } from './stageSourceBuildTileExtension';

export const TilesRegistrator: ExtensionRegistrator = {
  async registerTypes() {},
  async registerExtensions(container) {
    // Initialize
    container.registerExtension({
      extension: ExtendedDefaultTypeGroupCaptionsTileExtension,
      stage: ExtensionStage.Initialize,
      singleton: true,
      order: 1
    });

    container.registerExtension({
      extension: HotkeyTilePanelExtension,
      stage: ExtensionStage.Initialize,
      singleton: true,
      order: 2
    });

    // AfterPlatform
    container
      .registerExtension({
        extension: KrTilesExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true,
        order: 1
      })
      .registerExtension({
        extension: KrEditModeTileExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true,
        order: 2
      })
      .registerExtension({
        extension: KrShowHiddenStagesTileExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true,
        order: 3
      })
      .registerExtension({
        extension: AcquaintanceTileExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true,
        order: 4
      })
      .registerExtension({
        extension: StageSourceBuildTileExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true,
        order: 5
      })
      .registerExtension({
        extension: KrTypesAndCreateBasedOnTileExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true,
        order: 6
      })
      .registerExtension({
        extension: CreateMultipleTemplateTileExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true,
        order: 7
      })
      .registerExtension({
        extension: ProhibitTilesInViewsTileExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true,
        order: 8
      })
      .registerExtension({
        extension: KrDocStateTileExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true,
        order: 9
      })
      .registerExtension({
        extension: DocLoadPrintBarcodeTileExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true,
        order: 10
      })
      .registerExtension({
        extension: KrPermissionsTileExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true,
        order: 11
      })
      .registerExtension({
        extension: FilterViewDialogOverrideTileExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true,
        order: 12
      })
      .registerExtension({
        extension: CardImportStartTileExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true,
        order: 13
      })
      .registerExtension({
        extension: CardImportCancelTileExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true,
        order: 14
      });
  }
};
