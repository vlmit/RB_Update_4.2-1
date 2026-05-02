import { injectable } from '@tessa/application';
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
import { CreateCardByTemplateWidget } from './createCardByTemplateWidget';
import { CreateCardByTemplateWidgetSettings } from './createCardByTemplateWidgetSettings';
import { CreateCardByTemplateWidgetSettingsEditorProvider } from './createCardByTemplateWidgetSettingsEditorProvider';

@injectable()
export class CreateCardByTemplateWidgetType extends DashboardWidgetTypeBase<CreateCardByTemplateWidgetSettings> {
  //#region constructors

  constructor() {
    super(CreateCardByTemplateWidgetType._descriptor);
  }

  //#endregion

  //#region static

  private static readonly _descriptor = new DashboardWidgetTypeDescriptor(
    DefaultWidgetNames.CreateCardByTemplate,
    DefaultWidgetNames.CreateCardByTemplateTitle,
    {
      image: 'images-emoji-magic-wand',
      title: DefaultWidgetNames.CreateCardByTemplateTitle,
      description: '$Dashboard_Widget_CreateCardByTemplate_Description'
    }
  );

  //#endregion

  //#region base overrides

  override createNewWidget(
    dashboard: DashboardViewModel,
    args?: DashboardWidgetCreateNewParams<CreateCardByTemplateWidgetSettings>
  ): CreateCardByTemplateWidget {
    const settings = args?.settings ?? new CreateCardByTemplateWidgetSettings();
    settings.icon ??= 'l-new-document';

    return new CreateCardByTemplateWidget(dashboard, args?.id, settings);
  }

  override deserializeWidget(
    storage: DashboardWidgetStorage,
    dashboard: DashboardViewModel
  ): CreateCardByTemplateWidget {
    const widget = new CreateCardByTemplateWidget(dashboard, storage.id);
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
    if (!(widget instanceof CreateCardByTemplateWidget)) {
      throw new Error('Widget must be of type CreateCardByTemplateWidget');
    }

    return new CreateCardByTemplateWidgetSettingsEditorProvider(widget);
  }

  override getWidgetPreview(): object | null {
    return new PreviewWidgetTileViewModel(this.descriptor);
  }

  //#endregion
}
