import { StorageHelper } from '@tessa/core';
import { extension } from '@tessa/application';
import { KrTileInfo } from '@tessa/platform';
import { KrGlobalTileContainer } from '../workflow/krProcess/krGlobalTileContainer';
import { KrTileInflater } from '../workflow/krProcess/krTileInflater';
import {
  TileExtension,
  ITileGlobalExtensionContext,
  TileGroups,
  ITileLocalExtensionContext,
  HotkeyStorage
} from 'tessa/ui/tiles';

@extension({ name: 'KrTilesExtension' })
export class KrTilesExtension extends TileExtension {
  public async initializingGlobal(context: ITileGlobalExtensionContext): Promise<void> {
    const tileInfos = KrGlobalTileContainer.instance.getTileInfos();
    if (tileInfos.length > 0) {
      const panel = context.workspace.leftPanel;
      const tiles = KrTileInflater.instance.inflate(
        panel.contextSource,
        tileInfos,
        TileGroups.WorkflowGlobal
      );
      panel.tiles.push(...tiles);
    }
  }

  public initializingLocal(context: ITileLocalExtensionContext): void {
    const panel = context.workspace.leftPanel;
    for (const tile of panel.tiles) {
      const info = StorageHelper.tryGet<KrTileInfo>(tile.sharedInfo, 'TileInfo');
      // Вешаем хоткей если тайл глобальный
      if (info?.isGlobal) {
        const tileHotkey = HotkeyStorage.tryGetTileHotkey(tile, info?.buttonHotkey);
        if (tileHotkey) {
          tile.contextSource.hotkeyStorage.addTileHotkey(tileHotkey);
        }
      }
    }
  }
}
