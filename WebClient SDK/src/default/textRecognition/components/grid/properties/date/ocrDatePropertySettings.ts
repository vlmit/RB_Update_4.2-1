import Moment from 'moment';
import { CalendarMonthSelectionCallback } from 'ui/datePicker/definitions';
import { IOcrPropertySettings } from '../ocrPropertySettings';

/** Settings for the recognized date property. */
export interface IOcrDatePropertySettings extends IOcrPropertySettings {
  /**
   * Action to be performed when a date is selected in the calendar.
   * @param value The selected date.
   */
  readonly handleOnDaySelect?: (value: Moment.Moment | null) => void;
  /** Action to be performed to modify the data displayed in the calendar.  */
  readonly calendarModification?: CalendarMonthSelectionCallback;
}
