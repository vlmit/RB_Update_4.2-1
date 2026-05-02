import moment from 'moment';
import { Guid, StorageAccessor } from '@tessa/core';
import {
  CardTaskState,
  IViewRepository,
  ViewCriteriaOperators,
  DbTaskState
} from '@tessa/platform';
import { localize } from '@tessa/application';
import { IAvatarViewModelFactory } from 'ui/avatar/avatarTypes';
import { DefaultAvatarDataSource } from 'ui/avatar/defaultAvatarDataSource';
import { AvatarViewModel } from 'ui/avatar/avatarViewModel';
import { RoleHelper } from 'tessa/roles';
import {
  IViewControlInitializationStrategy,
  ViewControlViewModelBase
} from 'tessa/ui/cards/controls';
import { DashboardViewModel } from 'tessa/ui/dashboard';
import { ColumnsSettings } from 'tessa/ui/views/settings/columnSettings';
import { CardControlTypes, CardTypeEntryControl } from 'tessa/cards/types';
import { UserAvatarTableCellViewModel } from '../../../views/userAvatar/userAvatarTableCellViewModel';
import { ViewWidgetSettings } from '../view/viewWidgetSettings';
import { ViewWidgetBase } from '../view/viewWidgetBase';
import { DefaultWidgetNames } from '../widgetNames';
import { TasksWidgetCompletionCellViewModel } from './view/completion/tasksWidgetCompletionCellViewModel';
import { TasksWidgetViewInitializationStrategy } from './view/tasksWidgetViewInitializationStrategy';
import { TasksWidgetInfoCellViewModel } from './view/info/tasksWidgetInfoCellViewModel';
import { TasksWidgetViewHelper } from './view/tasksWidgetViewHelper';
import { TasksWidgetHeader } from './header/tasksWidgetHeader';
import { ITasksWidgetViewParameterManager } from './tasksTypes';
import './tasksWidgetStyle.scss';

/** Виджет "Мои задания". */
export class TasksWidget extends ViewWidgetBase<ViewWidgetSettings, TasksWidgetHeader> {
  //#region constructors

  /**
   * Создаёт экземпляр класса {@link TasksWidget}.
   * @param _parameterManager  Менеджер для управления параметрами виджета.
   * @param _avatarFactory Фабрика для создания модели представления аватара.
   * @param viewRepository Предоставляет доступ к представлениям доступным в системе.
   * @param taskStates Набор значений для состояния задания.
   * @param dashboard Модель представления дашборда.
   * @param id Уникальный идентификатор виджета. Если не задан, то будет сгенерирован новый идентификатор.
   * @param settings Настройки виджета. Если не заданы, то будут созданы новые настройки со значением по умолчанию.
   * @param _initialSettings Настройки для колонок в представлении.
   * @param header Модель представления для заголовка виджета.
   */
  constructor(
    private readonly _parameterManager: ITasksWidgetViewParameterManager,
    private readonly _avatarFactory: IAvatarViewModelFactory,
    viewRepository: IViewRepository,
    dashboard: DashboardViewModel,
    taskStates: DbTaskState[],
    id = Guid.newGuid(),
    settings = new ViewWidgetSettings(),
    private readonly _initialSettings?: ColumnsSettings,
    header = new TasksWidgetHeader(
      (state, included) => this.modifyParameter(state, included),
      viewRepository,
      settings,
      taskStates
    )
  ) {
    settings.viewAlias ||= TasksWidgetViewHelper.ViewAlias;
    super(DefaultWidgetNames.Tasks, settings, header, dashboard, id);
  }

  //#endregion

  //#region base overrides

  protected override async initializeCore(): Promise<void> {
    await super.initializeCore();
    this.classNames.add('tasks-widget');
  }

  protected override getFakeCardTypeEntryControl(): CardTypeEntryControl | null {
    const result = new CardTypeEntryControl();
    result.type = CardControlTypes.ViewControlControlType;
    const sa = new StorageAccessor(result.controlSettings);
    sa.setString('ViewAlias', this.settings.viewAlias);
    return result;
  }

  protected override getViewControlInitializationStrategy(): IViewControlInitializationStrategy {
    return new TasksWidgetViewInitializationStrategy();
  }

  protected override async modifyViewControl(viewControl: ViewControlViewModelBase): Promise<void> {
    this.initializeDefaultParameters(viewControl);
    this.initializeTableCellContent(viewControl);
    this.initializeRefreshedEvent(viewControl);
    await this.initializeColumnSettings();
  }

  //#endregion

  //#region private methods

  private initializeDefaultParameters(viewControl: ViewControlViewModelBase): void {
    if (viewControl.viewMetadata) {
      viewControl.parameters.addParameters(
        ...this._parameterManager.generateParameters(
          viewControl.viewMetadata,
          ViewCriteriaOperators.EqualsTo,
          TasksWidgetViewHelper.DefaultParameters
        )
      );
    }
  }

  private initializeTableCellContent(viewControl: ViewControlViewModelBase): void {
    if (!viewControl.table) {
      return;
    }

    viewControl.firstRowSelection = false;
    viewControl.table.showSelectionCheckbox = false;
    const defaultCellAction = viewControl.table.createCellAction;

    viewControl.table.createCellAction = options => {
      const columnName = options.column.columnName;
      const rowData = options.row.data;

      if (columnName === TasksWidgetViewHelper.AuthorColumnName) {
        const userId = rowData.get(TasksWidgetViewHelper.AuthorIdColumnName);
        if (userId && typeof userId === 'string' && Guid.isValid(userId)) {
          const userName = rowData.get(TasksWidgetViewHelper.AuthorNameColumnName);
          const avatar = this._avatarFactory({
            avatarInfo: { id: userId },
            size: 'sm',
            shape: 'circle'
          });
          return new UserAvatarTableCellViewModel({ ...options, avatar, content: userName });
        }
      } else if (columnName === TasksWidgetViewHelper.PerformerColumnName) {
        const roleId = rowData.get(TasksWidgetViewHelper.RoleIdColumnName);
        if (roleId && typeof roleId === 'string' && Guid.isValid(roleId)) {
          const roleType = rowData.get(TasksWidgetViewHelper.RoleTypeIdColumnName);
          const roleName = localize(rowData.get(TasksWidgetViewHelper.RoleNameColumnName));

          if (roleType === RoleHelper.personalRoleTypeId) {
            const avatar = this._avatarFactory({
              avatarInfo: { id: roleId },
              size: 'sm',
              shape: 'circle'
            });

            return new UserAvatarTableCellViewModel({ ...options, avatar, content: roleName });
          }

          const dataSource = new DefaultAvatarDataSource(roleId, roleName);
          const avatar = new AvatarViewModel(dataSource, 'sm', 'circle');

          return new UserAvatarTableCellViewModel({ ...options, avatar, content: roleName });
        }
      } else if (columnName === TasksWidgetViewHelper.InfoColumnName) {
        return new TasksWidgetInfoCellViewModel({
          ...options,
          taskType: rowData.get(TasksWidgetViewHelper.TypeCaptionColumnName),
          cardType: rowData.get(TasksWidgetViewHelper.CardTypeNameColumnName),
          cardName: rowData.get(TasksWidgetViewHelper.CardNameColumnName),
          cardSubject: rowData.get(TasksWidgetViewHelper.CardSubjectColumnName),
          taskInfo: rowData.get(TasksWidgetViewHelper.TaskInfoColumnName)
        });
      } else if (columnName === TasksWidgetViewHelper.CompletionColumnName) {
        const planned = moment(rowData.get(TasksWidgetViewHelper.PlannedDateColumnName));
        if (planned.isValid()) {
          const completion = rowData.get(TasksWidgetViewHelper.TimeToCompletionColumnName);
          return new TasksWidgetCompletionCellViewModel({ ...options, planned, completion });
        }
      }

      return defaultCellAction(options);
    };
  }

  private initializeRefreshedEvent(viewControl: ViewControlViewModelBase): void {
    viewControl.onRefreshed.add(async () => {
      if (viewControl.viewMetadata) {
        await this.header.updateState(
          ...this._parameterManager.generateParameters(
            viewControl.viewMetadata,
            ViewCriteriaOperators.EqualsTo,
            TasksWidgetViewHelper.DefaultParameters
          )
        );
      }
    });
  }

  private async initializeColumnSettings(): Promise<void> {
    if (this._initialSettings) {
      await this._settingsProvider?.storeSettings(this._initialSettings);
    }
  }

  private modifyParameter(state: CardTaskState, include = true): void {
    if (this.control) {
      this._parameterManager.modifyParameter(
        this.control.parameters,
        ViewCriteriaOperators.EqualsTo,
        TasksWidgetViewHelper.StateParameterName,
        state,
        include
      );
    }
  }

  //#endregion
}
