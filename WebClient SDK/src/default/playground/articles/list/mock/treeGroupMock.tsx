import { FC, MouseEvent, ReactElement, useCallback } from 'react';
import classNames from 'classnames';
import { runInAction } from 'mobx';
import { observer } from 'mobx-react-lite';
import { Icon } from 'ui/icon/icon';
import { ListGroupProps, ListItemProps } from 'ui/list';

export const TreeGroupMock = observer<ListGroupProps>(function TreeGroupMock(props) {
  const { depth = 1, group, render, paths } = props;
  const { icon, caption, isExpanded } = group;
  const { items } = props.listProps;

  const handleClick = useCallback(
    (e: MouseEvent) => {
      runInAction(() => {
        e.preventDefault();
        group.isExpanded = !group.isExpanded;
      });
    },
    [group]
  );

  const children = () => {
    if (!isExpanded) {
      return undefined;
    }

    const content: ReactElement[] = [];

    for (const c of group.children) {
      if (typeof c === 'string') {
        const i = items.find(i => i.name === c);
        if (!i) {
          continue;
        }
        const Component = render('item', i.type) as FC<ListItemProps>;
        content.push(
          <Component
            key={'item-' + i.name}
            item={i}
            group={group}
            paths={paths}
            listProps={props.listProps}
          />
        );
      } else {
        const Component = render('group', c.type) as FC<ListGroupProps>;
        content.push(<Component key={group.name} {...props} depth={depth + 1} group={c} />);
      }
    }
    return <div className="children">{content}</div>;
  };

  return (
    <div className={classNames('tree__node')}>
      <div className="tree__node-header" onClick={handleClick}>
        <button className={classNames('tree__expand-toggle', 'visible')}>
          <Icon
            icon="icon-Int1362"
            size="s"
            className={classNames('tree__expand-icon', { expanded: isExpanded })}
          />
        </button>
        {icon && <Icon icon={icon} size="m" />}
        <span className="tree__caption">{caption}</span>
      </div>
      {children}
    </div>
  );
});
