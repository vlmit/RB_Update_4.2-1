import { StorageAccessor } from '@tessa/core';
import { CardControlTypes, CardTypeEntryControl } from 'tessa/cards/types';
import { DashboardViewModel, DashboardWidgetHeader } from 'tessa/ui/dashboard';
import { ViewControlViewModelBase } from 'tessa/ui/cards/controls';
import { ColumnsSettings } from 'tessa/ui/views/settings/columnSettings';
import { DefaultWidgetNames } from '../widgetNames';
import { ViewWidgetSettings } from './viewWidgetSettings';
import { ViewWidgetBase } from './viewWidgetBase';

export class ViewWidget extends ViewWidgetBase<ViewWidgetSettings> {
  //#region fields

  private readonly _initialSettings?: ColumnsSettings;

  //#endregion

  //#region ctor

  constructor(
    dashboard: DashboardViewModel,
    id?: string,
    settings = new ViewWidgetSettings(),
    columnsSettings?: ColumnsSettings,
    header = new DashboardWidgetHeader(settings)
  ) {
    super(DefaultWidgetNames.View, settings, header, dashboard, id);

    this._initialSettings = columnsSettings;
  }

  //#endregion

  //#region protected methods

  protected override getFakeCardTypeEntryControl(): CardTypeEntryControl | null {
    const result = new CardTypeEntryControl();
    result.type = CardControlTypes.ViewControlControlType;

    new StorageAccessor(result.controlSettings).setString('ViewAlias', this.settings.viewAlias);

    return result;
  }

  protected override async modifyViewControl(viewControl: ViewControlViewModelBase): Promise<void> {
    if ((viewControl.viewMetadata && this.settings.parameters?.length) ?? 0 > 0) {
      viewControl.parameters.addParameters(...this.settings.parameters!);
      viewControl.parameters.parameters.forEach(param => {
        const metadata = viewControl.viewMetadata?.parameters.tryGet(param.name);
        if (metadata) {
          const metadataClone = metadata.clone();
          metadataClone.hidden = true;
          param.metadata = metadataClone;
        }
      });
    }

    if (this._initialSettings) {
      await this._settingsProvider?.storeSettings(this._initialSettings);
    }
  }

  //#endregion
}
