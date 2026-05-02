import { runInAction } from 'mobx';
import { KrTileInflater } from '../../workflow/krProcess/krTileInflater';
import { KrLocalTileCommand } from '../../workflow/krProcess/krLocalTileCommand';
import {
  ButtonToolbarActionGroup,
  ButtonToolbarVisibilityMode,
  CardToolbarActionGroup,
  CardToolbarItem,
  CardUIExtension,
  ICardUIExtensionContext,
  KrButtonToolbarAction,
  KrButtonToolbarActionGroup
} from 'tessa/ui/cards';
import { ITilePanel, Tile, TileGroups, ITile, HotkeyStorage } from 'tessa/ui/tiles';
import { IStorage } from 'tessa/platform/storage';
import { extension } from '@tessa/application';
import { Guid, StorageHelper } from '@tessa/core';
import { KrTileInfo } from '@tessa/platform';
import { TileNames } from 'tessa/platform';
import { IUIContext } from 'tessa/ui';

@extension({ name: 'KrTilesUIExtension' })
export class KrTilesUIExtension extends CardUIExtension {
  public contextInitialized(context: ICardUIExtensionContext): void {
    const card = context.card;
    let tilesPanel: ITilePanel;
    if (
      !card ||
      context.model.inSpecialMode ||
      !context.uiContext.tiles ||
      !(tilesPanel = context.uiContext.tiles.leftPanel)
    ) {
      return;
    }

    const tilesStorage = StorageHelper.tryGet<IStorage>(card.info, 'LocalTilesInfoMark');

    if (!tilesStorage) {
      // Даже если кнопок Вторичных процессов нет, необходимо это установить и для тулбара.
      KrTilesUIExtension.setSecondaryProcessToolbarButtons([], context);

      // А также очищаем предыдущий список тайлов
      runInAction(() => {
        KrTilesUIExtension.removeTilesWithTileInfo(tilesPanel.tiles);

        const groupTile = tilesPanel.tryGetTile('ActionsGrouping');
        if (groupTile) {
          KrTilesUIExtension.removeTilesWithTileInfo(groupTile.tiles);
        }
      });

      return;
    }

    const localTiles: KrTileInfo[] = [];
    if (tilesStorage) {
      if (Array.isArray(tilesStorage)) {
        for (const tile of tilesStorage) {
          localTiles.push(new KrTileInfo(tile));
        }
      } else {
        const names = Object.getOwnPropertyNames(tilesStorage);
        for (const name of names) {
          localTiles.push(new KrTileInfo(tilesStorage[name]));
        }
      }
    }

    // Установить кнопки Вторичных процессов для тулбара.
    const tilesForToolbar: KrTileInfo[] = localTiles.map(x => new KrTileInfo(x.getStorage()));
    KrTilesUIExtension.setSecondaryProcessToolbarButtons(tilesForToolbar, context);

    // т.к. в этот момент tilesPanel.tiles уже observable, то нужно завернуть в runInAction
    runInAction(() => {
      KrTilesUIExtension.removeTilesWithTileInfo(tilesPanel.tiles);

      const tiles = KrTileInflater.instance.inflate(
        tilesPanel.contextSource,
        localTiles,
        TileGroups.Workflow
      );

      const actionGroupingTiles = tiles
        .filter(x => {
          const info = StorageHelper.tryGet<KrTileInfo>(x.sharedInfo, 'TileInfo');
          return info && info.actionGrouping;
        })
        .map(x => x.clone());

      let groupTile = tilesPanel.tryGetTile('ActionsGrouping');
      if (groupTile) {
        KrTilesUIExtension.removeTilesWithTileInfo(groupTile.tiles);
      }

      if (actionGroupingTiles.length > 0) {
        if (!groupTile) {
          groupTile = new Tile({
            name: 'ActionsGrouping',
            caption: '$UI_Tiles_ActionsGrouping',
            icon: 'ta icon-thin-258',
            contextSource: tilesPanel.contextSource,
            group: TileGroups.CardsTop,
            order: 10
          });
          tilesPanel.tiles.push(groupTile);
        }

        groupTile.info['MinActionsGroupingCount'] = 1;
        groupTile.tiles.push(...actionGroupingTiles);
      }

      KrTilesUIExtension.setHotkeys(tiles);

      // TODO: может быть сюда надо пихать тайлы с фильтром !actionGrouping
      tilesPanel.tiles.push(...tiles);
      // TODO: удалять тайлы из карточки как сделано в WorkflowTilesUIExtension?
      // delete card.info['LocalTilesInfoMark'];
    });
  }

  private static setSecondaryProcessToolbarButtons(
    tileInfos: KrTileInfo[],
    context: ICardUIExtensionContext
  ) {
    const toolbar = context.toolbar;
    if (!toolbar) {
      return;
    }

    const buttons = toolbar.items.filter(
      x =>
        (x instanceof KrButtonToolbarAction || x instanceof KrButtonToolbarActionGroup) &&
        // Не удалять группу "Действия", т.к. в ней могут быть не только кнопки вторичных процессов.
        x.name !== TileNames.ActionsGrouping
    );

    if (buttons?.length) {
      for (const bpButton of buttons) {
        toolbar.removeItem(bpButton);
      }
    }

    // Если группа "Действия" уже есть, и она является ButtonToolbarActionGroup, очистить Kr-кнопки внутри.
    const existedActionGroup = toolbar.items.find(x => x.name === TileNames.ActionsGrouping);
    if (
      existedActionGroup instanceof ButtonToolbarActionGroup &&
      existedActionGroup.actions.length > 0
    ) {
      const buttons = existedActionGroup.actions.filter(
        x => x instanceof KrButtonToolbarAction || x instanceof KrButtonToolbarActionGroup
      );

      if (buttons?.length) {
        for (const bpButton of buttons) {
          existedActionGroup.removeAction(bpButton);
        }
      }

      // Если после очистки ничего не осталось, удалить группу ActionsGrouping.
      if (existedActionGroup.actions.length === 0) {
        toolbar.removeItem(existedActionGroup);
      }
    }

    // После предварительных действий с тулбаром, если тайлов не было, значит кнопок для добавления нет.
    if (tileInfos.length === 0) {
      return;
    }

    const actionGroupingTiles: KrTileInfo[] = [];
    for (const tileInfo of tileInfos) {
      // Тайлы для группы "Действия".
      if (
        tileInfo?.actionGrouping &&
        !tileInfo.hidden &&
        tileInfo.toolbarVisibilityMode !== ButtonToolbarVisibilityMode.DoNotShow
      ) {
        actionGroupingTiles.push(tileInfo);
        continue;
      }

      const action = KrTilesUIExtension.tryGetToolbarActionFromTile(tileInfo, context.uiContext);
      if (action) {
        toolbar.addItem(action);
      }
    }

    // Тайлы в группе "Действия".
    if (actionGroupingTiles.length > 0) {
      const actionGroupToolbarItems: CardToolbarItem[] = [];

      for (const tileInfo of actionGroupingTiles) {
        // Т.к. верхнее меню тулбара сейчас не поддерживает иерархии, необходимо расположить кнопки плоским списком.
        const flattenTiles = KrTilesUIExtension.flatTile(tileInfo);

        for (const flattenTileInfo of flattenTiles) {
          const action = KrTilesUIExtension.tryGetToolbarActionFromTile(
            flattenTileInfo,
            context.uiContext
          );

          if (action) {
            actionGroupToolbarItems.push(action);
          }
        }
      }

      // Подменить группу "Действия" чтобы использовался ButtonToolbarActionGroup вместо CardToolbarActionGroup или создать новую.
      const oldActionGroup = toolbar.items.find(
        x => x.name === TileNames.ActionsGrouping && x instanceof CardToolbarActionGroup
      );

      if (oldActionGroup) {
        const oldActions = (oldActionGroup as CardToolbarActionGroup).actions as CardToolbarItem[];
        for (const oldAction of oldActions) {
          oldAction.detach();
        }

        actionGroupToolbarItems.push(...oldActions);
        toolbar.removeItem(oldActionGroup);
      }

      const newActionGroup = new ButtonToolbarActionGroup(
        TileNames.ActionsGrouping,
        '$UI_Tiles_ActionsGrouping',
        ButtonToolbarVisibilityMode.Show,
        'Thin258',
        null,
        actionGroupToolbarItems,
        0
      );

      toolbar.addItem(newActionGroup);
    }
  }

  private static removeTilesWithTileInfo(tiles: ITile[]) {
    const removeIndices: number[] = [];
    tiles.forEach((tile, i) => {
      const info = StorageHelper.tryGet<KrTileInfo>(tile.sharedInfo, 'TileInfo');
      // Не удаляем если тайл глобальный или он не маршрутный
      if (info?.isGlobal === false) {
        removeIndices.push(i);
        this.removeHotkey(tile);
      }
    });
    removeIndices.reverse();

    for (const i of removeIndices) {
      tiles.splice(i, 1);
    }
  }

  private static removeHotkey(tile: ITile) {
    tile.contextSource.hotkeyStorage.removeHotkey(tile);
    for (const childTile of tile.tiles) {
      this.removeHotkey(childTile);
    }
  }

  private static setHotkeys(tiles: ITile[]) {
    for (const tile of tiles) {
      this.setHotkeys(tile.tiles);

      const info = StorageHelper.tryGet<KrTileInfo>(tile.sharedInfo, 'TileInfo');
      // Не вешаем хоткей если тайл глобальный или он не маршрутный
      if (info?.isGlobal === false) {
        const tileHotkey = HotkeyStorage.tryGetTileHotkey(tile, info?.buttonHotkey);
        if (tileHotkey) {
          tile.contextSource.hotkeyStorage.addTileHotkey(tileHotkey);
        }
      }
    }
  }

  private static tryGetToolbarActionFromTile(
    tileInfo: KrTileInfo,
    uiContext: IUIContext
  ): CardToolbarItem | null {
    if (
      !tileInfo ||
      tileInfo.toolbarVisibilityMode === ButtonToolbarVisibilityMode.DoNotShow ||
      tileInfo.hidden
    ) {
      return null;
    }

    if (tileInfo.nestedTiles.length > 0) {
      const innerItems: CardToolbarItem[] = [];

      for (const nestedTileInfo of tileInfo.nestedTiles) {
        const toolbarItem = KrTilesUIExtension.tryGetToolbarActionFromTile(
          nestedTileInfo,
          uiContext
        );

        if (toolbarItem) {
          innerItems.push(toolbarItem);
        }
      }

      return new KrButtonToolbarActionGroup(
        Guid.newGuid(), // У tileInfo для kr-групп нет имени (см. GlobalButtonsInitializationExtension), поэтому здесь генерируем новый Guid, т.к. UI тулбара использует имя как идентификатор.
        tileInfo.caption,
        tileInfo.toolbarVisibilityMode,
        tileInfo.icon,
        tileInfo.tooltip,
        innerItems.sort((x, y) => x.order - y.order),
        tileInfo.order
      );
    } else {
      return new KrButtonToolbarAction(tileInfo, uiContext, this.localButtonOnClickCommand);
    }
  }

  private static async localButtonOnClickCommand(button: KrButtonToolbarAction): Promise<void> {
    if (button && button.uiContext && button.tileInfo) {
      const command = KrLocalTileCommand.instance;
      await command.onClickAction(button.uiContext, null, button.tileInfo as KrTileInfo);
    }
  }

  private static flatTile(tileInfo: KrTileInfo): KrTileInfo[] {
    if (tileInfo.nestedTiles.length === 0) {
      return [tileInfo];
    }

    const result: KrTileInfo[] = [];

    for (const nestedTile of tileInfo.nestedTiles) {
      const flattenNestedTiles = KrTilesUIExtension.flatTile(nestedTile);
      for (const flattenNestedTile of flattenNestedTiles) {
        result.push(flattenNestedTile);
      }
    }

    return result;
  }
}
