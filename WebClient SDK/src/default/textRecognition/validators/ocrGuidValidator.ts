import { Guid, ValidationResult, ValidationResultType } from '@tessa/core';
import { localize } from '@tessa/application';
import { OcrValidationResult } from './ocrValidationResult';
import { OcrValidator } from './ocrValidator';

/** Валидатор значения в формате UUID. */
export class OcrGuidValidator extends OcrValidator {
  //#region base overrides

  protected get patterns(): ReadonlyArray<RegExp> {
    return [];
  }

  protected get error(): string {
    return '$UI_Controls_Guid_InvalidValue';
  }

  protected compileValue(match: RegExpExecArray): string {
    return match[0];
  }

  //#endregion

  //#region public methods

  validate(value: string): OcrValidationResult {
    if (Guid.isValid(value)) {
      const formattedValue = value.toLowerCase();
      let validationResult: ValidationResult | null = null;
      if (formattedValue != value) {
        validationResult = ValidationResult.fromText(formattedValue, ValidationResultType.Info);
      }
      return new OcrValidationResult(validationResult, value, formattedValue);
    }

    const result = ValidationResult.fromText(localize(this.error), ValidationResultType.Error);
    return new OcrValidationResult(result);
  }

  //#endregion
}
