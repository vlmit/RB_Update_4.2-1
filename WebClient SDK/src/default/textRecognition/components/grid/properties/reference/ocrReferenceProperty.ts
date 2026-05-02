import { observable, reaction, runInAction } from 'mobx';
import { localize } from '@tessa/application';
import { Lazy, StringHelper, ValidationResult, ValidationResultType } from '@tessa/core';
import { getTessaIcon } from 'common/utility/uiHelpers';
import { Visibility } from 'tessa/platform/visibility';
import { UIButton } from 'tessa/ui/uiButton';
import { tryGetFromSettings } from 'tessa/ui/uiHelper';
import { showLoadingOverlay } from 'tessa/ui/loadingOverlay';
import { CardControlHelper } from 'tessa/ui/cards/controls/cardControlHelper';
import { ReferenceOpenMode } from 'tessa/ui/cards/controls/referenceOpenMode';
import { AdvancedCardDialogManager } from 'tessa/ui/cards/advancedCardDialogManager';
import { IOcrReferencePropertySettings } from './ocrReferencePropertySettings';
import { OcrReferencePropertyDataSource } from './ocrReferencePropertyDataSource';
import { OcrTextFieldValidatorKey } from '../../../../misc/ocrConstants';
import { OcrProperty } from '../ocrProperty';

/** View model for the recognized reference property. */
export class OcrReferenceProperty extends OcrProperty {
  //#region constructors

  /**
   * Creates an instance of the {@link OcrReferenceProperty} class.
   * @param dataSource The data source for the property.
   * @param settings The settings for the property.
   */
  constructor(
    readonly dataSource: OcrReferencePropertyDataSource,
    settings: IOcrReferencePropertySettings
  ) {
    super(dataSource, settings);

    this._manualInput = tryGetFromSettings<boolean>(settings, 'ManualInput', false);
    this._isAllowOpenRefs = tryGetFromSettings<boolean>(settings, 'IsAllowOpenRefs', true);
    this._isClearFieldVisible = tryGetFromSettings<boolean>(settings, 'IsClearFieldVisible', true);
    this._hideSelectorButton = tryGetFromSettings<boolean>(settings, 'HideSelectorButton', false);
    this._autoCompleteReferenceMode = CardControlHelper.getReferenceMode(settings);
  }

  //#endregion

  //#region fields

  @observable.ref
  private _searchDelay = 100;

  @observable.ref
  private _manualInput = false;

  @observable.ref
  private _isAllowOpenRefs = true;

  @observable.ref
  private _isClearFieldVisible = true;

  @observable.ref
  private _hideSelectorButton = false;

  @observable.ref
  private _autoCompleteReferenceMode = ReferenceOpenMode.Default;

  private static readonly _noRowsWarningResult: Lazy<ValidationResult> = new Lazy(() => {
    const message = localize('$Views_Table_NoRows');
    return ValidationResult.fromText(message, ValidationResultType.Warning);
  });

  private static readonly _noRowsErrorResult: Lazy<ValidationResult> = new Lazy(() => {
    const message = localize('$Views_Table_NoRows');
    return ValidationResult.fromText(message, ValidationResultType.Error);
  });

  //#endregion

  //#region properties

  /**
   * Indicates whether manual input is allowed.
   * @default false
   * @shadow-available
   */
  get manualInput(): boolean {
    return this._shadow.get('manualInput', this._manualInput);
  }
  set manualInput(value: boolean) {
    runInAction(() => (this._manualInput = value));
  }

  /**
   * Specifies whether opening links is permitted.
   * @default true
   * @shadow-available
   */
  get isAllowOpenRefs(): boolean {
    return this._shadow.get('isAllowOpenRefs', this._isAllowOpenRefs);
  }
  set isAllowOpenRefs(value: boolean) {
    runInAction(() => (this._isAllowOpenRefs = value));
  }

  /**
   * Determines whether the "Clear" button is visible.
   * @default true
   * @shadow-available
   */
  get isClearFieldVisible(): boolean {
    return this._shadow.get('isClearFieldVisible', this._isClearFieldVisible);
  }
  set isClearFieldVisible(value: boolean) {
    runInAction(() => (this._isClearFieldVisible = value));
  }

  /**
   * Indicates whether the selector button is hidden.
   * @default false
   * @shadow-available
   */
  get hideSelectorButton(): boolean {
    return this._shadow.get('hideSelectorButton', this._hideSelectorButton);
  }
  set hideSelectorButton(value: boolean) {
    runInAction(() => (this._hideSelectorButton = value));
  }

  /**
   * Defines the mode for automatically completing references.
   * @default Default
   * @shadow-available
   */
  get autoCompleteReferenceMode(): ReferenceOpenMode {
    return this._shadow.get('autoCompleteReferenceMode', this._autoCompleteReferenceMode);
  }
  set autoCompleteReferenceMode(value: ReferenceOpenMode) {
    runInAction(() => (this._autoCompleteReferenceMode = value));
  }

  /**
   * Delay in milliseconds before the search for dropdown list items begins.
   * @default 100
   * @shadow-available
   */
  get searchDelay(): number {
    return this._shadow.get('searchDelay', this._searchDelay);
  }
  set searchDelay(value: number) {
    runInAction(() => (this._searchDelay = value));
  }

  //#endregion

  //#region base overrides

  protected override async initializeCore(): Promise<void> {
    await super.initializeCore();
    this.initializeTextFieldControl();
    this.initializeSelectButton();
    this.initializeOpenButton();
  }

  protected override initializeValidation(): void {
    super.initializeValidation();

    this.control.validationContainer.removeById(OcrTextFieldValidatorKey);
    this.control.validationContainer.add(
      context => {
        const displayed = context.rawValue;
        const formattedValue = this.dataSource.getItemText();

        if (displayed && !formattedValue) {
          context.addResult(
            this.manualInput
              ? OcrReferenceProperty._noRowsWarningResult.value
              : OcrReferenceProperty._noRowsErrorResult.value
          );
          context.handled = !this.manualInput;
        } else if (formattedValue && !StringHelper.equals(formattedValue, displayed, false)) {
          context.addResult({
            type: ValidationResultType.Info,
            message: formattedValue
          });
        } else if (StringHelper.equals(formattedValue, displayed, false)) {
          if (!this.dataSource.getItemReference()) {
            context.addResult(OcrReferenceProperty._noRowsWarningResult.value);
          }
        }
      },
      { id: OcrTextFieldValidatorKey, order: Number.MAX_SAFE_INTEGER }
    );
  }

  protected override initializeClearButton(): UIButton {
    const button = super.initializeClearButton();
    button.setVisibility(() =>
      this.dataSource.nullable && this.isClearFieldVisible
        ? Visibility.Visible
        : Visibility.Collapsed
    );
    return button;
  }

  //#endregion

  //#region protected methods

  protected initializeTextFieldControl(): void {
    this.disposeList.add(
      reaction(
        () => this.dataSource.itemsLoading,
        loading => (this.control.inProgress = loading)
      )
    );

    this.control.onTextChange.add(async ({ text }) => {
      !!text ? await this.dataSource.findItem(text, this.searchDelay) : this.dataSource.clear();
    });

    this.control.focusManager.onBlur.add(
      async () => {
        if (this.dataSource.hasChanges(true)) {
          const displayed = this.displayed;
          const validator = this.control.validationContainer;
          const validationResult = await validator.validateAsync(displayed, displayed);
          const value = validationResult.isSuccessful ? this.value : null;
          this.dataSource.set(displayed, value);
        }
      },
      { order: Number.MAX_SAFE_INTEGER }
    );
  }

  protected initializeSelectButton(): UIButton {
    const button = UIButton.create({
      name: 'Select',
      type: 'small',
      theme: 'control',
      icon: 'm-etc',
      visibility: () => (this.hideSelectorButton ? Visibility.Collapsed : Visibility.Visible),
      buttonAction: async () => {
        await this.contextExecutor(async () => {
          const displayed = await this.dataSource.selectItem();
          displayed && this.dataSource.set(displayed, displayed);
        });
        this.control.focusManager.focus();
      }
    });
    this.control.toolbar.buttons.splice(0, 0, button);
    return button;
  }

  protected initializeOpenButton(): UIButton {
    const button = UIButton.create({
      name: 'Open',
      type: 'small',
      theme: 'control',
      icon: getTessaIcon('Thin77'),
      visibility: () =>
        this.isAllowOpenRefs && !!this.dataSource.getItemReference()
          ? Visibility.Visible
          : Visibility.Collapsed,
      buttonAction: async () => {
        const cardId = this.dataSource.getItemReference();
        if (cardId) {
          await this.contextExecutor(async context => {
            await showLoadingOverlay(async splashResolve => {
              await AdvancedCardDialogManager.instance.openCard({
                splashResolve,
                cardId,
                context,
                info: { ['.autocomplete']: this.dataSource.getItemInfo() },
                isCustomEditorOverridings: true,
                cardEditorActionOverridings: CardControlHelper.getReferenceOpenModeOverridings(
                  this.autoCompleteReferenceMode,
                  context,
                  null
                )
              });
            });
          });
        }
      }
    });
    this.control.toolbar.buttons.splice(0, 0, button);
    return button;
  }

  //#endregion
}
