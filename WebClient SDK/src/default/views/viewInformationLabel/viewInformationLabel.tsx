import * as React from 'react';
import { ControlContainerProps } from 'ui/controlContainer/definitions';
import { ViewInformationLabelViewModel } from './viewInformationLabelViewModel';
import { observer } from 'mobx-react-lite';
import { Label } from 'ui';

export interface ViewInformationLabelProps extends ControlContainerProps {
  viewModel: ViewInformationLabelViewModel;
}

export const ViewInformationLabel: React.FC<ViewInformationLabelProps> = observer(props => {
  return <Label viewModel={props.viewModel.label} />;
});
