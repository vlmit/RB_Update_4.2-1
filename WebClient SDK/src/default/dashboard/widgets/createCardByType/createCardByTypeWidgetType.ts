import { inject, injectable } from '@tessa/application';
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
import { ICardTypeSelectorTypesProvider$ } from '../widgetInjects';
import { CreateCardByTypeWidget } from './createCardByTypeWidget';
import { CreateCardByTypeWidgetSettings } from './createCardByTypeWidgetSettings';
import { ICardTypeSelectorTypesProvider } from './cardTypeSelector/cardTypeSelectorTypes';
import { CreateCardByTypeWidgetSettingsEditorProvider } from './createCardByTypeWidgetSettingsEditorProvider';

@injectable()
export class CreateCardByTypeWidgetType extends DashboardWidgetTypeBase<CreateCardByTypeWidgetSettings> {
  //#region constructors

  constructor(
    @inject(ICardTypeSelectorTypesProvider$)
    private readonly _typesProvider: ICardTypeSelectorTypesProvider
  ) {
    super(CreateCardByTypeWidgetType._descriptor);
  }

  //#endregion

  //#region static

  private static readonly _descriptor = new DashboardWidgetTypeDescriptor(
    DefaultWidgetNames.CreateCardByType,
    DefaultWidgetNames.CreateCardByTypeTitle,
    {
      image: 'images-emoji-pencil',
      title: DefaultWidgetNames.CreateCardByTypeTitle,
      description: '$Dashboard_Widget_CreateCardByType_Description'
    }
  );

  //#endregion

  //#region base overrides

  override createNewWidget(
    dashboard: DashboardViewModel,
    args?: DashboardWidgetCreateNewParams<CreateCardByTypeWidgetSettings>
  ): CreateCardByTypeWidget {
    const settings = args?.settings ?? new CreateCardByTypeWidgetSettings();
    settings.icon ??= 'l-new-document';

    return new CreateCardByTypeWidget(dashboard, args?.id, settings);
  }

  override deserializeWidget(
    storage: DashboardWidgetStorage,
    dashboard: DashboardViewModel
  ): CreateCardByTypeWidget {
    const widget = new CreateCardByTypeWidget(dashboard, storage.id);
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
    if (!(widget instanceof CreateCardByTypeWidget)) {
      throw new Error('Widget must be of type CreateCardByTypeWidget');
    }

    return new CreateCardByTypeWidgetSettingsEditorProvider(widget, this._typesProvider);
  }

  override getWidgetPreview(): object | null {
    return new PreviewWidgetTileViewModel(this.descriptor);
  }

  //#endregion
}
