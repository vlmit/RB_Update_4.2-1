import { EntryPropertyDataSource, IPropertySettings } from 'tessa/ui/propertyGrid';

/** Источник данных для свойства "Число". */
export class NumberPropertyDataSource extends EntryPropertyDataSource<number> {}

/** Настройки для свойства "Число". */
export interface INumberPropertySettings extends IPropertySettings {
  /** Минимально допустимое значение. */
  readonly minValue: number;
  /** Максимально допустимое значение. */
  readonly maxValue: number;
}
