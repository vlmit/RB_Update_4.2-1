import { observer } from 'mobx-react-lite';
import React from 'react';
import { localize } from '@tessa/application';
import { ViewControl } from 'tessa/ui/cards/components/controls';
import { ViewWidget } from './viewWidget';
import './viewWidgetStyles.scss';

export const ViewWidgetComponent: React.FC<{ viewModel: ViewWidget }> = observer(
  ({ viewModel }) => {
    if (viewModel.error) {
      return <span className="view-widget-error">{localize(viewModel.error)}</span>;
    }

    return (
      <div className="view-widget-container">
        {viewModel.control ? <ViewControl viewModel={viewModel.control} /> : null}
      </div>
    );
  }
);
