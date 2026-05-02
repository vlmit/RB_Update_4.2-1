import Moment from 'moment';
import { computed, observable, runInAction } from 'mobx';
import { Visibility } from 'tessa/platform/visibility';
import { tryGetFromSettings } from 'tessa/ui/uiHelper';
import { UIButton } from 'tessa/ui/uiButton';
import { CalendarMonthSelectionCallback } from 'ui/datePicker/definitions';
import { OcrProperty } from '../ocrProperty';
import { OcrDatePropertyPicker } from './ocrDatePropertyPicker';
import { IOcrDatePropertySettings } from './ocrDatePropertySettings';
import { OcrDatePropertyDataSource } from './ocrDatePropertyDataSource';
import { DateHelper } from 'ui/datePicker/helpers';

/** View model for the recognized date property. */
export class OcrDateProperty extends OcrProperty {
  //#region constructors

  /**
   * Creates an instance of the {@link OcrDateProperty} class.
   * @param dataSource The data source for the property.
   * @param settings The settings for the property.
   */
  constructor(
    readonly dataSource: OcrDatePropertyDataSource,
    settings: IOcrDatePropertySettings
  ) {
    super(dataSource, settings);

    this._minDate = tryGetFromSettings<Moment.Moment | null>(settings, 'MinDate', null);
    this._maxDate = tryGetFromSettings<Moment.Moment | null>(settings, 'MaxDate', null);
    this._beginDate = tryGetFromSettings<Moment.Moment | null>(settings, 'BeginDate', null);
    this._highlightBeginDate = tryGetFromSettings<boolean>(settings, 'HighlightBeginDate', true);
    this.handleOnDaySelect = settings.handleOnDaySelect ?? this.handleOnDaySelectBase;
    this.calendarModification = settings.calendarModification;

    if (!tryGetFromSettings<boolean>(settings, 'DateNullable', false)) {
      this.dataSource.nullable = false;
    }
  }

  //#endregion

  //#region fields

  @observable.ref
  private _minDate: Moment.Moment | null = null;

  @observable.ref
  private _maxDate: Moment.Moment | null = null;

  @observable.ref
  private _beginDate: Moment.Moment | null = null;

  @observable.ref
  private _highlightBeginDate = true;

  //#endregion

  //#region properties

  /**
   * Minimum date boundary.
   * @remarks Days before this value will be unavailable for selection.
   * @default null
   * @shadow-available
   */
  get minDate(): Moment.Moment | null {
    return this._shadow.get('minDate', this._minDate);
  }
  set minDate(value: Moment.Moment | null) {
    runInAction(() => (this._minDate = value));
  }

  /**
   * Maximum date boundary.
   * @remarks Days after this value will be unavailable for selection.
   * @default null
   * @shadow-available
   */
  get maxDate(): Moment.Moment | null {
    return this._shadow.get('maxDate', this._maxDate);
  }
  set maxDate(value: Moment.Moment | null) {
    runInAction(() => (this._maxDate = value));
  }

  /**
   * The date on which the calendar will be opened.
   * @default null
   * @shadow-available
   */
  get beginDate(): Moment.Moment | null {
    return this._shadow.get('beginDate', this._beginDate);
  }
  set beginDate(value: Moment.Moment | null) {
    runInAction(() => (this._beginDate = value));
  }

  /**
   * Flag to highlight {@link beginDate} in the calendar.
   * @default true
   * @shadow-available
   */
  get highlightBeginDate(): boolean {
    return this._shadow.get('highlightBeginDate', this._highlightBeginDate);
  }
  set highlightBeginDate(value: boolean) {
    runInAction(() => (this._highlightBeginDate = value));
  }

  /**
   * Action to be performed when a date is selected in the calendar.
   * @param value The selected date.
   */
  readonly handleOnDaySelect: (value: Moment.Moment | null) => void;

  /** Action to be performed to modify the data displayed in the calendar.  */
  readonly calendarModification?: CalendarMonthSelectionCallback;

  /** Displayed date/time in the property, or `null` if no date/time is selected. */
  @computed
  get selectedDate(): Moment.Moment | null {
    return this.dataSource.getDate();
  }
  set selectedDate(value: Moment.Moment | null) {
    this.dataSource.setDate(
      !value ||
        (this.maxDate && value.isAfter(this.maxDate)) ||
        (this.minDate && value.isBefore(this.minDate))
        ? null
        : value
    );
  }

  //#endregion

  //#region base overrides

  protected override async initializeCore(): Promise<void> {
    await super.initializeCore();
    this.initializeCalendarButton();
  }

  //#endregion

  //#region protected methods

  protected initializeCalendarButton(): UIButton {
    const button = UIButton.create({
      name: 'Calendar',
      type: 'small',
      theme: 'control',
      icon: 'm-date',
      visibility: this.dataSource.useDate ? Visibility.Visible : Visibility.Collapsed
    });
    button.setReactDropdown(props => <OcrDatePropertyPicker property={this} {...props} />);
    this.control.toolbar.buttons.splice(0, 0, button);
    return button;
  }

  //#endregion

  //#region private methods

  private handleOnDaySelectBase = (value: Moment.Moment | null): void => {
    const dateOut = DateHelper.validateDate(value);
    if (dateOut) {
      const dateIn = DateHelper.tryGetDate(this.dataSource.getDateString());
      if (dateIn !== null) {
        dateOut.second(dateIn.second());
        dateOut.minute(dateIn.minute());
        dateOut.hour(dateIn.hour());
      }
    }

    this.selectedDate = dateOut;
    this.control.focusManager.focus();
  };

  //#endregion
}
