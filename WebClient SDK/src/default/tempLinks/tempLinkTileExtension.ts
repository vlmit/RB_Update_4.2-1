import { extension } from '@tessa/application';
import {
  SchemeType,
  ViewCriteriaOperators,
  ViewRequestParameterBuilder,
  ViewExtensionMetadata,
  ViewParameterMetadata
} from '@tessa/platform';
import { getTessaIcon } from 'common/utility/uiHelpers';
import { CardEditorTileExtension } from 'tessa/defaultExtensions/platform/tiles/cardEditorTileExtension';
import { TileGroups } from 'tessa/ui/tiles';
import { Tile } from 'tessa/ui/tiles/tile';
import { TileExtension } from 'tessa/ui/tiles/tileExtension';
import { ITileLocalExtensionContext } from 'tessa/ui/tiles/tileLocalExtensionContext';
import { UIContext } from 'tessa/ui/uiContext';
import { showView } from 'tessa/ui/uiHost';

@extension({ name: 'TempLinkTileExtension' })
export class TempLinkTileExtension extends TileExtension {
  //#region TileExtension

  public async initializingLocal(context: ITileLocalExtensionContext): Promise<void> {
    const panel = context.workspace.leftPanel;
    const contextSource = panel.contextSource;

    const tempLinksTile = new Tile({
      name: 'TempFileLinks',
      caption: '$UI_Tiles_CardFileDownloadingTokens',
      toolTip: '$UI_Tiles_CardFileDownloadingTokens_Tooltip',
      icon: getTessaIcon('Thin117'),
      contextSource,
      command: TempLinkTileExtension.cardTempFileLinksAction,
      group: TileGroups.CardsBottom,
      evaluating: CardEditorTileExtension.enableOnUpdateCard
    });

    panel.tiles.push(tempLinksTile);
  }

  //#endregion

  //#region Private Methods

  private static cardTempFileLinksAction() {
    const context = UIContext.current;
    const cardEditor = context.cardEditor;
    if (!cardEditor || !cardEditor.cardModel || cardEditor.operationInProgress) {
      return;
    }

    const cardId = cardEditor.cardModel.card.id;

    const parameterMetadata = new ViewParameterMetadata();
    parameterMetadata.alias = 'CardID';
    parameterMetadata.caption = 'CardID';
    parameterMetadata.hidden = true;
    parameterMetadata.schemeType = SchemeType.Guid;
    parameterMetadata.multiple = false;

    const parameters = [
      new ViewRequestParameterBuilder()
        .withMetadata(parameterMetadata)
        .addCriteria(ViewCriteriaOperators.EqualsTo, cardId, cardId)
        .asRequestParameter()
    ];

    showView({
      viewAlias: 'FileLoadingTokensForCard',
      displayValue: '$Workplaces_Tokens_FileAccessTokens',
      parameters,
      extensions: [
        new ViewExtensionMetadata(
          'Tessa.Extensions.Default.Client.Views.AccessTokensContextMenuExtension'
        )
      ]
    });
  }

  //#endregion
}
