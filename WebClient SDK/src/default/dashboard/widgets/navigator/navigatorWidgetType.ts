import { IFileContentSaver, IFileContentSaver$ } from '@tessa/platform';
import { inject, injectable } from '@tessa/application';
import { getTessaIcon } from 'common/utility/uiHelpers';
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
import { DefaultWidgetNames } from '../widgetNames';
import { NavigatorWidget } from './navigatorWidget';
import { NavigatorWidgetSettings } from './navigatorWidgetSettings';
import { NavigatorWidgetSettingsEditorProvider } from './navigatorWidgetSettingsEditorProvider';

@injectable()
export class NavigatorWidgetType extends DashboardWidgetTypeBase<NavigatorWidgetSettings> {
  //#region constructors

  constructor(@inject(IFileContentSaver$) private readonly _fileContentSaver: IFileContentSaver) {
    super(NavigatorWidgetType._descriptor);
  }

  //#endregion

  //#region static

  private static readonly _descriptor = new DashboardWidgetTypeDescriptor(
    DefaultWidgetNames.Navigator,
    DefaultWidgetNames.NavigatorTitle,
    {
      image: 'images-emoji-compass',
      title: '$Dashboard_Widget_Navigator_Preview',
      description: '$Dashboard_Widget_Navigator_Description'
    }
  );

  //#endregion

  //#region base overrides

  override createNewWidget(
    dashboard: DashboardViewModel,
    args?: DashboardWidgetCreateNewParams<NavigatorWidgetSettings>
  ): NavigatorWidget {
    const settings = args?.settings ?? new NavigatorWidgetSettings();
    settings.icon ??= getTessaIcon('Thin124');

    return new NavigatorWidget(this._fileContentSaver, dashboard, args?.id, settings);
  }

  override deserializeWidget(
    storage: DashboardWidgetStorage,
    dashboard: DashboardViewModel
  ): NavigatorWidget {
    const widget = new NavigatorWidget(this._fileContentSaver, dashboard, storage.id);
    widget.setStorage(storage);

    return widget;
  }

  createWidgetByTemplate(
    templateStorage: DashboardWidgetStorage,
    dashboard: DashboardViewModel,
    _templateOptions: DashboardWidgetTemplateOptions
  ): IDashboardWidget {
    return this.deserializeWidget(templateStorage, dashboard);
  }

  override getSettingsEditorProvider(
    widget: IDashboardWidget
  ): IDashboardWidgetSettingsEditorProvider {
    if (!(widget instanceof NavigatorWidget)) {
      throw new Error('Widget must be of type NavigatorWidget');
    }

    return new NavigatorWidgetSettingsEditorProvider(widget);
  }

  override getWidgetPreview(): object | null {
    return new PreviewWidgetTileViewModel(this.descriptor);
  }

  //#endregion
}
