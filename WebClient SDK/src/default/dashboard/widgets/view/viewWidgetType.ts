import { injectable, localize } from '@tessa/application';
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
import { DefaultWidgetNames } from '../widgetNames';
import { ViewWidget } from './viewWidget';
import { ViewWidgetSettings } from './viewWidgetSettings';
import { ViewWidgetSettingsEditorProvider } from './viewWidgetSettingsEditorProvider';

@injectable()
export class ViewWidgetType extends DashboardWidgetTypeBase<ViewWidgetSettings> {
  //#region ctor

  constructor() {
    super(ViewWidgetType._descriptor);
  }

  //#endregion

  //#region static

  private static readonly _descriptor = new DashboardWidgetTypeDescriptor(
    DefaultWidgetNames.View,
    DefaultWidgetNames.ViewTitle,
    {
      hidden: true,
      image: 'images-emoji-clipboard',
      title: DefaultWidgetNames.ViewTitle,
      description: '$Dashboard_Widget_View_PasteTile_Caption'
    }
  );

  //#endregion

  //#region methods

  override createNewWidget(
    dashboard: DashboardViewModel,
    args?: DashboardWidgetCreateNewParams<ViewWidgetSettings>
  ): ViewWidget {
    const { content, id, settings = new ViewWidgetSettings() } = args ?? {};
    settings.headerCaption ??= localize(this.descriptor.title);
    const columnsSettings = !!content && content instanceof ColumnsSettings ? content : undefined;

    return new ViewWidget(dashboard, id, settings, columnsSettings);
  }

  override deserializeWidget(
    storage: DashboardWidgetStorage,
    dashboard: DashboardViewModel
  ): ViewWidget {
    const widget = new ViewWidget(dashboard, storage.id);
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
    if (!(widget instanceof ViewWidget)) {
      throw new Error('Widget must be of type ViewWidget');
    }

    return new ViewWidgetSettingsEditorProvider(widget, DefaultWidgetNames.ViewTitle);
  }

  override getWidgetPreview(): object | null {
    const preview = new PreviewWidgetTileViewModel(this.descriptor);
    preview.className.add('view-widget-past-preview');

    return preview;
  }

  //#endregion
}
