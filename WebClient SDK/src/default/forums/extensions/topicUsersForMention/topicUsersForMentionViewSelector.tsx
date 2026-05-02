import { observer } from 'mobx-react-lite';
import { Select } from 'ui';
import { Themes, useLocalTheme } from 'ui/internal/safeImport';
import { TopicUsersForMentionViewSelectorViewModel } from './topicUsersForMentionViewSelectorViewModel';
import './topicUsersForMentionViewSelector.scss';

const theme = {
  padding: '5 3 5 8',
  font: {
    size: '$fontSizePrimary',
    height: '$fontSizeBase'
  }
};

export const TopicUsersForMentionViewSelector = observer<{
  viewModel: TopicUsersForMentionViewSelectorViewModel;
}>(function TopicUsersForMentionViewSelector({ viewModel }) {
  const style = useLocalTheme(Themes.current, theme);

  // TODO: перевести на selectType="toolbar" (пока непонятно, почему в этом диалоге этот режим не работает так, как должен)

  return (
    <div className="topic-users-selector" style={style}>
      <Select
        border="none"
        selectType="compact"
        value={viewModel.viewType}
        values={viewModel.getMenuActions()}
        className={viewModel.className.result}
      />
    </div>
  );
});
