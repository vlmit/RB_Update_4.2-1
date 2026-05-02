import { Guid, Primitive } from '@tessa/core';
import { Card, CardRow } from '@tessa/platform';
import { CardSingletonCache } from 'tessa/cards/cardSingletonCache';
import { OcrPatternTypes } from '../misc/ocrTypes';
import { OcrSettingsTypeName } from './ocrConstants';

export class OcrSettings {
  //#region fields

  private static _instance: OcrSettings;
  public readonly isEnabled: boolean;
  public readonly mapping: Mapping;
  public readonly patterns: Map<OcrPatternTypes, RegExp[]>;

  //#endregion

  //#region properties

  public static get instance(): OcrSettings {
    return (OcrSettings._instance ??= new OcrSettings());
  }

  //#endregion

  //#region constructors

  private constructor() {
    const card = CardSingletonCache.instance.cards.get(OcrSettingsTypeName);
    if (card) {
      // Инициализация признака, показывающего, что функциональность OCR доступна
      this.isEnabled = card.sections.tryGet('OcrSettings')?.fields.tryGet('IsEnabled') ?? false;
      // Инициализация данных для маппинга полей
      this.mapping = new Mapping(card);
      // Инициализация паттернов для валидации значения поля
      this.patterns = new Map<OcrPatternTypes, RegExp[]>();
      const patterRows = [...card.sections.get('OcrSettingsPatterns').rows];
      for (const row of patterRows.sort(
        (a, b) => a.get<number>('Order')! - b.get<number>('Order')!
      )) {
        const patternType = row.get<OcrPatternTypes>('TypeID')!;
        const patterns = this.patterns.get(patternType) || [];
        patterns.push(new RegExp(row.get<string>('Value')!.replaceAll(/\n/g, ''), 'i'));
        this.patterns.set(patternType, patterns);
      }
    } else {
      throw new Error(`Can not find singleton card with name '${OcrSettingsTypeName}' at cache.`);
    }
  }

  //#endregion
}

class Item {
  protected _row: CardRow;
  protected _prefix: string;

  public get id(): string {
    return this._row.get(`${this._prefix}ID`)!;
  }

  public get name(): string {
    return this._row.get(`${this._prefix}Name`)!;
  }

  constructor(row: CardRow, prefix: string) {
    this._row = row;
    this._prefix = prefix;
  }

  public get(key: string): Primitive | null {
    return this._row.get(key);
  }
}

class Collection<T extends Item> {
  private readonly _elements: ReadonlyArray<T>;

  constructor(
    ctor: new (card: Card, row: CardRow, sectionPrefix: string) => T,
    card: Card,
    sectionPrefix: string,
    parentRowId?: string
  ) {
    const rows = card.sections.get(`OcrMappingSettings${sectionPrefix}s`).rows;

    if (parentRowId) {
      this._elements = rows
        .filter(r => Guid.equals(r.parentRowId, parentRowId))
        .map(row => new ctor(card, row, sectionPrefix))
        .sort((a, b) => a.name.localeCompare(b.name));
    } else {
      this._elements = rows.map(row => new ctor(card, row, sectionPrefix));
    }
  }

  public getById(id: string): T | undefined {
    return this._elements.find(e => Guid.equals(e.id, id));
  }

  public getByName(name: string): T | undefined {
    return this._elements.find(e => e.name.localeCompare(name) === 0);
  }

  [Symbol.iterator](): ArrayIterator<T> {
    return this._elements[Symbol.iterator]();
  }
}

class Field extends Item {
  constructor(_card: Card, row: CardRow) {
    super(row, 'Field');
  }
}

class Section extends Item {
  public readonly fields: Collection<Field>;

  constructor(card: Card, row: CardRow) {
    super(row, 'Section');
    this.fields = new Collection(Field, card, 'Field', row.rowId);
  }
}

class Type extends Item {
  public readonly sections: Collection<Section>;

  constructor(card: Card, row: CardRow) {
    super(row, 'Type');
    this.sections = new Collection(Section, card, 'Section', row.rowId);
  }
}

class Mapping {
  public readonly types: Collection<Type>;

  constructor(card: Card) {
    this.types = new Collection(Type, card, 'Type');
  }
}
