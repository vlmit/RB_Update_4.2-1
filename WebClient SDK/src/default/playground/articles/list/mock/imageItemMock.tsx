import { observer } from 'mobx-react-lite';
import classNames from 'classnames';
import { ListItemProps } from 'ui/list';

export const ImageItemMock = observer<ListItemProps>(function ImageItemMock() {
  return (
    <div className={classNames('tree__node')}>
      <img className="images-about" />
    </div>
  );
});
