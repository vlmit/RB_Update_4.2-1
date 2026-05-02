import { IStorage, ValidationResult, ValidationResultType } from '@tessa/core';
import { localize } from '@tessa/application';
import { tryGetFromSettings } from 'tessa/ui/uiHelper';
import { OcrValidationResult } from './ocrValidationResult';
import { OcrValidator } from './ocrValidator';

/** Валидатор строкового значения. */
export class OcrStringValidator extends OcrValidator {
  //#region fields

  /** Максимальная длина строки. */
  private readonly _maxLength: number;

  //#endregion

  //#region constructors

  /**
   * Создает экземпляр класса {@link OcrStringValidator}.
   * @param settings Настройки типа проверяемого контрола.
   */
  constructor(settings: IStorage) {
    super();
    this._maxLength = tryGetFromSettings(settings, 'MaxLength', Number.MAX_SAFE_INTEGER);
  }

  //#endregion

  //#region base overrides

  protected get patterns(): ReadonlyArray<RegExp> {
    return [];
  }

  protected get error(): string {
    return '$UI_Cards_TypesEditor_MaximumLength';
  }

  protected compileValue(match: RegExpExecArray): string {
    return match[0];
  }

  validate(value: string): OcrValidationResult {
    return value.length > this._maxLength
      ? new OcrValidationResult(
          ValidationResult.fromText(localize(this.error), ValidationResultType.Error),
          value
        )
      : new OcrValidationResult(null, value, value);
  }

  //#endregion
}
