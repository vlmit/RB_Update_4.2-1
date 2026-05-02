import { MouseEvent, useCallback, useMemo, useRef } from 'react';
import { observer } from 'mobx-react-lite';
import classNames from 'classnames';
import { runInAction } from 'mobx';
import { MenuAction } from 'tessa/ui';
import { useDropdownMenu } from 'ui/dropdown/useDropdown';
import { Icon } from 'ui/icon/icon';
import { ListItemProps } from 'ui/list';

export const TreeItemMock = observer<ListItemProps>(function TreeItemMock(props) {
  const { item } = props;
  const { icon, caption, isSelected } = item;
  const rootRef = useRef<HTMLDivElement>(null);

  const actions = useMemo(() => {
    if (!(item.actions?.[0] instanceof MenuAction)) {
      return [];
    }

    return item.actions as MenuAction[];
  }, []);

  const { element, toggle } = useDropdownMenu(actions, {
    rootElement: rootRef.current,
    openPosition: 'right'
  });

  const handleClick = useCallback(
    (e: MouseEvent) => {
      runInAction(() => {
        e.preventDefault();
        props.listProps.onSelectionChange(item);
      });
    },
    [props.listProps, item]
  );

  const handleContextMenu = useCallback(
    (e: MouseEvent) => {
      e.preventDefault();
      toggle();
    },
    [toggle]
  );

  return (
    <div className={classNames('tree__node')} ref={rootRef}>
      <div
        className={classNames('tree__node-header item', { selected: isSelected })}
        onClick={handleClick}
        onContextMenu={handleContextMenu}
      >
        {icon && <Icon icon={icon} size="m" />}
        <span className="tree__caption">{caption}</span>
        {element}
      </div>
    </div>
  );
});
