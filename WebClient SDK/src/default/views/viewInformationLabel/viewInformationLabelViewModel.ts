import { IViewComponentBase, IWorkplaceViewComponent } from 'tessa/ui/views';
import { BaseContentItem, ContentPlaceArea, ContentPlaceOrder } from 'tessa/ui/views/content';
import { LabelViewModel } from 'ui/label/labelViewModel';

/**
 * View model for view information label.
 */
export class ViewInformationLabelViewModel<
  T extends IViewComponentBase = IWorkplaceViewComponent
> extends BaseContentItem<T> {
  //#region fields

  readonly label: LabelViewModel;

  //#endregion

  //#region ctor

  constructor(
    viewComponent: T,
    area: ContentPlaceArea = ContentPlaceArea.ToolBarPanel,
    order: number = ContentPlaceOrder.AfterAll
  ) {
    super(viewComponent, area, order);

    this.label = new LabelViewModel();
  }

  //#endregion

  //#region props

  get isLoading(): boolean {
    return this.viewComponent.isDataLoading;
  }

  //#endregion
}
