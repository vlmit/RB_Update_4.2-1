import {
  IDbEnumerationProvider,
  IDbEnumerationProvider$,
  IViewRepository,
  IViewRepository$,
  DbTaskState
} from '@tessa/platform';
import { inject, injectable, localize } from '@tessa/application';
import { IAvatarViewModelFactory$ } from 'ui/avatar/avatarInjects';
import { IAvatarViewModelFactory } from 'ui/avatar/avatarTypes';
import {
  PreviewWidgetTileViewModel,
  DashboardViewModel,
  DashboardWidgetCreateNewParams,
  DashboardWidgetStorage,
  DashboardWidgetTypeBase,
  DashboardWidgetTypeDescriptor,
  IDashboardWidget,
  IDashboardWidgetSettingsEditorProvider,
  DashboardWidgetTemplateOptions
} from 'tessa/ui/dashboard';
import { ColumnsSettings } from 'tessa/ui/views/settings/columnSettings';
import { ViewWidgetSettingsEditorProvider } from '../view/viewWidgetSettingsEditorProvider';
import { ViewWidgetSettings } from '../view/viewWidgetSettings';
import { ITasksWidgetViewParameterManager$ } from '../widgetInjects';
import { DefaultWidgetNames } from '../widgetNames';
import { ITasksWidgetViewParameterManager } from './tasksTypes';
import { TasksWidget } from './tasksWidget';

/** Тип виджета {@link TasksWidget}. */
@injectable()
export class TasksWidgetType extends DashboardWidgetTypeBase<ViewWidgetSettings> {
  //#region constructors

  /**
   * Создаёт экземпляр класса {@link TasksWidgetType}.
   * @param _parameterManager  Менеджер для управления параметрами виджета.
   * @param _viewRepository Предоставляет доступ к представлениям доступным в системе.
   * @param _avatarFactory Фабрика для создания модели представления аватара.
   * @param _dbEnumerationProvider Поставщик перечислений БД.
   */
  constructor(
    @inject(ITasksWidgetViewParameterManager$)
    private readonly _parameterManager: ITasksWidgetViewParameterManager,
    @inject(IViewRepository$) private readonly _viewRepository: IViewRepository,
    @inject(IAvatarViewModelFactory$) private readonly _avatarFactory: IAvatarViewModelFactory,
    @inject(IDbEnumerationProvider$) private readonly _dbEnumerationProvider: IDbEnumerationProvider
  ) {
    super(TasksWidgetType._descriptor);
  }

  //#endregion

  //#region static

  private static readonly _descriptor = new DashboardWidgetTypeDescriptor(
    DefaultWidgetNames.Tasks,
    DefaultWidgetNames.TasksTitle,
    {
      image: 'images-emoji-briefcase',
      title: '$Dashboard_Widget_Tasks_Preview_Title',
      description: '$Dashboard_Widget_Tasks_Description'
    }
  );

  //#endregion

  //#region base overrides

  override createNewWidget(
    dashboard: DashboardViewModel,
    args?: DashboardWidgetCreateNewParams<ViewWidgetSettings>
  ): TasksWidget {
    const { content, id, settings = new ViewWidgetSettings() } = args ?? {};
    settings.headerCaption ??= localize(this.descriptor.title);
    const columnsSettings = !!content && content instanceof ColumnsSettings ? content : undefined;

    return new TasksWidget(
      this._parameterManager,
      this._avatarFactory,
      this._viewRepository,
      dashboard,
      this._dbEnumerationProvider.getAll(DbTaskState),
      id,
      settings,
      columnsSettings
    );
  }

  override deserializeWidget(
    storage: DashboardWidgetStorage,
    dashboard: DashboardViewModel
  ): TasksWidget {
    const widget = new TasksWidget(
      this._parameterManager,
      this._avatarFactory,
      this._viewRepository,
      dashboard,
      this._dbEnumerationProvider.getAll(DbTaskState),
      storage.id
    );
    widget.setStorage(storage);

    return widget;
  }

  override createWidgetByTemplate(
    templateStorage: DashboardWidgetStorage,
    dashboard: DashboardViewModel,
    _templateOptions: DashboardWidgetTemplateOptions
  ): IDashboardWidget {
    return this.deserializeWidget(templateStorage, dashboard);
  }

  override getSettingsEditorProvider(
    widget: IDashboardWidget
  ): IDashboardWidgetSettingsEditorProvider {
    if (!(widget instanceof TasksWidget)) {
      throw new Error('Widget must be of type TasksWidget');
    }

    return new ViewWidgetSettingsEditorProvider(widget, DefaultWidgetNames.TasksTitle);
  }

  override getWidgetPreview(): object | null {
    return new PreviewWidgetTileViewModel(this.descriptor);
  }

  //#endregion
}
