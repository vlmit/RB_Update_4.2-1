import type { SyntheticEvent, KeyboardEvent } from 'react';
import { IGridCellViewModel, IGridRowViewModel } from 'components/cardElements/grid';
import { localize } from 'tessa/localization';
import { StageGroup } from './stageGroup';
import { stageSelectorColumnName } from './stageSelectorDialog';
import { StageSelectorViewModel } from './stageSelectorViewModel';
import { StageType } from './stageType';

export class StageDialogRowAdapter implements IGridRowViewModel {
  constructor(
    model: StageType | StageGroup,
    viewModel: StageSelectorViewModel,
    id: string,
    handleCloseFormWithResult?: IGridRowViewModel['onDoubleClick']
  ) {
    this._viewModel = viewModel;
    this._id = id;
    this._handleCloseFormWithResult = handleCloseFormWithResult;
    this._type = model instanceof StageType ? 'type' : 'group';

    const content = model instanceof StageType ? model.caption : model.name;

    this._cells = [
      {
        parent: this,
        content: localize(content),
        columnId: stageSelectorColumnName
      }
    ];
  }

  private _viewModel: StageSelectorViewModel;
  private _id: string;
  private _type: 'group' | 'type';
  private _cells: IGridCellViewModel[];
  private _handleCloseFormWithResult?: IGridRowViewModel['onDoubleClick'];

  public get id(): string {
    return this._id;
  }

  public get cells(): IGridCellViewModel[] {
    return this._cells;
  }

  public get showChildren(): boolean {
    return true;
  }

  public get showOverflow(): boolean {
    return true;
  }

  public get isSelected(): boolean {
    return this._type === 'group'
      ? this._viewModel.selectedGroup?.id === this._id
      : this._viewModel.selectedType?.id === this._id;
  }
  public set isSelected(value: boolean) {
    if (!value) {
      return;
    }

    const { _viewModel } = this;
    if (this._type === 'group') {
      _viewModel.setSelectedGroupIndex(_viewModel.groups.findIndex(g => g.id === this.id));
      _viewModel.updateType();
    } else {
      _viewModel.setSelectedTypeIndex(_viewModel.types.findIndex(g => g.id === this.id));
    }
  }

  public get isLastSelected(): boolean {
    return this._type === 'group'
      ? this._viewModel.selectedGroup?.id === this._id
      : this._viewModel.selectedType?.id === this._id;
  }

  onDoubleClick(
    e: SyntheticEvent<Element, Event> | KeyboardEvent<Element>,
    columnIndex?: number
  ): void {
    if (this._type === 'type') {
      this._handleCloseFormWithResult?.(e, columnIndex);
    }
  }
}
