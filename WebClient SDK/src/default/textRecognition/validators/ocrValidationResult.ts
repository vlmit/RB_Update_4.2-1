import { ValidationResult } from '@tessa/core';

/** Информация о результате проверки значения. */
export class OcrValidationResult {
  //#region fields

  /** Проверенное значение, преобразованное в формат, поддерживаемый системой. */
  readonly compiledValue: string | null;

  /** Проверенное значение, преобразованное в отображаемый формат. */
  readonly formattedValue: string | null;

  /** Результат проверки значения. */
  readonly validationResult: ValidationResult | null;

  //#endregion

  //#region constructors

  /**
   * Создает экземпляр класса {@link OcrValidationResult}.
   * @param validationResult Результат проверки значения.
   * @param compiledValue
   * Проверенное значение, преобразованное в формат, поддерживаемый системой.
   * Если параметр не задан, то значение по умолчанию - `null`.
   * @param formattedValue
   * Проверенное значение, преобразованное в отображаемый формат.
   * Если параметр не задан, то значение по умолчанию - `null`.
   */
  constructor(
    validationResult: ValidationResult | null,
    compiledValue: string | null = null,
    formattedValue: string | null = null
  ) {
    this.validationResult = validationResult;
    this.compiledValue = compiledValue;
    this.formattedValue = formattedValue;
  }

  //#endregion
}
