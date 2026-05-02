import { extension } from '@tessa/application';
import { Guid, IStorage, StorageSerializable, TypedJsonConverter } from '@tessa/core';
import {
  CardHelper,
  CardImportInfo,
  ICardImportService,
  ICardImportService$,
  ICardService,
  ICardService$
} from '@tessa/platform';
import { ImportCardsHelper } from 'tessa/ui/import';
import { ITileGlobalExtensionContext, Tile, TileExtension, TileGroups } from 'tessa/ui/tiles';

@extension({ name: 'CardImportStartTileExtension' })
export class CardImportStartTileExtension extends TileExtension {
  //#region ctor

  constructor(
    @ICardImportService$() private readonly _importService: ICardImportService,
    @ICardService$() private readonly _cardService: ICardService
  ) {
    super();
  }

  //#endregion

  //#region TileExtension

  public async initializingGlobal(context: ITileGlobalExtensionContext): Promise<void> {
    const contextSource = context.workspace.leftPanel.contextSource;

    context.workspace.leftPanel.tiles.push(
      new Tile({
        name: 'StartImport',
        caption: '$UI_CardImport_Import',
        icon: 'ta icon-thin-120',
        contextSource,
        command: async tile => {
          const editor = tile.context.cardEditor;
          const card = editor?.cardModel?.card;
          const cardsInfoString = card?.sections.get('CardImports').fields.getString('CardsInfo');
          if (!card || !cardsInfoString) {
            return;
          }

          const importingCardsInfo = TypedJsonConverter.deserializeList(cardsInfoString).map(
            (s: IStorage) => StorageSerializable.deserialize(CardImportInfo, s)
          );

          if (
            await ImportCardsHelper.showImportDialog(this._cardService, this._importService, {
              importSettings: {
                cardId: card.id,
                importingCardsInfo
              }
            })
          ) {
            await editor.refreshCard(editor.context);
          }
        },
        group: TileGroups.CardsTop,
        evaluating: e => {
          const editor = e.currentTile.context.cardEditor;

          e.setIsEnabledWithCollapsing(
            e.currentTile,
            !!editor &&
              !!editor.cardModel &&
              Guid.equals(editor.cardModel.cardType.id, CardHelper.CardImportTypeID) &&
              ImportCardsHelper.isStartable(editor.cardModel.card)
          );
        }
      })
    );
  }

  //#endregion
}
