import React from 'react';
import Moment from 'moment';
import Platform from 'common/platform';
import { UIButtonDropdownProps } from 'tessa/ui/uiButton';
import { DatePickerCalendar, Dialog, DialogContainer, DialogContent, Popover } from 'ui';
import { OcrDateProperty } from './ocrDateProperty';

/** Props for the date picker component used within the recognized date property. */
interface OcrDatePropertyPickerProps extends UIButtonDropdownProps {
  /** View model for the recognized date property */
  property: OcrDateProperty;
}

/** Component for selecting a date from a calendar within the recognized date property. */
export const OcrDatePropertyPicker: React.FC<OcrDatePropertyPickerProps> = props => {
  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  const { property, isOpened, onOpen, onClose, ...dropdownProps } = props;

  const handleCloseRequest = React.useCallback(() => {
    onClose?.();
  }, [onClose]);

  const handleOnDaySelect = React.useCallback(
    (value: Moment.Moment) => {
      property.handleOnDaySelect(value);
      handleCloseRequest();
    },
    [property, handleCloseRequest]
  );

  const calendar = (
    <DatePickerCalendar
      minDate={property.minDate}
      maxDate={property.maxDate}
      beginDate={property.beginDate}
      selectedDate={property.selectedDate}
      isHighlightBeginDate={property.highlightBeginDate}
      onCalendarModification={property.calendarModification}
      onCalendarDaySelect={handleOnDaySelect}
    />
  );

  if (Platform.isMobile()) {
    return (
      <Dialog
        isOpened={isOpened}
        autoSizeWidth={true}
        autoSizeHeight={true}
        onCloseRequest={handleCloseRequest}
      >
        <DialogContainer>
          <DialogContent>{calendar}</DialogContent>
        </DialogContainer>
      </Dialog>
    );
  } else {
    return (
      <Popover isOpened={isOpened} {...dropdownProps}>
        <div className="date-picker-popover">{calendar}</div>
      </Popover>
    );
  }
};
