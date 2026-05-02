import {
  DashboardWidgetSettingsHelper,
  IDashboardWidgetSettingsEditorProvider
} from 'tessa/ui/dashboard';
import {
  IPropertyGrid,
  PropertyGridBuilder,
  PropertyGridBuilderInstance,
  PropertyGridDataProvider
} from 'tessa/ui/propertyGrid';
import { ViewWidgetBase } from './viewWidgetBase';
import { ViewWidgetSettingsBase } from './viewWidgetSettingsBase';

export class ViewWidgetSettingsEditorProvider<
  TSettings extends ViewWidgetSettingsBase
> implements IDashboardWidgetSettingsEditorProvider {
  //#region ctor

  constructor(
    protected readonly _widget: ViewWidgetBase<TSettings>,
    protected readonly _title: string
  ) {}

  //#endregion

  //#region props

  get hasSettingsEditor(): boolean {
    return true;
  }

  //#endregion

  //#region methods

  async createEditor(modal: boolean): Promise<IPropertyGrid | null> {
    const provider = DashboardWidgetSettingsHelper.createSettingsProvider(this._widget, modal);

    const builder = PropertyGridBuilder.create(provider);
    DashboardWidgetSettingsHelper.addBaseContainerSettings(provider, builder);
    ViewWidgetSettingsEditorProvider.addSpecialSettings(this._title, provider, builder);

    return this.modifyBuilder(builder, provider).build();
  }

  protected modifyBuilder(
    builder: PropertyGridBuilderInstance,
    _data: PropertyGridDataProvider
  ): PropertyGridBuilderInstance {
    return builder;
  }

  //#endregion

  private static addSpecialSettings(
    title: string,
    data: PropertyGridDataProvider,
    builder: PropertyGridBuilderInstance
  ): PropertyGridBuilderInstance {
    return builder
      .startGroup('$Dashboard_Widget_SpecialSettings')
      .addBooleanProperty({
        data: data,
        alias: 'disableHorizontalScroll',
        caption: '$Dashboard_Widget_View_Settings_DisableHorizontalScroll',
        controlCaption: 'override'
      })
      .onGridCreated(grid => {
        grid.title = title;
        grid.leftCaption = false;
      });
  }

  //#endregion
}
