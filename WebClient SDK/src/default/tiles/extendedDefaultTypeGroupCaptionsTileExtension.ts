import { TileExtension, ITileGlobalExtensionContext, ITileWorkspace } from 'tessa/ui/tiles';
import { tryGetFromInfo } from 'tessa/ui';
import { IStorage } from 'tessa/platform/storage';
import { extension } from '@tessa/application';

@extension()
export class ExtendedDefaultTypeGroupCaptionsTileExtension extends TileExtension {
  public async initializingGlobal(context: ITileGlobalExtensionContext): Promise<void> {
    ExtendedDefaultTypeGroupCaptionsTileExtension.setTypeGroupCaption(
      context.workspace,
      'Routes',
      '$KrTiles_Routes'
    );
  }

  private static setTypeGroupCaption(workspace: ITileWorkspace, group: string, caption: string) {
    let captions = tryGetFromInfo<IStorage | null>(workspace.info, 'TypeGroupCaption', null);
    if (!captions) {
      captions = {};
      workspace.info['TypeGroupCaption'] = captions;
    }

    captions[group] = caption;
  }
}
