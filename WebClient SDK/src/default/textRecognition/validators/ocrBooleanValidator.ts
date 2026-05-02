import { FormattingHelper, ValidationResult, ValidationResultType } from '@tessa/core';
import { localize } from '@tessa/application';
import { OcrPatternTypes } from '../misc/ocrTypes';
import { OcrValidator } from './ocrValidator';
import { OcrValidationResult } from './ocrValidationResult';

/** Валидатор булева значения. */
export class OcrBooleanValidator extends OcrValidator {
  //#region base overrides

  protected get patterns(): ReadonlyArray<RegExp> {
    return OcrValidator.getPatterns(OcrPatternTypes.Boolean);
  }

  protected get error(): string {
    return '$UI_Controls_Boolean_InvalidValue';
  }

  protected compileValue(match: RegExpExecArray): string {
    if (match.groups) {
      const positive = match.groups['positive'] ?? '';
      const negative = match.groups['negative'] ?? '';
      return positive ? 'true' : negative ? 'false' : '';
    } else {
      return match[0];
    }
  }

  //#endregion

  //#region public methods

  validate(value: string): OcrValidationResult {
    for (const pattern of this.patterns) {
      const match = pattern.exec(value);
      if (match && match.length > 0) {
        const compiledValue = this.compileValue(match);
        const parsedValue = Boolean(compiledValue);
        const formattedValue = localize(FormattingHelper.formatBoolean(parsedValue));
        let validationResult: ValidationResult | null = null;
        if (formattedValue != value) {
          validationResult = ValidationResult.fromText(formattedValue, ValidationResultType.Info);
        }
        return new OcrValidationResult(validationResult, compiledValue, formattedValue);
      }
    }

    const result = ValidationResult.fromText(localize(this.error), ValidationResultType.Error);
    return new OcrValidationResult(result);
  }

  //#endregion
}
