import { assertNotNull } from '@tessa/core';
import { localize } from '@tessa/application';
import { showSearchQueryDialog } from 'tessa/ui/uiHost/filterDialog/searchQueryDialog';
import { SearchQueryWidgetSettings } from '../../searchQuery/searchQueryWidgetSettings';
import {
  DashboardType,
  DashboardViewModel,
  IDashboardWidgetTile,
  IDashboardWidgetType
} from 'tessa/ui/dashboard';

export class SearchQueryTile implements IDashboardWidgetTile {
  //#region fields

  private readonly _widgetType: IDashboardWidgetType;

  private readonly _localizedTitle: string;

  //#endregion

  //#region ctor

  constructor(widgetType: IDashboardWidgetType) {
    this._widgetType = widgetType;
    this._localizedTitle = localize(this._widgetType.descriptor.title).toLowerCase();
  }

  //#endregion

  //#region props

  readonly order = 2;

  readonly name = 'SearchQuery';

  readonly isTemplate = false;

  readonly isShared = false;

  readonly visibility = true;

  //#endregion

  //#region  methods

  getViewModel(): object {
    return assertNotNull(this._widgetType.getWidgetPreview());
  }

  async handleClick(_e: React.MouseEvent, dashboard: DashboardViewModel): Promise<void> {
    const dialog = await showSearchQueryDialog({
      needSelectedQuery: true,
      isPublicQueriesEnabled: true,
      isPublicQueryCheckBoxVisible: false,
      isUserQueriesEnabled: dashboard.type === DashboardType.Personal
    });

    if (!dialog) {
      return;
    }

    const selected = dialog.currentQuery;
    if (!selected) {
      return;
    }

    const settings = new SearchQueryWidgetSettings();
    settings.headerCaption = dialog.name;
    settings.searchQueryId = selected.id;

    const widget = this._widgetType.createNewWidget(dashboard, {
      id: dashboard.generateWidgetIdentifier(),
      settings: settings
    });
    await dashboard.addWidget(widget);
  }

  filter(search: string): boolean {
    return this._localizedTitle.includes(search.toLowerCase());
  }

  dispose(): void {}

  //#endregion
}
