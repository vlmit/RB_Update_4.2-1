import { IStorage, StorageAccessor, StorageHelper } from '@tessa/core';
import { extension, inject } from '@tessa/application';
import {
  IInitializationExtensionContext,
  IInitializationRepository,
  IInitializationRepository$,
  InitializationExtension
} from '@tessa/platform';
import { TileExtension, ITileGlobalExtensionContext, Tile, TileGroups } from 'tessa/ui/tiles';

/**
 * Позволяет получать и использовать данные из MetadataStorage.info,
 * добавленные в мету через ServerInitializationExtension.
 *
 * Результат работы расширения:
 * - на клиенте достает данные, содержащие информацию о тайлах, добавленную
 *   через ServerInitializationExtension (по ключу из меты и добавляет в MetadataStorage.info).
 * - после инициализации приложения обращается к MetadataStorage.info, получает информацию о тайлах
 *   и добавляет их в левую панель.
 */

const AdditionalTilesKey = 'AdditionalTiles';

@extension()
export class AdditionalMetaInitializationExtension extends InitializationExtension {
  constructor(
    @inject(IInitializationRepository$) private _initializationRepository: IInitializationRepository
  ) {
    super();
  }

  override async afterRequest(context: IInitializationExtensionContext): Promise<void> {
    // проверяем наличие info в контексте
    const info = context.response?.tryGetInfo();
    if (!info) {
      return;
    }

    // получаем информацию о тайлах, добавленную в мету через ServerInitializationExtension
    const additionalTiles = StorageHelper.tryGet<IStorage[]>(info, AdditionalTilesKey);
    if (!additionalTiles) {
      return;
    }

    // если информация о тайлах отсутствует в IInitializationRepository, то добавляем ее
    await this._initializationRepository.setIfNotExists(AdditionalTilesKey, additionalTiles);
  }
}

@extension()
export class AdditionalMetaTileExtension extends TileExtension {
  constructor(
    @inject(IInitializationRepository$) private _initializationRepository: IInitializationRepository
  ) {
    super();
  }

  override async initializingGlobal(context: ITileGlobalExtensionContext): Promise<void> {
    const additionalTiles =
      await this._initializationRepository.tryGet<IStorage[]>(AdditionalTilesKey);
    if (!additionalTiles) {
      return;
    }

    const leftPanel = context.workspace.leftPanel;

    for (const tileInfo of additionalTiles) {
      const sa = new StorageAccessor(tileInfo);
      // создаем тайлы из полученной информации и добавляем их в левую панель
      leftPanel.tiles.push(
        new Tile({
          name: sa.tryGetStringOrDefault('name'),
          caption: sa.tryGetStringOrDefault('caption'),
          icon: sa.tryGetStringOrDefault('icon'),
          contextSource: leftPanel.contextSource,
          group: TileGroups.CardsTop,
          order: 10
        })
      );
    }
  }
}
