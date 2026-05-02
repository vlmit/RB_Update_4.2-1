import { IStorage, Primitive, StorageHelper, TypedField } from '@tessa/core';
import { CardRow } from '@tessa/platform';
import {
  IComplexEntryPropertyDataProvider,
  IEntryPropertyDataProvider,
  IPropertyGridDataProvider
} from 'tessa/ui/propertyGrid';

/**
 * Объект для получения/установки типизированного значения в простые (одиночные) поля строки таблицы карточки,
 * используемый в провайдере данных для обозревателя свойств.
 */
export class EntryPropertyRowFieldsDataProvider<
  TValue = unknown
> implements IEntryPropertyDataProvider<TValue> {
  //#region constructors

  /**
   * Создаёт экземпляр класса {@link EntryPropertyFieldsDataProvider}.
   * @param _masterDataProvider Общий провайдер данных для свойств.
   * @param _row строка таблицы.
   * @param key Ключ, по которому значение хранится в полях секции карточки.
   * @param createFieldFunction Переопределение функции создания TypedField.
   */
  constructor(
    private readonly _masterDataProvider: IPropertyGridDataProvider,
    private readonly _row: CardRow,
    readonly key: string,
    readonly createFieldFunction?: (value: Primitive) => TypedField
  ) {}

  //#endregion

  //#region props

  get isChanged(): boolean {
    return this._masterDataProvider.isChanged(this.key) || this._row.isChanged(this.key);
  }

  //#endregion

  //#region base overrides

  getPropertyValue<TValue>(): TValue {
    return TypedField.tryGet(this._row.tryGetField(this.key)) as TValue;
  }

  setPropertyValue<TValue>(value: TValue): void {
    if (value == null) {
      this._row.set(this.key, null);
    } else if (StorageHelper.isPrimitiveType(value)) {
      this._row.set(
        this.key,
        this.createFieldFunction
          ? this.createFieldFunction(value)
          : TypedField.tryCreateByValueType(value)
      );
    } else {
      throw new Error('Unsupported value type.');
    }
    this._masterDataProvider.reportChanged(this.key, true);
  }

  dispose(): void {}

  //#endregion
}

/**
 * Объект для получения/установки типизированного значения в сложные (составные) поля строки таблицы карточки,
 * используемый в провайдере данных для обозревателя свойств.
 */
export class ComplexEntryRowPropertyFieldsDataProvider<
  TValue = unknown
> implements IComplexEntryPropertyDataProvider<TValue | null> {
  //#region constructors

  /**
   * Создаёт экземпляр класса {@link ComplexEntryPropertyFieldsDataProvider}.
   * @param _masterDataProvider Общий провайдер данных для свойств.
   * @param _row строка таблицы.
   * @param key Ключ-префикс, который используется для формирования имени полей секции карточки.
   * @param fields Коллекция имён полей, которые используются совместно с префиксом для формирования имени полей секции карточки.
   * Если значение не указано, то используется коллекция полей с именами `ID` и `Name`.
   */
  constructor(
    private readonly _masterDataProvider: IPropertyGridDataProvider,
    private readonly _row: CardRow,
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
      this._keys.some(key => this._row.isChanged(key))
    );
  }

  //#endregion

  //#region base overrides

  getPropertyValue<TValue>(): TValue | null {
    const result: IStorage = {};
    for (const key of this._keys) {
      const value = TypedField.tryGet(this._row.tryGetField(key));
      if (value) {
        result[key] = value;
      }
    }

    return Object.keys(result).length > 0 ? (result as TValue) : null;
  }

  setPropertyValue<TValue>(value: TValue): void {
    if (value == null || StorageHelper.isStorage(value)) {
      for (const key of this._keys) {
        this._row.set(key, this.getKeyValue(value, key));
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
