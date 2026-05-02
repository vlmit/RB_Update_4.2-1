import React from 'react';
import moment from 'moment';
import { StringHelper } from '@tessa/core';
import { BirthdayInfoComponentProps } from './birthdayInfoComponent';

export const useBirthdayDate = (props: BirthdayInfoComponentProps): React.ReactElement | null => {
  const { info } = props;

  if (!info.birthday) {
    return null;
  }

  const date = moment(info.birthday);
  const day = (`0` + date.format('DD')).slice(-2);
  const month = StringHelper.capitalize(date.format('MMMM').slice(0, 3));

  return (
    <div className="user-birthday">
      <div className="birthday-date">{day}</div>
      <div className="birthday-month">{month}</div>
    </div>
  );
};
