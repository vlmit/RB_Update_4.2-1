import { StorageAccessor } from '@tessa/core';
import { ISearchQueryMetadata, ISearchQueryRepository } from '@tessa/platform';
import { CardControlTypes, CardTypeEntryControl } from 'tessa/cards/types';
import { DashboardViewModel, DashboardWidgetHeader } from 'tessa/ui/dashboard';
import { ViewControlViewModelBase } from 'tessa/ui/cards/controls';
import { DefaultWidgetNames } from '../widgetNames';
import { ViewWidgetBase } from '../view/viewWidgetBase';
import { SearchQueryWidgetSettings } from './searchQueryWidgetSettings';

export class SearchQueryWidget extends ViewWidgetBase<SearchQueryWidgetSettings> {
  //#region fields

  private readonly _searchQueryRepository: ISearchQueryRepository;
  private _searchQueryMetadata: ISearchQueryMetadata | null;

  //#endregion

  //#region ctor

  constructor(
    searchQueryRepository: ISearchQueryRepository,
    dashboard: DashboardViewModel,
    id?: string,
    settings = new SearchQueryWidgetSettings(),
    header = new DashboardWidgetHeader(settings)
  ) {
    super(DefaultWidgetNames.SearchQuery, settings, header, dashboard, id);

    this._searchQueryRepository = searchQueryRepository;
  }

  protected override async initializeCore(): Promise<void> {
    this._searchQueryMetadata = await this._searchQueryRepository.getById(
      this.settings.searchQueryId
    );

    await super.initializeCore();
  }

  protected override getFakeCardTypeEntryControl(): CardTypeEntryControl | null {
    if (!this._searchQueryMetadata) {
      this.error = '$Dashboard_Widget_SearchQuery_Error_NotFound';

      return null;
    }

    const result = new CardTypeEntryControl();
    result.type = CardControlTypes.ViewControlControlType;

    new StorageAccessor(result.controlSettings).setString(
      'ViewAlias',
      this._searchQueryMetadata!.viewAlias
    );

    return result;
  }

  protected override async modifyViewControl(viewControl: ViewControlViewModelBase): Promise<void> {
    if ((viewControl.viewMetadata && this._searchQueryMetadata?.parameters.length) ?? 0 > 0) {
      viewControl.parameters.addParameters(...this._searchQueryMetadata!.parameters);
      viewControl.parameters.parameters.forEach(param => {
        const metadataClone = param.metadata.clone();
        metadataClone.hidden = true;
        param.metadata = metadataClone;
      });
    }
  }

  //#endregion
}
