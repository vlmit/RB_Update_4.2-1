import { Property } from 'tessa/ui/propertyGrid';
import { NumberPropertyDataSource, INumberPropertySettings } from './36_numberPropertyTypes';

/** Модель представления для свойства "Число". */
export class NumberProperty extends Property<NumberPropertyDataSource, INumberPropertySettings> {
  //#region constructors

  /**
   * Создаёт экземпляр класса {@link NumberProperty}.
   * @param dataSource Источник данных для свойства.
   * @param settings Настройки для свойства.
   */
  constructor(dataSource: NumberPropertyDataSource, settings: INumberPropertySettings) {
    super(dataSource, settings);

    this.minValue = settings.minValue;
    this.maxValue = settings.maxValue;
  }

  //#endregion

  //#region properties

  /** Значение свойства. */
  get value(): number {
    return this.dataSource.getValue();
  }
  set value(value: number) {
    this.dataSource.setValue(value);
  }

  /** Минимально допустимое значение. */
  readonly minValue: number;

  /** Максимально допустимое значение. */
  readonly maxValue: number;

  //#endregion
}
