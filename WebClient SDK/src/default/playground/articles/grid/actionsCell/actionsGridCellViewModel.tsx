import { observable, runInAction } from 'mobx';
import { List } from '@tessa/core';
import { GridCellViewModel, GridCellCreateOptions } from 'ui/grid';
import { UIButton } from 'tessa/ui/uiButton';
import { ActionCellContent } from './actionsCellContent';

export abstract class ActionsGridCellViewModel extends GridCellViewModel {
  //#region fields

  @observable.ref
  private _gap = 5;

  //#endregion

  //#region constructor

  constructor(options: GridCellCreateOptions) {
    super(options);

    this.actions = List.create<UIButton>({
      observable: true
    });

    this._contentOverride = () => <ActionCellContent viewModel={this} />;
  }

  //#endregion

  //#region properties

  readonly actions: List<UIButton>;

  get gap(): number {
    return this._gap;
  }
  set gap(value: number) {
    runInAction(() => {
      this._gap = value;
    });
  }

  //#endregion

  //#region methods

  protected override async initializeCore(): Promise<void> {
    this.initActions();

    await super.initializeCore();
  }

  protected initActions(): void {}

  //#endregion
}
