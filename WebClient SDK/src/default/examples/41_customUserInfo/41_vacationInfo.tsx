import { FC } from 'react';
import { VacationInfoViewModel } from './41_vacationInfoViewModel';

export const VacationInfo: FC<{ viewModel: VacationInfoViewModel }> = ({ viewModel }) => {
  return <span className="user-info-vacation">{viewModel.text}</span>;
};
