import { observer } from 'mobx-react-lite';
import { forceBreakLongWord } from 'common';
import { maxWordLength } from 'components/cardElements/grid';
import { UserAvatarTableCellViewModel } from './userAvatarTableCellViewModel';
import { Avatar } from 'ui/avatar/avatar';
import './userAvatarCellContent.scss';

type UserAvatarCellContentProps = {
  viewModel: UserAvatarTableCellViewModel;
};

export const UserAvatarCellContent = observer<UserAvatarCellContentProps>(
  function UserAvatarCellContent({ viewModel }) {
    return (
      <div className="user-avatar-table-cell">
        <Avatar viewModel={viewModel.avatar} />
        {viewModel.content && forceBreakLongWord(viewModel.content, maxWordLength)}
      </div>
    );
  }
);
