import Moment from 'moment';
import { DateTimeTypeFormat } from '@tessa/platform';
import { tryGetFromSettings } from 'tessa/ui/uiHelper';
import { IPropertyGridDataProvider } from 'tessa/ui/propertyGrid';
import { DatePickerFormatter } from 'ui/datePicker/datePickerFormatter';
import { OcrPropertyDataSource } from '../ocrPropertyDataSource';
import { IOcrDatePropertySettings } from './ocrDatePropertySettings';

/** Data source for the recognized date property. */
export class OcrDatePropertyDataSource extends OcrPropertyDataSource {
  //#region constructors

  /**
   * Creates an instance of the {@link OcrPropertyDataSource} class.
   * @param dataProvider The data provider for getting/setting the property value.
   * @param settings The settings for the property.
   */
  constructor(dataProvider: IPropertyGridDataProvider, settings: IOcrDatePropertySettings) {
    super(dataProvider, settings);

    this._ignoreTimezone = tryGetFromSettings<boolean>(settings, 'IgnoreTimezone', false);
    const dateEnabled = tryGetFromSettings<boolean>(settings, 'DateFormat', false);
    const timeEnabled = tryGetFromSettings<boolean>(settings, 'TimeEnabled', false);
    const dateTimeFormat = tryGetFromSettings<string>(settings, 'DateTimeFormat', '');
    this._dateTimeFormat =
      DateTimeTypeFormat[dateTimeFormat] ??
      ((dateEnabled && timeEnabled) || (!dateEnabled && !timeEnabled)
        ? DateTimeTypeFormat.DateTime
        : dateEnabled
          ? DateTimeTypeFormat.Date
          : DateTimeTypeFormat.Time);
    this.useDate =
      this._dateTimeFormat === DateTimeTypeFormat.Date ||
      this._dateTimeFormat === DateTimeTypeFormat.DateTime;
    this._dateTimeFormatter = new DatePickerFormatter({ dateTimeFormat: this._dateTimeFormat });
  }

  //#endregion

  //#region fields

  private readonly _ignoreTimezone: boolean;
  private readonly _dateTimeFormat: DateTimeTypeFormat;
  private readonly _dateTimeFormatter: DatePickerFormatter;
  private static readonly _format = ['YYYY-MM-DDTHH:mm:ssZ', Moment.ISO_8601];

  //#endregion

  //#region properties

  /** Flag indicating whether date selection is enabled based on the format settings. */
  readonly useDate: boolean;

  //#endregion

  //#region public methods

  override getValue(): string | null {
    return this.isChanged ? super.getValue() : this.getDateFormatted();
  }

  /**
   * Retrieves the date/time displayed by the property, or `null` if no date/time is selected.
   * The date is adjusted according to the timezone and formatting settings.
   * @returns The date/time as a moment object, or `null` if no date/time is selected.
   */
  getDate(): Moment.Moment | null {
    let value = super.getValue();
    if (!value) {
      return null;
    }

    // Adjust the year if it's before 1970, as Moment.js has issues with dates before 1970
    value = !this.useDate ? value.replace(/^1753/, '1970') : value;

    const date = Moment.utc(value, OcrDatePropertyDataSource._format, true);
    if (!date?.isValid()) {
      return null;
    }

    if (this._dateTimeFormat === DateTimeTypeFormat.Date || this._ignoreTimezone) {
      // Convert UTC to local date considering daylight saving time (DST)
      const kltLocalDate = date.clone().local(true);
      const localDate = date.clone().local();
      const offsetDiff = localDate.utcOffset() - kltLocalDate.utcOffset();
      if (offsetDiff !== 0) {
        kltLocalDate.add(offsetDiff, 'minute');
      }
      return kltLocalDate;
    }

    return date.local();
  }

  /**
   * Sets the date/time to be displayed by the property.
   * If `null` is passed, clears the current value.
   * Adjusts the date according to the settings and formats it for storage.
   * @param value The date/time to set, or `null` to clear the value.
   */
  setDate(value: Moment.Moment | null): void {
    if (!value) {
      this.clear();
      return;
    }

    // Adjust the date if it's not needed
    value = !this.useDate ? value.month(0).date(2) : value;

    let formatDate =
      this._dateTimeFormat === DateTimeTypeFormat.Date || this._ignoreTimezone
        ? value.utc(true).format() // Convert local value to UTC
        : value.utc().format();

    // Replace year manually if date is not used
    formatDate = !this.useDate ? formatDate.replace(/^\d\d\d\d/, '1753') : formatDate;

    this.setValue(formatDate);
    this.setDisplayed(this.getDateFormatted());
  }

  /**
   * Returns the date/time displayed by the control in string format, or `null` if no date/time is selected.
   * @returns The date/time as a formatted string, or `null` if no date/time is selected.
   */
  getDateString(): string | null {
    return this.getDate()?.format() ?? null;
  }

  /**
   * Returns the formatted string representation of the date/time
   * displayed by the control, or `null` if no date/time is selected.
   * @returns The formatted string representation of the date/time, or `null` if no date/time is selected.
   */
  getDateFormatted(): string | null {
    const date = Moment(this.getDateString(), OcrDatePropertyDataSource._format, true);
    const value = date?.isValid() && date.year() >= 1753 ? date : null;
    return this._dateTimeFormatter.getDateFormat(value) ?? null;
  }

  //#endregion
}
