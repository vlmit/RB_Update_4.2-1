import { useCallback, useEffect, useRef, useState } from 'react';
import { observer } from 'mobx-react-lite';
import classNames from 'classnames';
import { Icon } from 'ui/icon/icon';
import { IBirthdayGroup } from './birthdayTypes';
import { localize } from '@tessa/application';
import { BirthdayInfoComponent } from './birthdayInfoComponent';

export interface BirthdayGroupComponentProps {
  group: IBirthdayGroup;
}

export const BirthdayGroupComponent = observer<BirthdayGroupComponentProps>(
  function BirthdayGroupComponent({ group }) {
    const ref = useRef<HTMLDivElement>(null);
    const [height, setHeight] = useState<number>();

    const className = classNames('birthday-group', {
      collapsed: !group.expanded
    });

    const toggleCollapse = useCallback(() => {
      group.expanded = !group.expanded;
    }, [group]);

    useEffect(() => {
      requestAnimationFrame(() => {
        if (group.expanded) {
          setHeight(ref.current?.scrollHeight);
        } else {
          setHeight(0);
        }
      });
    }, [group.expanded]);

    const birthdays = group.birthdays.map(birthday => (
      <BirthdayInfoComponent key={birthday.user.id} info={birthday} />
    ));

    if (!group.birthdays.length) {
      return null;
    }

    return (
      <div className={className}>
        <div className="group-header" onMouseDown={toggleCollapse}>
          <div className="group-title">
            <span className="title-name">{localize(group.title)}</span>
            <span className="title-count">{group.birthdays.length}</span>
          </div>
          <Icon icon={group.expanded ? 'm-up' : 'm-drop'} size="m" />
        </div>
        <div ref={ref} className="group-items" style={{ height }}>
          {birthdays}
        </div>
      </div>
    );
  }
);
