import { runInAction } from 'mobx';
import { Guid, IStorage, Primitive, StorageHelper, TypedField } from '@tessa/core';
import { CardRow, CardRowState, CardSection } from '@tessa/platform';
import {
  IComplexEntryPropertyDataProvider,
  IEntryPropertyDataProvider,
  IPropertyGridDataProvider,
  ITablePropertyDataProvider,
  ITablePropertyRowMetadata
} from 'tessa/ui/propertyGrid';
import { ICardModel } from 'tessa/ui/cards';
import { ArgumentOutOfRangeError } from 'tessa/platform/errors';

/**
 * Объект для получения/установки типизированного значения в простые (одиночные) поля карточки,
 * используемый в провайдере данных для обозревателя свойств.
 */
export class EntryPropertyFieldsDataProvider<
  TValue = unknown
> implements IEntryPropertyDataProvider<TValue> {
  //#region constructors

  /**
   * Создаёт экземпляр класса {@link EntryPropertyFieldsDataProvider}.
   * @param _masterDataProvider Общий провайдер данных для свойств.
   * @param _section Секция карточки.
   * @param key Ключ, по которому значение хранится в полях секции карточки.
   */
  constructor(
    private readonly _masterDataProvider: IPropertyGridDataProvider,
    private readonly _section: CardSection,
    readonly key: string
  ) {}

  //#endregion

  //#region props

  get isChanged(): boolean {
    return this._masterDataProvider.isChanged(this.key) || this._section.isChanged(this.key);
  }

  //#endregion

  //#region base overrides

  getPropertyValue<TValue>(): TValue {
    return TypedField.tryGet(this._section.fields.tryGetField(this.key)) as TValue;
  }

  setPropertyValue<TValue>(value: TValue): void {
    if (value == null) {
      this._section.fields.set(this.key, null);
    } else if (StorageHelper.isPrimitiveType(value)) {
      this._section.fields.set(this.key, TypedField.tryCreateByValueType(value));
    } else {
      throw new Error('Unsupported value type.');
    }
    this._masterDataProvider.reportChanged(this.key, true);
  }

  dispose(): void {}

  //#endregion
}

/**
 * Объект для получения/установки типизированного значения в сложные (составные) поля карточки,
 * используемый в провайдере данных для обозревателя свойств.
 */
export class ComplexEntryPropertyFieldsDataProvider<
  TValue = unknown
> implements IComplexEntryPropertyDataProvider<TValue> {
  //#region constructors

  /**
   * Создаёт экземпляр класса {@link ComplexEntryPropertyFieldsDataProvider}.
   * @param _masterDataProvider Общий провайдер данных для свойств.
   * @param _section Секция карточки.
   * @param key Ключ-префикс, который используется для формирования имени полей секции карточки.
   * @param fields Коллекция имён полей, которые используются совместно с префиксом для формирования имени полей секции карточки.
   * Если значение не указано, то используется коллекция полей с именами `ID` и `Name`.
   */
  constructor(
    private readonly _masterDataProvider: IPropertyGridDataProvider,
    private readonly _section: CardSection,
    readonly key: string,
    ...fields: ReadonlyArray<string>
  ) {
    this._keys =
      fields.length > 0 ? fields.map(name => `${key}${name}`) : [`${key}ID`, `${key}Name`];
  }

  //#endregion

  //#region fields

  private readonly _keys: ReadonlyArray<string>;

  //#endregion

  //#region props

  get isChanged(): boolean {
    return (
      this._masterDataProvider.isChanged(this.key) ||
      this._keys.some(key => this._section.isChanged(key))
    );
  }

  //#endregion

  //#region base overrides

  getPropertyValue<TValue>(): TValue {
    const result: IStorage = {};
    for (const key of this._keys) {
      result[key] = TypedField.tryGet(this._section.fields.tryGetField(key));
    }

    return result as TValue;
  }

  setPropertyValue<TValue>(value: TValue): void {
    if (value == null || StorageHelper.isStorage(value)) {
      for (const key of this._keys) {
        this._section.fields.set(key, this.getKeyValue(value, key));
      }
    } else {
      throw new Error('Unsupported value type.');
    }
    this._masterDataProvider.reportChanged(this.key, true);
  }

  dispose(): void {}

  private getKeyValue(value: unknown, key: string): TypedField | null {
    if (value == null) {
      return null;
    }

    const keyValue = value[key];
    if (keyValue == null) {
      return null;
    }

    return TypedField.isTypedFieldContainer(keyValue)
      ? keyValue
      : TypedField.tryCreateByValueType(keyValue as Primitive);
  }

  //#endregion
}

/**
 * Объект для получения/установки типизированного значения в коллекционные поля карточки,
 * используемый в провайдере данных для обозревателя свойств.
 */
export class TablePropertyFieldsDataProvider implements ITablePropertyDataProvider<CardRow> {
  //#region ctor

  /**
   * Создаёт экземпляр класса {@link TablePropertyFieldsDataProvider}.
   * @param _masterDataProvider Общий провайдер данных для свойств.
   * @param _cardModel Модель карточки.
   * @param _section Секция карточки.
   * @param key Ключ, по которому значение хранится в полях секции карточки.
   */
  constructor(
    private readonly _masterDataProvider: IPropertyGridDataProvider,
    private readonly _cardModel: ICardModel,
    private readonly _section: CardSection,
    readonly key: string
  ) {}

  //#endregion

  //#region props

  get isChanged(): boolean {
    return this._masterDataProvider.isChanged(this.key) || this._section.hasChanges();
  }

  //#endregion

  //#region methods

  createDefault(): CardRow[] {
    return [];
  }

  getPropertyRows(): CardRow[] {
    return this._section.rows;
  }

  clearPropertyRows(): boolean {
    const rows = this._section.rows;
    if (rows.length === 0) {
      return false;
    }

    runInAction(() => {
      for (let index = rows.length - 1; index >= 0; index--) {
        const row = rows[index];
        this._cardModel.binder.removeRow(this._section.name, row);
      }
      this._masterDataProvider.reportChanged(this.key, true);
    });

    return true;
  }

  deletePropertyRow(index: number): void {
    const rows = this._section.rows;

    if (index < 0 || index >= rows.length) {
      throw new ArgumentOutOfRangeError('index', index);
    }

    runInAction(() => {
      const row = rows[index];
      this._cardModel.binder.removeRow(this._section.name, row);
      this._masterDataProvider.reportChanged(this.key, true);
    });
  }

  createPropertyRow(): CardRow {
    return new CardRow();
  }

  addPropertyRow(rowData: unknown, index?: number): void {
    const rows = this._section.rows;

    if (index != null && (index < 0 || index >= rows.length)) {
      throw new ArgumentOutOfRangeError('index', index);
    }

    runInAction(() => {
      const row = rowData instanceof CardRow ? rowData : new CardRow(rowData as IStorage);
      if (row.rowId === Guid.empty) {
        row.rowId = Guid.newGuid();
      }
      row.state = CardRowState.Inserted;

      if (index == null) {
        rows.push(row);
      } else {
        rows.splice(index, 0, row);
      }

      this._masterDataProvider.reportChanged(this.key, true);
    });
  }

  setPropertyRowField<TValue = unknown>(row: CardRow, field: string, value: TValue): void {
    runInAction(() => {
      row.set(field, value as Primitive);
      this._masterDataProvider.reportChanged(this.key, true);
    });
  }

  getPropertyRowField<TValue = unknown>(row: CardRow, field: string): TValue {
    return row.get(field) as TValue;
  }

  getPropertyRowMetadata(_row: CardRow): ITablePropertyRowMetadata | null {
    return null;
  }

  dispose(): void {}
}
