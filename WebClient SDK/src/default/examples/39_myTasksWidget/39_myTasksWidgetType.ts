import { inject, injectable } from '@tessa/application';
import { MyTasksWidget } from './39_myTasksWidget';
import { MyTasksWidgetSettings } from './39_myTasksWidgetSettings';
import { MyTasksWidgetSettingsEditorProvider } from './39_myTasksWidgetSettingsEditorProvider';
import { IViewRepository, IViewRepository$ } from '@tessa/platform';
import {
  PreviewWidgetTileViewModel,
  DashboardViewModel,
  DashboardWidgetCreateNewParams,
  DashboardWidgetStorage,
  DashboardWidgetTypeBase,
  DashboardWidgetTypeDescriptor,
  IDashboardWidget,
  IDashboardWidgetSettings,
  IDashboardWidgetSettingsEditorProvider,
  DashboardWidgetTemplateOptions
} from 'tessa/ui/dashboard';

@injectable()
export class MyTasksWidgetType extends DashboardWidgetTypeBase<MyTasksWidgetSettings> {
  //#region constructors

  constructor(@inject(IViewRepository$) private readonly _viewRepository: IViewRepository) {
    super(MyTasksWidgetType._descriptor);
  }
  //#endregion

  //#region static

  static readonly title = 'Мои задания';

  private static readonly _descriptor = new DashboardWidgetTypeDescriptor(
    'MyTasks',
    MyTasksWidgetType.title,
    {
      icon: 'ta icon-thin-091',
      title: MyTasksWidgetType.title,
      description:
        'Открывает вкладку с представлением "Мои задания" с фильтром по заданному типу задания.'
    }
  );

  //#endregion

  //#endregion

  //#region base overrides

  override createNewWidget(
    dashboard: DashboardViewModel,
    args?: DashboardWidgetCreateNewParams<MyTasksWidgetSettings>
  ): MyTasksWidget {
    const settings = args?.settings ?? new MyTasksWidgetSettings();

    return new MyTasksWidget(this._viewRepository, dashboard, args?.id, settings);
  }

  override deserializeWidget(
    storage: DashboardWidgetStorage,
    dashboard: DashboardViewModel
  ): MyTasksWidget {
    const widget = new MyTasksWidget(this._viewRepository, dashboard, storage.id);
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
    widget: IDashboardWidget<IDashboardWidgetSettings>
  ): IDashboardWidgetSettingsEditorProvider {
    if (!(widget instanceof MyTasksWidget)) {
      throw new Error('Widget must be of type MyTasksWidget');
    }

    return new MyTasksWidgetSettingsEditorProvider(widget);
  }

  override getWidgetPreview(): object | null {
    // может быть любая вью-модель, зарегистрированная во ViewComponentRegistry
    return new PreviewWidgetTileViewModel(this.descriptor);
  }

  //#endregion
}
