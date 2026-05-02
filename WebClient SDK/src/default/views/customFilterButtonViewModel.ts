import {
  ContentPlaceArea,
  ContentPlaceOrder,
  OpenFilterDialogButtonViewModel
} from 'tessa/ui/views/content';

import { IWorkplaceViewComponent } from 'tessa/ui/views';

/**
 * Модель представления, позволяющая переопределить действие, выполняемое при нажатии на кнопку открытия диалога с параметрами фильтрации представления.
 */
export class CustomOpenFilterDialogButtonViewModel extends OpenFilterDialogButtonViewModel {
  //#region ctor

  constructor(
    openFilterDialogCommand: (viewComponent: IWorkplaceViewComponent) => Promise<void>,
    viewComponent: IWorkplaceViewComponent,
    area: ContentPlaceArea = ContentPlaceArea.ToolBarPanel,
    order: number = ContentPlaceOrder.BeforeAll
  ) {
    super(viewComponent, area, order);
    this._openFilterDialogCommand = openFilterDialogCommand;
    this._name = 'OpenFilterDialogButton';
  }

  //#endregion

  //#region fields

  private _openFilterDialogCommand: (viewComponent: IWorkplaceViewComponent) => Promise<void>;

  //#endregion

  //#region base overrides

  async openFilterDialog(): Promise<void> {
    await this._openFilterDialogCommand(this.viewComponent);
  }

  //#endregion
}
