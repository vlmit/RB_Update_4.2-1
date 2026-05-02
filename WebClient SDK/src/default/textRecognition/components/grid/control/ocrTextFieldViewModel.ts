import { computed, observable, runInAction } from 'mobx';
import { ValidationResult, ValidationResultType } from '@tessa/core';
import { DefaultEventHandlers } from 'ui/hooks/defaultEventHandlers';
import { TextFieldViewModel } from 'ui/textField/textFieldViewModel';
import { InputToolbarAffixFactory } from 'ui/textField/inputToolbarAffix';
import { InputNotifyMode } from 'ui/textField/textFieldTypes';
import { OcrTextFieldValidatorKey } from '../../../misc/ocrConstants';
import { OcrValidator } from '../../../validators/ocrValidator';
import { IOcrTextFieldDataSource } from './ocrTextFieldTypes';

/** View model for a text recognized field. */
export class OcrTextFieldViewModel extends TextFieldViewModel {
  //#region constructors

  /**
   * Creates an instance of {@link OcrTextFieldViewModel} class.
   * @param _dataSource The data source for OCR text field.
   * @param _validator The object for OCR text field validation.
   */
  constructor(
    protected readonly _dataSource: IOcrTextFieldDataSource,
    protected readonly _validator: OcrValidator | null
  ) {
    super(_dataSource);
  }

  //#endregion

  //#region fields

  @observable.ref
  private _inProgress = false;

  @observable.ref
  private _value: string | null | undefined = undefined;

  private static readonly _cloneIcon = 'fa fa-clone';
  private static readonly _infoIcon = 'fa fa-info-circle';
  private static readonly _successIcon = 'fa fa-check-circle';
  private static readonly _errorIcon = 'fa fa-exclamation-circle';
  private static readonly _warningIcon = 'fa fa-exclamation-triangle';
  private static readonly _loadingIcon = 'fa fa-spinner fa-spin';

  //#endregion

  //#region properties

  @computed
  override get text(): string {
    if (this._text !== null) {
      return this._text;
    } else if (this._dataSource.isChanged) {
      return this._dataSource.getDisplayed() ?? '';
    } else {
      const editMode = this.availability === 'enabled' && this.focusManager.isFocused;
      return this.formatValue(this.value, editMode);
    }
  }
  override set text(value: string) {
    runInAction(() => (this._text = value));
    this.notifyValueChanged(value, 'instant');
  }

  @computed
  override get value(): string | null {
    if (this._value !== undefined) {
      return this._value;
    } else {
      return this._dataSource.getValue();
    }
  }
  override set value(value: string | null) {
    runInAction(() => (this._value = value));
  }

  /**
   * Indicates whether text field in-progress state.
   * @default false
   * @shadow-available
   */
  get inProgress(): boolean {
    return this._shadow.get('inProgress', this._inProgress);
  }
  set inProgress(value: boolean) {
    runInAction(() => (this._inProgress = value));
  }

  //#endregion

  //#region base overrides

  protected override async initializeCore(): Promise<void> {
    await super.initializeCore();
    this.initializeValidation();
    this.initializeHandlers();
    this.initializeAffix();
    this.syncCommittedChanges = true;
  }

  protected override notifyValueChanged(value: string, notifyMode: InputNotifyMode): void {
    if (this.notifyMode === notifyMode) {
      super.notifyValueChanged(value, notifyMode);
      if (!this.validationContainer.isEnabled || this.validationContainer.result.hasErrors) {
        this.value = null;
      }
    }
  }

  protected override formatValue(value: string | null, editMode?: boolean): string {
    if (this._validator) {
      const { validationResult, formattedValue } = this._validator.validate(value ?? '');
      return !validationResult?.hasErrors ? (formattedValue ?? '') : '';
    } else {
      return super.formatValue(value, editMode);
    }
  }

  protected override convertValue(value: string, final?: boolean): string | null {
    if (final && this._validator) {
      const { validationResult, compiledValue } = this._validator.validate(value);
      return !validationResult?.hasErrors ? compiledValue : null;
    } else {
      return super.convertValue(value, final);
    }
  }

  override commitChanges(): void {
    if (this._text !== null) {
      this._dataSource.setDisplayed(this.text);
      this.notifyValueChanged(this._text, 'blur');
    }
    if (this._value !== undefined) {
      const converted = this.convertValue(this._value ?? '', true);
      this._dataSource.setValue(converted);
    }
    if (this.syncCommittedChanges) {
      this.syncWithDataSource();
    }
  }

  override syncWithDataSource(): void {
    runInAction(() => {
      this._text = null;
      this._value = undefined;
    });
  }

  //#endregion

  //#region protected methods

  protected initializeValidation(): void {
    this.validationContainer.add(
      context => {
        if (!this.inProgress && context.value) {
          const result = this._validator?.validate(context.value);
          if (result?.validationResult) {
            context.addResult(result.validationResult);
          }
        }
      },
      { id: OcrTextFieldValidatorKey, order: Number.MAX_SAFE_INTEGER }
    );
  }

  protected initializeHandlers(): void {
    this.handlersContainer.onKeyDown.add(e => {
      if ((this.maxRows ?? 1) > 1 && e.code === 'Enter') {
        DefaultEventHandlers.stopBubble(e);
      }
    });
  }

  protected initializeAffix(): void {
    const affix = InputToolbarAffixFactory.create('', true);
    affix.setIcon(() => this.getPrefixIcon());
    affix.setStyle(() => this.getPrefixStyle(affix.icon));
    this.toolbar.prefixes.add(affix);

    this.handleOnExpand = null;
    this.handleOnCollapse = null;
  }

  //#endregion

  //#region private methods

  private getPrefixIcon(): string {
    if (this.inProgress) {
      return OcrTextFieldViewModel._loadingIcon;
    }

    const { invalid, result } = this.validationContainer.resultDisplay;
    if (invalid) {
      return OcrTextFieldViewModel._errorIcon;
    } else if (typeof result === 'string') {
      return OcrTextFieldViewModel._infoIcon;
    } else if (result instanceof ValidationResult) {
      if (result.items.length > 0) {
        let hasWarning = false;
        for (const item of result.items) {
          if (item.type === ValidationResultType.Error) {
            return OcrTextFieldViewModel._errorIcon;
          } else if (!hasWarning && item.type === ValidationResultType.Warning) {
            hasWarning = true;
          }
        }
        return hasWarning ? OcrTextFieldViewModel._warningIcon : OcrTextFieldViewModel._infoIcon;
      }
    }

    return this._dataSource.isChanged
      ? OcrTextFieldViewModel._successIcon
      : OcrTextFieldViewModel._cloneIcon;
  }

  private getPrefixStyle(icon: string | null): React.CSSProperties {
    const style: React.CSSProperties = { pointerEvents: 'none' };

    switch (icon) {
      case OcrTextFieldViewModel._successIcon:
        return { color: '#51d356', ...style };
      case OcrTextFieldViewModel._errorIcon:
        return { color: '#bf3737', ...style };
      case OcrTextFieldViewModel._infoIcon:
        return { color: '#9baaaa', ...style };
      case OcrTextFieldViewModel._warningIcon:
        return { color: '#fbac35', ...style };
      default:
        return style;
    }
  }

  //#endregion
}
