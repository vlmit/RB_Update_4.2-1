import { inject, injectable } from '@tessa/application';
import {
  IDashboardWidgetTypeFactory$,
  IDashboardWidgetTile,
  IDashboardWidgetTilesProvider,
  IDashboardWidgetTypeFactory,
  IDashboardWidgetTilesProviderType,
  DashboardWidgetTilesProviderArgs
} from 'tessa/ui/dashboard';
import { DefaultWidgetNames } from '../../widgetNames';
import { SearchQueryTile } from './searchQueryTile';

@injectable()
export class SearchQueryTilesProvider implements IDashboardWidgetTilesProvider {
  // #region ctor

  constructor(
    @inject(IDashboardWidgetTypeFactory$)
    private readonly _widgetTypeFactory: IDashboardWidgetTypeFactory
  ) {}

  //#endregion

  //#region props

  readonly type: IDashboardWidgetTilesProviderType = 'static';

  //#endregion

  //#region methods

  async getTiles(_args: DashboardWidgetTilesProviderArgs): Promise<IDashboardWidgetTile[]> {
    const searchQueryWidgetType = this._widgetTypeFactory.getType(DefaultWidgetNames.SearchQuery);

    return [new SearchQueryTile(searchQueryWidgetType)];
  }

  //#endregion
}
