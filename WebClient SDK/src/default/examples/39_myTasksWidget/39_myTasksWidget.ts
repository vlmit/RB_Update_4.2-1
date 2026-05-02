import { computed, observable, runInAction } from 'mobx';
import { localize } from '@tessa/application';
import {
  IViewRepository,
  SchemeType,
  ViewCriteriaOperators,
  ViewParameterMetadata,
  ViewRequest,
  ViewRequestParameterBuilder
} from '@tessa/platform';
import { showView } from 'tessa/ui/uiHost';
import {
  DashboardLayoutValue,
  DashboardViewModel,
  DashboardWidgetPeriodicRefresher,
  DashboardWidgetSize,
  DashboardWidgetSizeLimits,
  DashboardWidgetConfigurationStorage
} from 'tessa/ui/dashboard';
import { ButtonWidget } from '../../dashboard/widgets/button/buttonWidget';
import { MyTasksWidgetSettings } from './39_myTasksWidgetSettings';

/** Виджет "Мои задания". */
export class MyTasksWidget extends ButtonWidget<MyTasksWidgetSettings> {
  //#region constructors

  constructor(
    viewRepository: IViewRepository,
    dashboard: DashboardViewModel,
    id?: string,
    settings = new MyTasksWidgetSettings()
  ) {
    super(
      'MyTasks',
      settings,
      dashboard,
      id,
      MyTasksWidget.initialWidgetSize,
      MyTasksWidget.widgetSizeLimits
    );

    this._viewRepository = viewRepository;

    this.periodicRefresher = new DashboardWidgetPeriodicRefresher(
      this.dashboard,
      this._refreshPeriod,
      async () => await this.updateTaskCount()
    );
  }

  //#endregion

  //#region fields

  private _lastTaskTypeId: string | null = null;

  private readonly _viewRepository: IViewRepository;

  @observable.ref
  private _tasksCount: number | null = null;

  private _refreshPeriod = 15 * 60 * 1000;

  //#endregion

  //#region properties

  @computed
  get caption(): string {
    const taskCaption = localize(this.settings.taskType?.caption);
    if (!taskCaption) {
      return '';
    }

    return `${taskCaption} - ${this._tasksCount ?? 0}`;
  }

  readonly periodicRefresher: DashboardWidgetPeriodicRefresher;

  //#endregion

  //#region base overrides

  protected override async initializeCore(): Promise<void> {
    await super.initializeCore();

    // Стартуем подписку на активацию хоста, чтобы выполнить рефреш
    this.periodicRefresher?.start(true);
  }

  protected override disposeCore(): void {
    this.periodicRefresher?.dispose();

    super.disposeCore();
  }

  //#endregion

  //#region static

  static initialWidgetSize: DashboardLayoutValue<DashboardWidgetSize> = {
    // Для всех брейкпоинтов до lg кнопка будет добавляться с размером шириной в 4 колонки,
    // начиная с xs и больше - в 3 колонок
    xxs: {
      columnsCount: 4,
      rowsCount: 1
    },
    xs: {
      columnsCount: 3,
      rowsCount: 1
    }
  };

  static widgetSizeLimits: DashboardLayoutValue<DashboardWidgetSizeLimits> = {
    // Для всех брейкпоинтов минимальные размера 2 колонки и 1 строка
    xxs: {
      minColumnsCount: 2,
      minRowsCount: 1
    }
  };

  //#endregion

  //#region methods

  protected override async handleClickCore(): Promise<void> {
    if (!this.settings.taskType) {
      return;
    }

    const parameterMetadata = new ViewParameterMetadata();
    parameterMetadata.alias = 'TaskType';
    parameterMetadata.caption = 'TaskType';
    parameterMetadata.hidden = true;
    parameterMetadata.schemeType = SchemeType.Guid;
    parameterMetadata.multiple = false;

    const parameters = [
      new ViewRequestParameterBuilder()
        .withMetadata(parameterMetadata)
        .addCriteria(
          ViewCriteriaOperators.EqualsTo,
          this.settings.taskType.caption,
          this.settings.taskType.id
        )
        .asRequestParameter()
    ];

    await showView({
      viewAlias: 'MyTasks',
      parameters,
      displayValue: '$Workplaces_User_MyTasks'
    });
  }

  protected override async beforeApplyConfiguration(
    _configuration: DashboardWidgetConfigurationStorage
  ): Promise<void> {
    // До того, как применяется новые настройки, сохраним текущий тип задания
    this._lastTaskTypeId = this.settings.taskType?.id ?? null;
  }

  protected override async afterApplyConfiguration(
    _configuration: DashboardWidgetConfigurationStorage
  ): Promise<void> {
    // Если в новых настройках поменялся тип задания, обновим счетчик
    if (this.settings.taskType?.id !== this._lastTaskTypeId) {
      await this.updateTaskCount();
      // Обновляем время последнего рефреша
      this.periodicRefresher?.setLastRefresh(Date.now());
    }
  }

  /** Получает количество заданий типа, указанного в настройках, из представления "Мои задания". */
  private async updateTaskCount(): Promise<void> {
    if (!this.settings.taskType) {
      return;
    }

    const view = await this._viewRepository.getByName('MyTasks');
    if (!view) {
      return;
    }

    const metadata = await view.getMetadata();
    const request = new ViewRequest(metadata);

    request.addParameter(builder =>
      builder
        .withMetadata(metadata.parameters.get('TaskType')!)
        .addCriteria(
          ViewCriteriaOperators.EqualsTo,
          this.settings.taskType!.caption,
          this.settings.taskType!.id
        )
        .asRequestParameter()
    );

    request.subsetName = 'Count';

    const result = await view.getData(request);
    if (result.rows.length === 0) {
      return;
    }

    const count = result.rows[0][0] as number;
    runInAction(() => {
      this._tasksCount = count;
    });
  }

  //#endregion
}
