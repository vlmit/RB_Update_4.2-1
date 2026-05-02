import Moment from 'moment';
import { injectable } from '@tessa/application';
import { DateTimeTypeFormat } from '@tessa/platform';
import { ValueDataSource } from '@tessa/ui';
import { PlaygroundArticle, DemoForm } from 'tessa/ui/playground/chunk';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';
import { DatePickerViewModel } from 'ui/datePicker/datePickerViewModel';
import { DatePicker } from 'ui/datePicker/datePicker';
import { showMessage } from 'tessa/ui/tessaDialog/show';

@injectable()
export class DatePickerArticle extends PlaygroundArticle {
  constructor() {
    super();
  }

  override getSettings(): PlaygroundArticleSettings {
    return {
      name: 'Controls/Date Picker',
      description: 'Date Picker allows users enter and edit date and time.'
    };
  }

  override async initialize(): Promise<void> {
    this.addBlock({
      caption: 'Formats',
      props: async () => {
        const date = new DatePickerViewModel();
        date.caption = 'Date';
        date.formatType = DateTimeTypeFormat.Date;
        await date.initialize();

        const dateTime = new DatePickerViewModel();
        dateTime.caption = 'DateTime';
        dateTime.formatType = DateTimeTypeFormat.DateTime;
        await dateTime.initialize();

        const interval = new DatePickerViewModel();
        interval.caption = 'Interval';
        interval.formatType = DateTimeTypeFormat.Interval;
        await interval.initialize();

        const time = new DatePickerViewModel();
        time.caption = 'Time';
        time.formatType = DateTimeTypeFormat.Time;
        await time.initialize();

        return {
          date,
          dateTime,
          interval,
          time
        };
      },
      view: ({ date, dateTime, interval, time }) => (
        <DemoForm customStyles={css => css({ flexDirection: 'column', gap: '5px' })}>
          <DatePicker viewModel={date} />
          <DatePicker viewModel={dateTime} />
          <DatePicker viewModel={interval} />
          <DatePicker viewModel={time} />
        </DemoForm>
      )
    });
    this.addBlock({
      caption: 'Force Timezone',
      description:
        'The forceTimezone flag ensures that the date is displayed with respect to the user’s local timezone',
      props: async () => {
        const dataSource = new ValueDataSource<string | null>(null);

        const dateTime = new DatePickerViewModel(dataSource);
        dateTime.caption = 'DateTime';
        dateTime.formatType = DateTimeTypeFormat.DateTime;
        await dateTime.initialize();

        const dateWithForceTimezone = new DatePickerViewModel(dataSource);
        dateWithForceTimezone.caption = 'Date With Force Timezone';
        dateWithForceTimezone.formatType = DateTimeTypeFormat.Date;
        dateWithForceTimezone.forceTimezone = true;
        await dateWithForceTimezone.initialize();

        return {
          dateTime,
          dateWithForceTimezone
        };
      },
      view: ({ dateTime, dateWithForceTimezone }) => (
        <DemoForm customStyles={css => css({ flexDirection: 'column', gap: '5px' })}>
          <DatePicker viewModel={dateTime} />
          <DatePicker viewModel={dateWithForceTimezone} />
        </DemoForm>
      )
    });

    this.addBlock({
      caption: 'Date Range',
      props: async () => {
        const minDate = Moment().subtract(10, 'days');
        const maxDate = Moment().add(10, 'days');

        const dateRange = new DatePickerViewModel();
        dateRange.caption = 'DatePicker with date range';
        dateRange.tooltip.text = `Date range: from ${minDate} to ${maxDate}.`;
        dateRange.minDate = minDate;
        dateRange.maxDate = maxDate;
        await dateRange.initialize();

        return {
          dateRange
        };
      },
      view: ({ dateRange }) => (
        <DemoForm customStyles={css => css({ flexDirection: 'column', gap: '5px' })}>
          <DatePicker viewModel={dateRange} />
        </DemoForm>
      )
    });

    this.addBlock({
      caption: 'Hide Time Seconds',
      description: 'The hideTimeSeconds option hides the seconds when displaying time.',
      props: async () => {
        const showSeconds = new DatePickerViewModel();
        showSeconds.caption = 'Time with Seconds (default)';
        showSeconds.formatType = DateTimeTypeFormat.DateTime;
        showSeconds.hideTimeSeconds = false;
        await showSeconds.initialize();

        const hideSeconds = new DatePickerViewModel();
        hideSeconds.caption = 'Time without Seconds';
        hideSeconds.formatType = DateTimeTypeFormat.DateTime;
        hideSeconds.hideTimeSeconds = true;
        await hideSeconds.initialize();

        return {
          showSeconds,
          hideSeconds
        };
      },
      view: ({ showSeconds, hideSeconds }) => (
        <DemoForm customStyles={css => css({ flexDirection: 'column', gap: '5px' })}>
          <DatePicker viewModel={showSeconds} />
          <DatePicker viewModel={hideSeconds} />
        </DemoForm>
      )
    });

    this.addBlock({
      caption: 'Events',
      props: async () => {
        const onDateChange = new DatePickerViewModel();
        onDateChange.caption = 'onDateChange';
        onDateChange.formatType = DateTimeTypeFormat.DateTime;
        const controlOnDateChange = onDateChange.onDateChange;
        onDateChange.onDateChange = date => {
          controlOnDateChange(date);
          showMessage(`onDateChange event handled. Updated date value: ${date}.`);
        };
        await onDateChange.initialize();

        const onCalendarButtonClick = new DatePickerViewModel();
        onCalendarButtonClick.caption = 'onCalendarButtonClick';
        const controlOnCalendarButtonClick = onCalendarButtonClick.onCalendarButtonClick;
        onCalendarButtonClick.onCalendarButtonClick = (b, e) => {
          controlOnCalendarButtonClick(b, e);
          showMessage(`onCalendarButtonClick event handled. Calendar button clicked.`);
        };
        await onCalendarButtonClick.initialize();

        return {
          onDateChange,
          onCalendarButtonClick
        };
      },
      view: ({ onDateChange, onCalendarButtonClick }) => (
        <DemoForm customStyles={css => css({ flexDirection: 'column', gap: '5px' })}>
          <DatePicker viewModel={onDateChange} />
          <DatePicker viewModel={onCalendarButtonClick} />
        </DemoForm>
      )
    });
  }
}
