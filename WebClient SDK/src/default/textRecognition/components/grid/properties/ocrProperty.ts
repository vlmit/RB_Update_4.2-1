import { observable, runInAction } from 'mobx';
import { localize } from '@tessa/application';
import { SchemeDbType } from '@tessa/platform';
import { Lazy, ValidationResult, ValidationResultType } from '@tessa/core';
import { ShadowPropsHelper, ShadowPropsStage, ValidationContainerConstants } from '@tessa/ui';
import {
  IUIContextExecutor,
  IUIContextExecutorProvider,
  IUIContext,
  UIContext
} from 'tessa/ui/uiContext';
import { UIButton } from 'tessa/ui/uiButton';
import { Property } from 'tessa/ui/propertyGrid';
import { tryGetFromSettings } from 'tessa/ui/uiHelper';
import { Visibility } from 'tessa/platform/visibility';
import { ScopeContextInstance } from 'tessa/platform/scopes/scopeContext';
import { OcrValidator } from '../../../validators/ocrValidator';
import { OcrGuidValidator } from '../../../validators/ocrGuidValidator';
import { OcrStringValidator } from '../../../validators/ocrStringValidator';
import { OcrDoubleValidator } from '../../../validators/ocrDoubleValidator';
import { OcrDecimalValidator } from '../../../validators/ocrDecimalValidator';
import { OcrIntegerValidator } from '../../../validators/ocrIntegerValidator';
import { OcrBooleanValidator } from '../../../validators/ocrBooleanValidator';
import { OcrDateTimeValidator } from '../../../validators/ocrDateTimeValidator';
import { OcrTextFieldViewModel } from '../control/ocrTextFieldViewModel';
import { OcrPropertyDataSource } from './ocrPropertyDataSource';
import { IOcrPropertySettings } from './ocrPropertySettings';

/** View model for a recognized text property. */
export class OcrProperty
  extends Property<OcrPropertyDataSource, IOcrPropertySettings>
  implements IUIContextExecutorProvider
{
  //#region constructors

  /**
   * Creates an instance of the {@link OcrProperty} class.
   * @param dataSource The data source for the property.
   * @param settings The settings for the property.
   */
  constructor(dataSource: OcrPropertyDataSource, settings: IOcrPropertySettings) {
    super(dataSource, settings);

    this.control = this.resolveControl(settings);
    this.dataSource.nullable = tryGetFromSettings<boolean>(settings, 'Nullable', true);
  }

  //#endregion

  //#region fields

  @observable.ref
  protected _requiredText: string | null = null;

  private readonly _nullableErrorResult: Lazy<ValidationResult> = new Lazy(() => {
    const message = localize('$Cards_ValidationKey_NullField', this.caption.localized);
    return ValidationResult.fromText(message, ValidationResultType.Error);
  });

  private readonly _requiredErrorResult: Lazy<ValidationResult> = new Lazy(() => {
    const message = localize(this.requiredText || '$UI_Common_FieldRequiredErrorText');
    return ValidationResult.fromText(message, ValidationResultType.Error);
  });

  private static readonly _longOperationInfoResult: Lazy<ValidationResult> = new Lazy(() => {
    const message = localize('$Views_GetData_LongOperation');
    return ValidationResult.fromText(message, ValidationResultType.Info);
  });

  //#endregion

  //#region properties

  /**
   * View model for the recognized text field control.
   * @remarks Ensure interactions with this model occur
   * after it has been initialized to avoid unexpected behavior.
   */
  readonly control: OcrTextFieldViewModel;

  /**
   * The text displayed when the control requires a value but none is provided.
   * If `null` or an empty string, the default string from the validator will be used.
   * @default null
   * @shadow-available
   */
  get requiredText(): string | null {
    return this._shadow.get('requiredText', this._requiredText);
  }
  set requiredText(value: string | null) {
    runInAction(() => (this._requiredText = value));
  }

  /** The actual value of the property. */
  get value(): string | null {
    return this.control.value;
  }
  set value(value: string | null) {
    this.control.value = value;
  }

  /** The displayed value of the property. */
  get displayed(): string {
    return this.control.text;
  }
  set displayed(value: string) {
    this.control.text = value;
  }

  /** Reference(-s) on linked item(-s) (unique identifier of item(-s)). */
  get reference(): number[] | number | null {
    return this.dataSource.getRef();
  }

  get required(): boolean {
    return this.control.required;
  }
  set required(value: boolean) {
    this.control.required = value;
  }

  //#endregion

  //#region IUIContextExecutorProvider implementation

  private _contextExecutor: IUIContextExecutor | null = null;

  private static readonly _contextExecutorDefault: IUIContextExecutor = action => {
    let scopeContext: ScopeContextInstance<IUIContext> | null = null;
    try {
      scopeContext = UIContext.create(UIContext.current);
      return action(UIContext.current);
    } finally {
      scopeContext?.dispose();
    }
  };

  get contextExecutor(): IUIContextExecutor {
    return this._contextExecutor ?? OcrProperty._contextExecutorDefault;
  }

  setContextExecutor(contextExecutor: IUIContextExecutor | null): void {
    this._contextExecutor = contextExecutor;
  }

  //#endregion

  //#region base overrides

  protected override async initializeCore(): Promise<void> {
    await super.initializeCore();
    await this.control.initialize();
    this.control.themeOptions.controlTheme.setParent(this.theme);
    this.initializeValidation();
    this.initializeClearButton();
    this.initializeAvailabilityReaction();
    this.initializeButtonsAvailabilityReaction();
  }

  protected override disposeCore(): void {
    this.control.dispose();
    super.disposeCore();
  }

  //#endregion

  //#region protected methods

  protected resolveControl(settings: IOcrPropertySettings): OcrTextFieldViewModel {
    const validator = this.resolveValidator(settings);
    const control = new OcrTextFieldViewModel(this.dataSource, validator);
    control.minRows = tryGetFromSettings<number>(settings, 'MinRows', 1);
    control.maxRows = tryGetFromSettings<number>(settings, 'MaxRows', 1);
    const theme = tryGetFromSettings(settings, 'Theme', undefined);
    theme && control.themeOptions.controlTheme.build(theme);
    control.validationContainer.resultInfo = this.validationResultInfo;
    control.themeOptions.border = 'hover';
    control.errorDisplayType = 'doorhanger';

    const { placeholder, required } = settings;
    if (placeholder !== undefined) {
      control.placeholder = placeholder;
    }
    if (required !== undefined) {
      control.required = required;
    }

    return control;
  }

  protected resolveValidator(settings: IOcrPropertySettings): OcrValidator | null {
    switch (settings.schemeType) {
      case SchemeDbType.Boolean:
        return new OcrBooleanValidator();
      case SchemeDbType.Guid:
        return new OcrGuidValidator();
      case SchemeDbType.Byte:
      case SchemeDbType.UInt16:
      case SchemeDbType.UInt32:
      case SchemeDbType.UInt64:
        return new OcrIntegerValidator(settings, true);
      case SchemeDbType.SByte:
      case SchemeDbType.Int16:
      case SchemeDbType.Int32:
      case SchemeDbType.Int64:
        return new OcrIntegerValidator(settings, false);
      case SchemeDbType.Single:
      case SchemeDbType.Double:
        return new OcrDoubleValidator(settings);
      case SchemeDbType.Decimal:
      case SchemeDbType.Currency:
        return new OcrDecimalValidator(settings);
      case SchemeDbType.AnsiString:
      case SchemeDbType.AnsiStringFixedLength:
      case SchemeDbType.String:
      case SchemeDbType.StringFixedLength:
      case SchemeDbType.Xml:
      case SchemeDbType.Json:
      case SchemeDbType.BinaryJson:
        return new OcrStringValidator(settings);
      case SchemeDbType.Time:
      case SchemeDbType.Date:
      case SchemeDbType.DateTime:
      case SchemeDbType.DateTime2:
      case SchemeDbType.DateTimeOffset:
        return new OcrDateTimeValidator(settings);
      case SchemeDbType.Object:
        return null;
      default:
        throw new Error(`Unsupported scheme type.`);
    }
  }

  protected initializeValidation(): void {
    this.disposeList.add(
      () => this.caption.localized,
      () => this._nullableErrorResult.reset()
    );

    this.disposeList.add(
      () => this.requiredText,
      () => this._requiredErrorResult.reset()
    );

    this.control.validationContainer.add(
      context => {
        if (this.control.inProgress) {
          context.addResult(OcrProperty._longOperationInfoResult.value);
          context.handled = true;
        }
      },
      { order: 0 }
    );

    this.control.validationContainer.add(
      context => {
        if (!this.dataSource.nullable && context.value == null) {
          context.addResult(this._nullableErrorResult.value);
        }
      },
      { order: 1 }
    );

    this.control.validationContainer.removeById(ValidationContainerConstants.requiredValidator);
    this.control.validationContainer.add(
      context => {
        if (this.required && !context.value) {
          context.addResult(this._requiredErrorResult.value);
        }
      },
      {
        id: ValidationContainerConstants.requiredValidator,
        order: ValidationContainerConstants.requiredValidatorOrder
      }
    );
  }

  protected initializeClearButton(): UIButton {
    const button = UIButton.create({
      name: 'Clear',
      type: 'small',
      theme: 'control',
      icon: 'm-cross',
      isEnabled: () => !!this.displayed,
      visibility: () => (this.dataSource.nullable ? Visibility.Visible : Visibility.Collapsed),
      buttonAction: () => this.dataSource.clear()
    });
    this.control.toolbar.buttons.add(button);
    return button;
  }

  protected initializeAvailabilityReaction(): void {
    this.disposeList.add(
      ShadowPropsHelper.add(
        this.control,
        'availability',
        previous => (this.disabled ? this.toAvailable(false, true) : previous()),
        { stage: ShadowPropsStage.After }
      )
    );
  }

  protected initializeButtonsAvailabilityReaction(): void {
    this.disposeList.add(
      ShadowPropsHelper.add(
        this.control.toolbar.buttons,
        'availability',
        previous => (this.disabled ? this.toAvailable(false) : previous()),
        { stage: ShadowPropsStage.After }
      )
    );
  }

  //#endregion
}
