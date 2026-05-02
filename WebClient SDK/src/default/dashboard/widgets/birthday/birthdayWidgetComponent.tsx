import React from 'react';
import { observer } from 'mobx-react-lite';
import { localize } from '@tessa/application';
import { BirthdayWidget } from './birthdayWidget';
import { BirthdayGroupComponent } from './birthdayGroupComponent';
import './birthdayStyle.scss';

export interface BirthdayWidgetComponentProps {
  viewModel: BirthdayWidget;
}

/** Компонент виджета "Дни рождения". */
export const BirthdayWidgetComponent: React.FC<BirthdayWidgetComponentProps> = observer(props => {
  const { viewModel } = props;

  if (!viewModel.birthdayGroups.some(group => !!group.birthdays.length)) {
    return (
      <div className="birthday-container">
        <div className="empty-groups">{localize('$Dashboard_Widget_Birthday_NoDates')}</div>
      </div>
    );
  }

  const groups = viewModel.birthdayGroups.map(group => {
    return <BirthdayGroupComponent key={group.name} group={group} />;
  });

  return <div className="birthday-container">{groups}</div>;
});
