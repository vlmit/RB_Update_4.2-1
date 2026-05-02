import { extension } from '@tessa/application';
import { Guid } from '@tessa/core';
import { CardHelper, ICardImportService, ICardImportService$ } from '@tessa/platform';
import { UIHost } from '@tessa/ui';
import { ImportCardsHelper } from 'tessa/ui/import';
import { ITileGlobalExtensionContext, Tile, TileExtension, TileGroups } from 'tessa/ui/tiles';

@extension({ name: 'CardImportCancelTileExtension' })
export class CardImportCancelTileExtension extends TileExtension {
  //#region ctor

  constructor(@ICardImportService$() private readonly _importService: ICardImportService) {
    super();
  }

  //#endregion

  //#region TileExtension

  public async initializingGlobal(context: ITileGlobalExtensionContext): Promise<void> {
    const contextSource = context.workspace.leftPanel.contextSource;

    context.workspace.leftPanel.tiles.push(
      new Tile({
        name: 'CancelImport',
        caption: '$UI_CardImport_CancelImport',
        icon: 'm-cross',
        contextSource,
        command: async tile => {
          const cardEditor = tile.context.cardEditor;
          const cardId = cardEditor?.cardModel?.card.id;
          if (!cardId) {
            return;
          }

          const result = await UIHost.showLoadingOverlay(() => this._importService.cancel(cardId), {
            delay: 300
          });
          await UIHost.showNotEmpty(result);

          if (result.isSuccessful) {
            await cardEditor.refreshCard(cardEditor.context);
          }
        },
        group: TileGroups.CardsTop,
        evaluating: e => {
          const editor = e.currentTile.context.cardEditor;

          e.setIsEnabledWithCollapsing(
            e.currentTile,
            !!editor?.cardModel &&
              Guid.equals(editor.cardModel.cardType.id, CardHelper.CardImportTypeID) &&
              ImportCardsHelper.isCancelable(editor.cardModel.card)
          );
        }
      })
    );
  }

  //#endregion
}
