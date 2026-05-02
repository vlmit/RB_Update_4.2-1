import { ShadowPropsHelper } from '@tessa/ui';
import {
  DashboardWidgetSettingsHelper,
  IDashboardWidgetSettingsEditorProvider
} from 'tessa/ui/dashboard';
import {
  BooleanProperty,
  IPropertyGrid,
  PropertyGridBuilder,
  PropertyGridBuilderInstance,
  PropertyGridDataProvider
} from 'tessa/ui/propertyGrid';
import { DefaultWidgetNames } from '../widgetNames';
import { NotesWidget } from './notesWidget';

export class NotesWidgetSettingsEditorProvider implements IDashboardWidgetSettingsEditorProvider {
  //#region ctor

  constructor(private readonly _widget: NotesWidget) {}

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
    NotesWidgetSettingsEditorProvider.addSpecialSettings(provider, builder);

    return builder.build();
  }

  private static addSpecialSettings(
    data: PropertyGridDataProvider,
    builder: PropertyGridBuilderInstance
  ): PropertyGridBuilderInstance {
    return builder
      .startGroup('$Dashboard_Widget_SpecialSettings')
      .addColorProperty({
        data: data,
        alias: 'editorBackground',
        caption: '$Dashboard_Widget_Notes_Settings_EditorBackground'
      })
      .addColorProperty({
        data: data,
        alias: 'editorBorder',
        caption: '$Dashboard_Widget_Notes_Settings_EditorBorder'
      })
      .addBooleanProperty({
        data: data,
        alias: 'noPadding',
        caption: '$Dashboard_Widget_Notes_Settings_NoPadding',
        controlCaption: 'override'
      })
      .onGridInitialized(grid => {
        const noPadding = grid.findProperty<BooleanProperty>('noPadding');
        const captionGroup = grid.findGroup('$Dashboard_Widget_HeaderSettings');
        if (!noPadding || !captionGroup) {
          return;
        }

        grid.disposeList.add(
          ShadowPropsHelper.add(captionGroup, 'visibility', () => !noPadding.value)
        );
      })
      .onGridCreated(grid => {
        grid.title = DefaultWidgetNames.NotesTitle;
        grid.leftCaption = false;
      });
  }

  //#endregion
}
