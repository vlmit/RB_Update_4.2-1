import { FieldType, TypedField } from '@tessa/core';
import { showLoadingOverlay } from 'tessa/ui';
import { createCard } from 'tessa/ui/uiHost';
import { DashboardViewModel } from 'tessa/ui/dashboard';
import { DefaultWidgetNames } from '../widgetNames';
import { ButtonWidget } from '../button/buttonWidget';
import { CreateCardByTypeWidgetSettings } from './createCardByTypeWidgetSettings';

export class CreateCardByTypeWidget extends ButtonWidget<CreateCardByTypeWidgetSettings> {
  //#region constructors

  constructor(
    dashboard: DashboardViewModel,
    id?: string,
    settings = new CreateCardByTypeWidgetSettings()
  ) {
    super(DefaultWidgetNames.CreateCardByType, settings, dashboard, id);
  }

  //#endregion

  //#region methods

  protected override async handleClickCore(): Promise<void> {
    await showLoadingOverlay(async splashResolve => {
      await createCard({
        cardTypeId: this.settings.cardType!.cardTypeId!,
        cardTypeName: this.settings.cardType!.cardTypeName!,
        info: {
          docTypeID: TypedField.create(this.settings.cardType!.documentTypeId, FieldType.Guid),
          docTypeTitle: TypedField.create(
            this.settings.cardType!.documentTypeTitle,
            FieldType.String
          )
        },
        splashResolve: splashResolve
      });
    });
  }

  //#endregion
}
