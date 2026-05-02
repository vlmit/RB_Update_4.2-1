import { IDashboardWidgetSettingsEditorProvider } from 'tessa/ui/dashboard/dashboardTypes';
import { DashboardWidgetSettingsHelper } from 'tessa/ui/dashboard/widgets/dashboardWidgetSettingsHelper';
import { IPropertyGrid, PropertyGridBuilder } from 'tessa/ui/propertyGrid';
import { DefaultWidgetNames } from '../widgetNames';
import { UsersWidget } from './usersWidget';

/** Провайдер настроек виджета "Справочник сотрудников". */
export class UsersWidgetSettingsEditorProvider implements IDashboardWidgetSettingsEditorProvider {
  //#region constructors

  /**
   * Создаёт экземпляр класса {@link UsersWidgetSettingsEditorProvider}.
   * @param _widget Виджет "Справочник сотрудников".
   */
  constructor(private readonly _widget: UsersWidget) {}

  //#endregion

  //#region IDashboardWidgetSettingsEditorProvider

  get hasSettingsEditor(): boolean {
    return true;
  }

  async createEditor(modal: boolean): Promise<IPropertyGrid | null> {
    const provider = DashboardWidgetSettingsHelper.createSettingsProvider(this._widget, modal);

    const builder = PropertyGridBuilder.create(provider);
    DashboardWidgetSettingsHelper.addBaseContainerSettings(provider, builder);
    builder.onGridCreated(grid => {
      grid.title = DefaultWidgetNames.UsersTitle;
      grid.leftCaption = false;
    });

    return builder.build();
  }

  //#endregion
}
