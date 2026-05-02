import { extension } from '@tessa/application';
import { KrGlobalTileContainer } from '../krGlobalTileContainer';
import { ApplicationExtension, IApplicationExtensionMetadataContext } from 'tessa';
import { KrTileInfo } from '@tessa/platform';
import { StorageHelper } from '@tessa/core';

@extension({ name: 'GlobalButtonsInitializationExtension' })
export class GlobalButtonsInitializationExtension extends ApplicationExtension {
  public async afterMetadataReceived(context: IApplicationExtensionMetadataContext): Promise<void> {
    if (context.response) {
      const tiles = StorageHelper.tryGet(context.response.info, 'GlobalTilesInfoMark') ?? [];
      const globalTiles: KrTileInfo[] = [];
      if (Array.isArray(tiles)) {
        for (const tile of tiles) {
          globalTiles.push(new KrTileInfo(tile));
        }
      } else {
        const names = Object.getOwnPropertyNames(tiles);
        for (const name of names) {
          globalTiles.push(new KrTileInfo(tiles[name]));
        }
      }

      KrGlobalTileContainer.instance.init(globalTiles);
    }
  }
}
