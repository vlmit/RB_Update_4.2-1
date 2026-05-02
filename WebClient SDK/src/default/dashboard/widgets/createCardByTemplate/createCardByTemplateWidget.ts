import { showLoadingOverlay } from 'tessa/ui';
import { createFromTemplate } from 'tessa/ui/uiHost';
import { DashboardViewModel } from 'tessa/ui/dashboard';
import { DefaultWidgetNames } from '../widgetNames';
import { ButtonWidget } from '../button/buttonWidget';
import { CreateCardByTemplateWidgetSettings } from './createCardByTemplateWidgetSettings';

export class CreateCardByTemplateWidget extends ButtonWidget<CreateCardByTemplateWidgetSettings> {
  //#region constructors

  constructor(
    dashboard: DashboardViewModel,
    id?: string,
    settings = new CreateCardByTemplateWidgetSettings()
  ) {
    super(DefaultWidgetNames.CreateCardByTemplate, settings, dashboard, id);
  }

  //#endregion

  //#region methods

  protected override async handleClickCore(): Promise<void> {
    await showLoadingOverlay(async splashResolve => {
      await createFromTemplate(this.settings.template!.id, {
        splashResolve: splashResolve,
        saveCreationRequest: false
      });
    });
  }

  //#endregion
}
