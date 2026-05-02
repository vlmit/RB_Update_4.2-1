import React from 'react';
import {
  useVirtualUserInfoPopoverViewModel,
  useVirtualUserInfoViewModel
} from 'ui/userInfo/useVirtualUserInfoViewModel';
import { UserInfoViewer } from 'ui/userInfo/userInfoViewer';
import { useBirthdayDate } from './useBirthdayDate';
import { IUserBirthdayInfo } from './birthdayTypes';
import { UserInfoTooltip } from 'ui/userInfo/userInfoTooltip';

export interface BirthdayInfoComponentProps {
  info: IUserBirthdayInfo;
}

export const BirthdayInfoComponent: React.FC<BirthdayInfoComponentProps> = props => {
  const { info } = props;

  const birthdayDate = useBirthdayDate(props);

  const userInfoViewModel = useVirtualUserInfoViewModel(info.user, null, vm => {
    vm.size = 'compact';
    vm.avatar.size = 'semi-md';
    vm.showContacts = false;
  });

  const popoverInfoViewModel = useVirtualUserInfoPopoverViewModel(info.user);

  return (
    <UserInfoTooltip viewModel={popoverInfoViewModel}>
      <div className="birthday-user">
        <UserInfoViewer viewModel={userInfoViewModel} />
        {birthdayDate}
      </div>
    </UserInfoTooltip>
  );
};
