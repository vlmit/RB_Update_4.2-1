import { computed, observable, runInAction } from 'mobx';
import { debounce, EventHandler } from '@tessa/core';
import { localize } from '@tessa/application';
import { InitializableViewModelBase } from '@tessa/ui';
import { SearchBoxViewModel } from 'ui/searchBox/searchBoxViewModel';
import { CardTypeSelectorNodeViewModel } from './cardTypeSelectorNodeViewModel';
import { CardTypeSelectorNodeGroupViewModel } from './cardTypeSelectorNodeGroupViewModel';
import {
  CardTypeSelectorNodeClickEventArgs,
  ICardTypeSelectorNode,
  ICardTypeSelectorNodeBase,
  ICardTypeSelectorNodeGroup,
  ICardTypeSelectorTypesProvider,
  isNodeLeafNode
} from './cardTypeSelectorTypes';

export class CardTypeSelectorViewModel extends InitializableViewModelBase {
  //#region fields

  private readonly _typesProvider: ICardTypeSelectorTypesProvider;

  private _groups: ICardTypeSelectorNodeGroup[];

  @observable.ref
  private _selectedNode: ICardTypeSelectorNode | null;

  private _searchBox!: SearchBoxViewModel;

  @observable.ref
  private _search: string = '';

  private _filterNodes!: () => void;

  //#endregion

  //#region ctor

  constructor(typesProvider: ICardTypeSelectorTypesProvider) {
    super();

    this._typesProvider = typesProvider;

    this.onNodeClick = new EventHandler(this);
    this.onNodeDoubleClick = new EventHandler(this);
  }

  //#endregion

  //#region props

  @computed
  get groups(): ICardTypeSelectorNodeGroup[] {
    if (this._search.length === 0) {
      return this._groups;
    }

    return this._groups.filter(node => node.children.length > 0);
  }

  get selectedNode(): ICardTypeSelectorNode | null {
    return this._selectedNode;
  }
  set selectedNode(value: ICardTypeSelectorNode | null) {
    runInAction(() => {
      this._selectedNode = value;
    });
  }

  get searchBox(): SearchBoxViewModel {
    return this._searchBox;
  }

  get search(): string {
    return this._search;
  }

  private onSearchValueChange = (value: string): void => {
    this._searchBox.value = value;
    this.refreshNodesWithDebounce();
  };

  //#endregion

  //#region InitializableViewModelBase

  protected override async initializeCore(): Promise<void> {
    this._groups = await this.getTypesGroups();

    this._filterNodes = debounce(() => this.refreshNodes(), 200);

    this._searchBox = new SearchBoxViewModel('CardTypeSelectorSearch');
    this._searchBox.placeholder = '$Views_QuickSearch_Placeholder';
    this._searchBox.spyglass = 'icon';
    this._searchBox.onChange = this.onSearchValueChange;
  }

  protected override disposeCore(): void {
    this.onNodeClick.dispose();
    this.onNodeDoubleClick.dispose();
  }

  //#endregion

  //#region methods

  handleNodeClick(node: ICardTypeSelectorNodeBase): void {
    if (isNodeLeafNode(node)) {
      this.selectedNode = this.selectedNode === node ? null : node;

      this.onNodeClick.invoke({
        node: node
      });
    }
  }

  handleNodeDoubleClick(node: ICardTypeSelectorNodeBase): void {
    if (isNodeLeafNode(node)) {
      this.selectedNode = node;

      this.onNodeDoubleClick.invoke({
        node: node
      });
    }
  }

  refreshNodes(): void {
    runInAction(() => {
      this._search = this._searchBox.value;
    });
  }

  refreshNodesWithDebounce(): void {
    this._filterNodes();
  }

  private async getTypesGroups(): Promise<ICardTypeSelectorNodeGroup[]> {
    const result: ICardTypeSelectorNodeGroup[] = [];
    const types = await this._typesProvider.getTypes();

    for (const group of types) {
      const groupNode = new CardTypeSelectorNodeGroupViewModel(
        this,
        group.groupInfo.name,
        group.groupInfo.caption
      );

      groupNode.children = group.types.map(type => new CardTypeSelectorNodeViewModel(this, type));
      groupNode.children.sort((a, b) => localize(a.caption).localeCompare(localize(b.caption)));

      result.push(groupNode);
    }

    result.sort((a, b) => localize(a.caption).localeCompare(localize(b.caption)));

    return result;
  }

  //#endregion

  //#region events

  readonly onNodeClick: EventHandler<CardTypeSelectorNodeClickEventArgs, CardTypeSelectorViewModel>;

  readonly onNodeDoubleClick: EventHandler<
    CardTypeSelectorNodeClickEventArgs,
    CardTypeSelectorViewModel
  >;

  //#endregion
}
