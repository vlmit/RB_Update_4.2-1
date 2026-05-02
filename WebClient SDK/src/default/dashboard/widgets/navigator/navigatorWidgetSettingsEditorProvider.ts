import { reaction } from 'mobx';
import { StringHelper } from '@tessa/core';
import { ShadowPropsHelper } from '@tessa/ui';
import { AppPath, HttpHelper } from '@tessa/application';
import {
  PropertyGridBuilder,
  PropertyGridBuilderInstance,
  PropertyGridDataProvider
} from 'tessa/ui/propertyGrid';
import { ButtonWidgetSettingsEditorProvider } from '../button/buttonWidgetSettingsEditorProvider';
import { DefaultWidgetNames } from '../widgetNames';
import { NavigatorWidgetSettings } from './navigatorWidgetSettings';
import { NavigatorWidget } from './navigatorWidget';

/** Провайдер настроек виджета {@link NavigatorWidget}. */
export class NavigatorWidgetSettingsEditorProvider extends ButtonWidgetSettingsEditorProvider<NavigatorWidgetSettings> {
  //#region constructors

  /**
   * Создаёт экземпляр класса {@link TagWidgetSettingsEditorProvider}.
   * @param _widget Виджет "Тег".
   */
  constructor(widget: NavigatorWidget) {
    super(widget);
  }

  //#endregion

  //#region base overrides

  override modifyBuilder(
    builder: PropertyGridBuilderInstance,
    data: PropertyGridDataProvider
  ): PropertyGridBuilderInstance {
    const linkProperty = PropertyGridBuilder.createTextProperty({
      data,
      alias: 'link',
      caption: '$Dashboard_Widget_Navigator_Settings_Link',
      required: true
    });

    const openViewInSeparateTabProperty = PropertyGridBuilder.createBooleanProperty({
      data,
      alias: 'openViewInSeparateTab',
      caption: '$Dashboard_Widget_Navigator_Settings_OpenViewInSeparateTab',
      controlCaption: 'override',
      onInitialized: async property => {
        property.disposeList.add(
          ShadowPropsHelper.add(property, 'visibility', value => {
            if (value()) {
              const relativeLink = HttpHelper.isAbsoluteURL(linkProperty.value)
                ? linkProperty.value.replace(AppPath.fullPath(), '')
                : linkProperty.value;
              return StringHelper.starts(relativeLink, '/view/');
            }
            return false;
          }),
          reaction(
            () => property.visibility,
            value => !value && (property.value = false)
          )
        );
      }
    });

    return builder
      .addProperty(linkProperty)
      .addProperty(openViewInSeparateTabProperty)
      .onGridCreated(grid => {
        grid.title = DefaultWidgetNames.NavigatorTitle;
      });
  }

  //#endregion
}
