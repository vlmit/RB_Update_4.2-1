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
import { ButtonWidgetSettings } from './buttonWidgetSettings';
import { ButtonWidget } from './buttonWidget';

export class ButtonWidgetSettingsEditorProvider<
  TSettings extends ButtonWidgetSettings
> implements IDashboardWidgetSettingsEditorProvider {
  //#region ctor

  constructor(protected readonly _widget: ButtonWidget<TSettings>) {}

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
    ButtonWidgetSettingsEditorProvider.addSpecialSettings(provider, builder);

    return this.modifyBuilder(builder, provider).build();
  }

  protected modifyBuilder(
    builder: PropertyGridBuilderInstance,
    _data: PropertyGridDataProvider
  ): PropertyGridBuilderInstance {
    return builder;
  }

  //#endregion

  //#region private methods

  private static addSpecialSettings(
    data: PropertyGridDataProvider,
    builder: PropertyGridBuilderInstance
  ): PropertyGridBuilderInstance {
    return builder
      .startGroup('$Dashboard_Widget_SpecialSettings')
      .addTextProperty({
        data,
        alias: 'caption',
        caption: '$Dashboard_Widget_Button_Caption',
        emojiButtonEnabled: true
      })
      .addBooleanProperty({
        data,
        alias: 'captionHidden',
        caption: '$Dashboard_Widget_Button_HideCaption',
        controlCaption: 'override'
      })
      .addTextProperty({
        data,
        alias: 'icon',
        caption: '$Dashboard_Widget_Button_Icon',
        onInitialized: async property => {
          property.control.availability = 'readonly';
        }
      })
      .addColorProperty({
        data,
        alias: 'color',
        caption: '$Dashboard_Widget_Button_Background'
      })
      .addColorProperty({
        data,
        alias: 'captionColor',
        caption: '$Dashboard_Widget_Button_Foreground'
      })
      .onGridCreated(grid => {
        grid.leftCaption = false;
      });
  }

  //#endregion
}
