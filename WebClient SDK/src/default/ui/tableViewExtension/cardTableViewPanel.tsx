import React from 'react';
import { CardTableViewControlViewModel } from './cardTableViewControlViewModel';
import { UIButton, UIButtonComponent } from 'tessa/ui';
import { BaseViewControlItem, ViewControlPagingViewModel } from 'tessa/ui/cards/controls';
import { GridInputFilter } from 'tessa/ui/cards/components/controls';
import { PagingView } from 'tessa/ui/views/components';
import { Toolbar } from 'ui/toolbar';
import { end, filler, group } from 'ui/toolbar/helpers';
import { Button } from 'ui/button/button';
import { observer } from 'mobx-react';
import type { IToolbarGroup, IToolbarSpreadProps } from 'ui/toolbar/interfaces';
import './cardTableView.scss';
import { Visibility } from 'tessa/platform';
import { BaseContentItemHelpers } from 'tessa/ui/views/content/helpers';
import { DropdownItem } from 'ui/dropdown/dropdownItem';

export class CardTableViewPanelViewModel extends BaseViewControlItem {
  constructor(viewComponent: CardTableViewControlViewModel) {
    super(viewComponent);

    this.paging = new ViewControlPagingViewModel(viewComponent);
    this.paging.initialize();
    this.paging.rowCountVisibility = Visibility.Visible;
  }

  leftButtons: UIButton[] = [];

  rightButtons: UIButton[] = [];

  paging: ViewControlPagingViewModel;
}

export interface ICardTableViewPanelProps {
  viewModel: CardTableViewPanelViewModel;
}

interface ICardTableViewPanelState {
  expandSearch: boolean;
}

const CARD_TABLE_VIEW_TOOLBAR_SPREAD_PROPS: IToolbarSpreadProps = {
  theme: 'transparent',
  type: 'small'
};

@observer
export class CardTableViewPanel extends React.Component<
  ICardTableViewPanelProps,
  ICardTableViewPanelState
> {
  constructor(props: ICardTableViewPanelProps) {
    super(props);

    this.state = { expandSearch: false };
  }

  render(): React.ReactElement {
    const { viewModel } = this.props;
    const { leftButtons, rightButtons } = viewModel;
    const viewComponent = viewModel.viewComponent as CardTableViewControlViewModel;

    const filterItem = (
      <GridInputFilter
        key="filter"
        filterableGrid={viewComponent}
        visibility={viewComponent.getSearchBoxVisibility()}
        onFocusChange={this.handleFocusChange}
        expand={this.state.expandSearch}
        buttonType="small"
      />
    );

    const items = [
      <PagingView key="paging" viewModel={viewModel.paging} />,
      ...leftButtons.map(b => <Button key={b.name} viewModel={b} />),
      ...rightButtons.map(b => <Button key={b.name} viewModel={b} />),
      filterItem,
      <UIButtonComponent
        key={viewComponent.sortButton.name}
        viewModel={viewComponent.sortButton}
      />,
      <Button key={viewComponent.toggleButton.name} viewModel={viewComponent.toggleButton} />
    ];

    const menuItems = [
      ...leftButtons,
      ...rightButtons,
      viewComponent.sortButton,
      viewComponent.toggleButton,
      <DropdownItem type="normal" key="filter">
        {filterItem}
      </DropdownItem>,
      <PagingView key="paging" viewModel={viewModel.paging} />
    ].map((b, i) => (b instanceof UIButton ? BaseContentItemHelpers.toDropdownItem(b, i) : b));

    const left: IToolbarGroup[] = leftButtons.length
      ? leftButtons.map(b => group('left ' + b.name, [b.name]))
      : [];

    const rightPositionGroup: IToolbarGroup[] = [];

    if (viewComponent.sortButton.visibility === Visibility.Visible) {
      rightPositionGroup.push(group('sort', [viewComponent.sortButton.name]));
    }

    if (viewComponent.toggleButton.visibility === Visibility.Visible) {
      rightPositionGroup.push(group('toggleButton', [viewComponent.toggleButton.name]));
    }

    const pagingGroup = group('paging', ['paging'], { align: 'end', crossAlign: 'center' });

    const filter = end(
      group('filter', ['filter'], { stretch: this.state.expandSearch, isPermanent: true })
    );
    const right: IToolbarGroup[] = [];
    for (const b of rightButtons) {
      right.push(group('right ' + b.name, [b.name]));
    }

    const groups: IToolbarGroup[] = [
      ...left,
      filler('filler-paging-left'),
      filter,
      ...right,
      ...rightPositionGroup,
      pagingGroup
    ];

    return (
      <div className="tableControlButtons card-table-view-footer-toolbar">
        <Toolbar
          items={items}
          menuItems={menuItems}
          groups={groups}
          spacing={10}
          overflow="spread"
          spreadProps={CARD_TABLE_VIEW_TOOLBAR_SPREAD_PROPS}
        />
      </div>
    );
  }

  private handleFocusChange = (isFocused: boolean): void => {
    this.setState({ expandSearch: isFocused });
  };
}
