import { IStorage } from '@tessa/core';
import { formatDecimal } from 'tessa/platform/formatting/formattingHelper';
import { OcrNumberValidator } from './ocrNumberValidator';
import { OcrPatternTypes } from '../misc/ocrTypes';
import { OcrValidator } from './ocrValidator';

/** Валидатор значения вещественного числа с плавающей запятой. */
export class OcrDoubleValidator extends OcrNumberValidator {
  //#region constructors

  /**
   * Создает экземпляр класса {@link OcrDoubleValidator}.
   * @param settings Настройки типа проверяемого контрола.
   */
  constructor(settings: IStorage) {
    super(settings);
  }

  //#endregion

  //#region base overrides

  protected get patterns(): ReadonlyArray<RegExp> {
    return OcrValidator.getPatterns(OcrPatternTypes.Double);
  }

  protected get error(): string {
    return '$UI_Controls_Double_InvalidValue';
  }

  protected compileValue(match: RegExpExecArray): string {
    if (match.groups) {
      const sign = match.groups['sign'] ?? '';
      const integer = match.groups['integer'] ?? '';
      const fractional = match.groups['fractional'] ?? '';
      if (!!fractional) {
        return integer ? `${sign}${integer}.${fractional}` : `${sign}0.${fractional}`;
      } else {
        return integer ? `${sign}${integer}` : '';
      }
    } else {
      return match[0];
    }
  }

  protected formatNumber(value: number): string {
    return formatDecimal(value, false);
  }

  //#endregion
}
