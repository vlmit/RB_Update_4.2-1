import { ISession, ISession$, extension, inject, localize } from '@tessa/application';
import { TileNames } from 'tessa/platform/tileNames';
import { Tile } from 'tessa/ui/tiles/tile';
import { ITile } from 'tessa/ui/tiles/interfaces';
import { TileExtension } from 'tessa/ui/tiles/tileExtension';
import { TileEvaluationEventArgs } from 'tessa/ui/tiles/tileEvaluationEventArgs';
import { ITileGlobalExtensionContext } from 'tessa/ui/tiles/tileGlobalExtensionContext';
import { IFormDialogManager$ } from 'tessa/ui/formEditor/injects';
import { IFormDialogManager } from 'tessa/ui/formEditor/types';
import { ApiAccessTokensHelper } from './apiAccessTokensHelper';

@extension({ name: 'ApiAccessTokensTileExtension' })
export class ApiAccessTokensTileExtension extends TileExtension {
  //#region constructors

  constructor(
    @inject(ISession$) private readonly _session: ISession,
    @inject(IFormDialogManager$) private readonly _dialogManager: IFormDialogManager
  ) {
    super();
  }

  //#endregion

  //#region base overrides

  override async initializingGlobal(context: ITileGlobalExtensionContext): Promise<void> {
    const leftPanel = context.workspace.leftPanel;
    const settingsTile = leftPanel.tryGetTile(TileNames.SettingsCard);

    if (settingsTile) {
      const caption = localize('$UI_Tiles_ApiAccessTokens');
      settingsTile.tiles.push(
        new Tile({
          caption,
          name: 'ApiAccessTokens',
          contextSource: leftPanel.contextSource,
          order: this.calculateTileOrder(settingsTile, caption),
          evaluating: this.evaluateTileVisibility,
          command: this.generateTileCommand
        })
      );
    }
  }

  //#endregion

  //#region private methods

  private calculateTileOrder = (settingsTile: ITile, caption: string): number => {
    return settingsTile.tiles.reduce(
      (order, tile) =>
        order < tile.order && caption.localeCompare(localize(tile.caption)) > 0
          ? tile.order
          : order,
      Number.MIN_SAFE_INTEGER
    );
  };

  private evaluateTileVisibility = (e: TileEvaluationEventArgs): void => {
    e.setIsEnabledWithCollapsing(e.currentTile, this._session.user.isAdmin);
  };

  private generateTileCommand = (_tile: ITile): Promise<void> => {
    return ApiAccessTokensHelper.openLayoutDialog(
      this._dialogManager,
      ApiAccessTokensHelper.ApiAccessTokensViewLayout,
      { autoSizeWidth: false, autoSizeHeight: false }
    );
  };

  //#endregion
}
