import { FieldType, Flags, StorageMap } from '@tessa/core';
import { extension, inject } from '@tessa/application';
import {
  CardMetadataColumnType,
  CardSection,
  CardTypeSchemeItem,
  ICardMetadataColumn,
  ICardMetadataRepository,
  ICardMetadataRepository$,
  ICardMetadataSection,
  IKrTypesCache,
  IKrTypesCache$,
  KrComponents,
  KrComponentsHelper
} from '@tessa/platform';
import { CardStoreExtension, ICardStoreExtensionContext } from 'tessa/cards/extensions';
import { runtimeCard } from '../krUIHelper';

/**
 * Расширение на сохранение карточки, содержащей маршрут.
 */
@extension({ name: 'KrCardStoreExtension' })
export class KrCardStoreExtension extends CardStoreExtension {
  //#region ctor

  constructor(
    @inject(IKrTypesCache$) private readonly _krTypesCache: IKrTypesCache,
    @inject(ICardMetadataRepository$) private readonly _metadataRepository: ICardMetadataRepository
  ) {
    super();
  }

  //#endregion

  //#region CardStoreExtension

  async beforeRequest(context: ICardStoreExtensionContext): Promise<void> {
    if (!context.validationResult.isSuccessful) {
      return;
    }

    const card = context.request.tryGetCard();
    if (!card) {
      return;
    }

    const info = card.tryGetInfo();
    if (info) {
      delete info['LocalTilesInfoMark'];
    }

    if (
      runtimeCard(card.typeId) &&
      Flags.hasNotFlag(
        await KrComponentsHelper.getKrComponentsByCard(card, this._krTypesCache),
        KrComponents.Routes
      )
    ) {
      // В карточке могут выполняться маршруты но они не добавлены
      return;
    }

    const stagesSection = card.sections.tryGet('KrStagesVirtual');
    if (!stagesSection || stagesSection.rows.length === 0) {
      return;
    }

    await this.visitSections(card.sections);

    for (const row of stagesSection.rows) {
      row.setChanged('DisplayTimeLimit', false);
      row.setChanged('DisplayParticipants', false);
      row.setChanged('DisplaySettings', false);
    }
  }

  //#endregion

  //#region methods

  private async visitSections(cardSections: StorageMap<CardSection>): Promise<void> {
    const generalMetadata = await this._metadataRepository.getCardMetadata();
    const cardTypeMetadata = await generalMetadata.getMetadataForType(
      '21bca3fc-f75f-413b-b5c8-49538cbfc761'
    ); // KrCardTypeID
    if (!cardTypeMetadata) {
      return;
    }

    const cardType = cardTypeMetadata.cardType;

    const schemeItems = new Map<string, CardTypeSchemeItem>();
    cardType.schemeItems.forEach(x => schemeItems.set(x.sectionId!, x));
    const topLevelSecMetadata = cardTypeMetadata.sections.getSectionByName('KrStagesVirtual');
    if (!topLevelSecMetadata) {
      return;
    }

    const stagesMapping = new Map<string, string>();
    const topLevelSection = cardSections.tryGet('KrStagesVirtual');
    if (!topLevelSection) {
      return;
    }

    for (const topLevelRow of topLevelSection.rows) {
      stagesMapping.set(topLevelRow.rowId, topLevelRow.rowId);
      topLevelRow.set('__ParentStageRowID', topLevelRow.rowId, FieldType.Guid);
    }

    let previousLayer = new Set<string>([topLevelSecMetadata.id!]);
    let currentLayer = new Set<string>();

    // Обход зависимостей проводится в "ширину"
    // Вершиной является переданная через параметр секция
    // В первый слой входят все секции, имеющие столбец с указанием на родителя KrStages
    // Вторым слоем будут все секции, у которых "ссылка на родителя" указывает на секции первого слоя
    // и т.д. до тех пор, пока очередной слой не станет пустым.
    while (previousLayer.size !== 0) {
      for (const secMetadata of cardTypeMetadata.sections) {
        // Секция не используется в карточке, а значит в обработке не участвует.
        const schemeItem = schemeItems.get(secMetadata.id!);
        if (!schemeItem) {
          continue;
        }

        // Получаем комплексный столбец с ссылкой на родителя.
        const refSecTuple = KrCardStoreExtension.getParentColumnSec(secMetadata, previousLayer);
        const parentComplexColumn = refSecTuple[0];
        const parentRowIdColumn = refSecTuple[1];
        if (!parentComplexColumn || !parentRowIdColumn) {
          continue;
        }

        // Комплексный столбец используется в карточке.
        if (!schemeItem.columnIdList.some(x => x === parentComplexColumn.id)) {
          continue;
        }

        const section = cardSections.tryGet(secMetadata.name);
        const rows = section?.tryGetRows();

        if (rows && rows.length > 0) {
          currentLayer.add(secMetadata.id!);
          // Проставляем каждой строке ссылку на этап, ориентируясь по
          // ссылке на непосредственного родителя.
          for (const row of rows) {
            const parentId = row.tryGet<string>(parentRowIdColumn.name!);
            let topLevelRowId: string | null = null;
            if (parentId && (topLevelRowId = stagesMapping.get(parentId)!)) {
              stagesMapping.set(row.rowId, topLevelRowId);
              row.set('__ParentStageRowID', topLevelRowId, FieldType.Guid);
            }
          }
        }
      }

      const layers = KrCardStoreExtension.swapLayers(previousLayer, currentLayer);
      previousLayer = layers[0];
      currentLayer = layers[1];
    }
  }

  private static getParentColumnSec(
    secMetadata: ICardMetadataSection,
    previousLayer: Set<string>
  ): [ICardMetadataColumn | null, ICardMetadataColumn | null] {
    let complex: ICardMetadataColumn | null = null;
    let rowId: ICardMetadataColumn | null = null;
    for (const column of secMetadata.columns) {
      if (
        column.parentRowSection &&
        column.columnType === CardMetadataColumnType.Complex &&
        previousLayer.has(column.parentRowSection.id!)
      ) {
        complex = column;
      } else if (
        complex &&
        column.columnType === CardMetadataColumnType.Physical &&
        column.parentRowSection &&
        complex.parentRowSection &&
        column.parentRowSection.id === complex.parentRowSection.id &&
        column.complexColumnIndex === complex.complexColumnIndex
      ) {
        rowId = column;
        break;
      }
    }

    return [complex, rowId];
  }

  private static swapLayers(
    previousLayer: Set<string>,
    currentLayer: Set<string>
  ): [Set<string>, Set<string>] {
    previousLayer.clear();
    return [currentLayer, previousLayer];
  }

  //#endregion
}
