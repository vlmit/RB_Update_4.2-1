import React from 'react';
import { observer } from 'mobx-react-lite';
import { SearchBox } from 'ui/searchBox/searchBox';
import { UIButtonComponent } from 'tessa/ui/uiButton';
import { UserInfoComponent } from './userInfoComponent';
import { UsersWidget } from './usersWidget';
import './usersStyle.scss';

/** Компонент виджета "Справочник сотрудников". */
export const UsersWidgetComponent: React.FC<{ viewModel: UsersWidget }> = observer(
  ({ viewModel }) => {
    const search = useSearch(viewModel);
    const users = useUsers(viewModel);
    const overflow = useOverflow(viewModel);

    return (
      <div className="users-wrapper">
        {search}
        {users}
        {overflow}
      </div>
    );
  }
);

function useSearch(viewModel: UsersWidget): React.ReactElement | undefined {
  return (
    <div className="users-search">
      <SearchBox viewModel={viewModel.search} />
    </div>
  );
}

function useUsers(viewModel: UsersWidget): React.ReactElement | undefined {
  if (viewModel.message) {
    return <div className="empty">{viewModel.message}</div>;
  } else if (viewModel.users.length > 0) {
    const items = viewModel.users.map(user => <UserInfoComponent key={user.id} viewModel={user} />);
    return <div className="users-container">{items}</div>;
  } else {
    return;
  }
}

function useOverflow(viewModel: UsersWidget): React.ReactElement | undefined {
  return (
    <div className="users-overflow">
      <UIButtonComponent viewModel={viewModel.overflow} />
    </div>
  );
}
