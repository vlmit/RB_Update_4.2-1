import React from 'react';
import { observer } from 'mobx-react-lite';
import { useWidgetHeaderButtons, useWidgetHeaderCaption } from 'tessa/ui/dashboard';
import { TasksWidgetHeader } from './tasksWidgetHeader';
import './tasksWidgetHeaderStyle.scss';

export const TasksWidgetHeaderComponent: React.FC<{ viewModel: TasksWidgetHeader }> = observer(
  ({ viewModel }) => {
    const caption = useWidgetHeaderCaption(viewModel);
    const buttons = useWidgetHeaderButtons(viewModel);

    return (
      <div className="widget-header tasks-widget-header">
        {caption}
        {buttons}
      </div>
    );
  }
);
