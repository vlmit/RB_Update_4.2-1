import React from 'react';
import { observable, runInAction } from 'mobx';
import { CardTableViewControlViewModel } from './cardTableViewControlViewModel';
import { ICardModel } from 'tessa/ui/cards';
import {
  BaseViewControlItem,
  IGridFormContainer,
  ViewControlViewModel
} from 'tessa/ui/cards/controls';
import { GridRowFormDialog } from 'tessa/ui/cards/components/controls';
import { Visibility } from 'tessa/platform';

export class CardTableViewFormContainerViewModel
  extends BaseViewControlItem
  implements IGridFormContainer
{
  //#region ctor

  constructor(viewComponent: CardTableViewControlViewModel) {
    super(viewComponent);
  }

  //#endregion

  //#region fields

  @observable
  private _isRowFormOpened: boolean;

  @observable
  private _dialogTitle: string;

  //#endregion

  //#region props

  get cardModel(): ICardModel {
    return (this.viewComponent as ViewControlViewModel).cardModel;
  }

  get isRowFormOpened(): boolean {
    return this._isRowFormOpened;
  }

  get visibility(): Visibility {
    return this._isRowFormOpened ? Visibility.Visible : Visibility.Collapsed;
  }

  get dialogTitle(): string {
    return this._dialogTitle;
  }
  set dialogTitle(value: string) {
    runInAction(() => {
      this._dialogTitle = value;
    });
  }

  //#endregion

  //#region methods

  openForm(): void {
    runInAction(() => (this._isRowFormOpened = true));
  }

  closeForm(): void {
    runInAction(() => (this._isRowFormOpened = false));
  }

  //#endregion
}

export interface CardTableViewFormContainerProps {
  viewModel: CardTableViewFormContainerViewModel;
}

export class CardTableViewFormContainer extends React.Component<CardTableViewFormContainerProps> {
  render(): React.ReactElement {
    const { viewModel } = this.props;

    return <GridRowFormDialog viewModel={viewModel} />;
  }
}
