import { IObservableValue, observable, runInAction } from 'mobx';
import {
  CardTaskState,
  IViewRepository,
  ViewRequest,
  ViewRequestParameter,
  DbTaskState
} from '@tessa/platform';
import { localize } from '@tessa/application';
import { Visibility } from 'tessa/platform/visibility';
import { UIButton } from 'tessa/ui/uiButton';
import { ContainerDashboardWidgetSettingsBase, DashboardWidgetHeader } from 'tessa/ui/dashboard';
import { TasksWidgetViewHelper } from '../view/tasksWidgetViewHelper';

type StateInfo = { name: string; counter: IObservableValue<number> };

export class TasksWidgetHeader extends DashboardWidgetHeader {
  //#region constructors

  constructor(
    private readonly _onButtonClickAction: (state: CardTaskState, include: boolean) => void,
    private readonly _viewRepository: IViewRepository,
    settings: ContainerDashboardWidgetSettingsBase,
    taskStates: DbTaskState[]
  ) {
    super(settings);
    this._statesInfo = new Map(
      taskStates.map(x => [x.id, { name: localize(x.name), counter: observable.box(0) }])
    );
  }

  //#endregion

  //#region fields

  private readonly _statesInfo: ReadonlyMap<CardTaskState, StateInfo>;

  //#endregion

  //#region public methods

  async updateState(...parameters: ViewRequestParameter[]): Promise<void> {
    const view = await this._viewRepository.getByName(TasksWidgetViewHelper.ViewAlias);
    if (!view) {
      this.invalidateState();
      return;
    }

    const metadata = await view.getMetadata();
    const request = new ViewRequest(metadata);
    request.parameters.push(...parameters);
    request.subsetName = TasksWidgetViewHelper.ByStateSubsetName;

    const result = await view.getData(request);
    const updated: StateInfo[] = [];

    runInAction(() => {
      for (const row of result.getRowsAsMap()) {
        const id = row.getNumber(TasksWidgetViewHelper.ByStateSubsetColumnIdName);
        const count = row.getNumber(TasksWidgetViewHelper.ByStateSubsetColumnCountName);
        if (id !== null && count !== null) {
          const info = this._statesInfo.get(id);
          if (info) {
            info.counter.set(count);
            updated.push(info);
          }
        }
      }
    });

    this.invalidateState(updated);
  }

  //#endregion

  //#region base overrides

  protected override async initializeCore(): Promise<void> {
    await super.initializeCore();

    for (const [id, info] of this._statesInfo) {
      this.buttons.push(
        UIButton.create({
          name: `${id}`,
          type: 'small',
          theme: 'control',
          caption: () => `${info.name} ${info.counter}`,
          visibility: () => (info.counter.get() > 0 ? Visibility.Visible : Visibility.Collapsed),
          buttonAction: async button => {
            button.isActive = !button.isActive;
            button.theme = button.isActive ? 'primary' : 'control';
            this._onButtonClickAction(id, button.isActive);
          }
        })
      );
    }
  }

  //#endregion

  //#region private methods

  private invalidateState(updated?: StateInfo[]): void {
    runInAction(() => {
      for (const info of this._statesInfo.values()) {
        if (!updated || !updated.includes(info)) {
          info.counter.set(0);
        }
      }
    });
  }

  //#endregion
}
