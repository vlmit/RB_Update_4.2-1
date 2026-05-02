import { MouseEvent, useCallback, useRef } from 'react';
import { observer } from 'mobx-react-lite';
import classNames from 'classnames';
import { useLocalize, useViewModel } from '@tessa/ui';
import { SearchBox } from 'ui/searchBox/searchBox';
import { CardTypeSelectorViewModel } from './cardTypeSelectorViewModel';
import { ICardTypeSelectorNode, ICardTypeSelectorNodeGroup } from './cardTypeSelectorTypes';
import './cardTypeSelector.scss';

type CardTypeSelectorProps = {
  viewModel: CardTypeSelectorViewModel;
};

export const CardTypeSelector = observer<CardTypeSelectorProps>(function CardTypeSelector({
  viewModel
}) {
  const ref = useRef<HTMLDivElement>(null);

  useViewModel(viewModel, ref);

  const groups = viewModel.groups.map(group => {
    return <CardTypeSelectorGroupNode viewModel={group} key={group.id} />;
  });

  return (
    <div className="dashboard-card-type-selector-container" ref={ref}>
      <SearchBox viewModel={viewModel.searchBox} />
      <div className="dashboard-card-type-selector-list">{groups}</div>
    </div>
  );
});

type CardTypeSelectorGroupNodeProps = {
  viewModel: ICardTypeSelectorNodeGroup;
};

const CardTypeSelectorGroupNode = observer<CardTypeSelectorGroupNodeProps>(
  function CardTypeSelectorGroupNode({ viewModel }) {
    const children = viewModel.children.map(child => {
      return <CardTypeSelectorLeafNode viewModel={child} key={child.id} />;
    });

    const caption = useLocalize(viewModel.caption);

    const onClick = useCallback(
      (e: MouseEvent) => {
        viewModel.handleClick(e);
      },
      [viewModel]
    );

    const onDoubleClick = useCallback(
      (e: MouseEvent) => {
        viewModel.handleDoubleClick(e);
      },
      [viewModel]
    );

    return (
      <div className="dashboard-card-type-selector-group">
        <div
          className="dashboard-card-type-selector-group__header"
          onClick={onClick}
          onDoubleClick={onDoubleClick}
        >
          {caption}
        </div>
        <div className="dashboard-card-type-selector-group__list">{children}</div>
      </div>
    );
  }
);

type CardTypeSelectorLeftNodeProps = {
  viewModel: ICardTypeSelectorNode;
};

const CardTypeSelectorLeafNode = observer<CardTypeSelectorLeftNodeProps>(
  function CardTypeSelectorLeafNode({ viewModel }) {
    const caption = useLocalize(viewModel.caption);

    const onClick = useCallback(
      (e: MouseEvent) => {
        viewModel.handleClick(e);
      },
      [viewModel]
    );

    const onDoubleClick = useCallback(
      (e: MouseEvent) => {
        viewModel.handleDoubleClick(e);
      },
      [viewModel]
    );

    return (
      <div
        onClick={onClick}
        onDoubleClick={onDoubleClick}
        className={classNames('dashboard-card-type-selector-item', {
          selected: viewModel.selected
        })}
      >
        <span>{caption}</span>
      </div>
    );
  }
);
