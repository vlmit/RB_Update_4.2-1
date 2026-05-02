import React from 'react';
import { IUserInfo } from '@tessa/platform';
import { UserInfoViewer } from 'ui/userInfo/userInfoViewer';
import { UserInfoTooltip } from 'ui/userInfo/userInfoTooltip';
import {
  useVirtualUserInfoPopoverViewModel,
  useVirtualUserInfoViewModel
} from 'ui/userInfo/useVirtualUserInfoViewModel';

/** Компонент элемента виджета "Справочник сотрудников". */
export const UserInfoComponent: React.FC<{ viewModel: IUserInfo }> = ({ viewModel }) => {
  const popoverInfoViewModel = useVirtualUserInfoPopoverViewModel(viewModel);
  const userInfoViewModel = useVirtualUserInfoViewModel(viewModel, null, vm => {
    vm.size = 'compact';
    vm.avatar.size = 'semi-md';
    vm.showContacts = false;
  });

  return (
    <UserInfoTooltip viewModel={popoverInfoViewModel}>
      <div className="user">
        <UserInfoViewer viewModel={userInfoViewModel} />
      </div>
    </UserInfoTooltip>
  );
};
