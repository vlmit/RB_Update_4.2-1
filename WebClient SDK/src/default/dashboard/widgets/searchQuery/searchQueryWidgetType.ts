import { inject, injectable, localize } from '@tessa/application';
import { ISearchQueryRepository, ISearchQueryRepository$ } from '@tessa/platform';
import {
  PreviewWidgetTileViewModel,
  DashboardViewModel,
  DashboardWidgetCreateNewParams,
  DashboardWidgetStorage,
  DashboardWidgetTypeBase,
  DashboardWidgetTypeDescriptor,
  IDashboardWidget,
  IDashboardWidgetSettingsEditorProvider,
  DashboardWidgetTemplateOptions
} from 'tessa/ui/dashboard';
import { ViewWidgetSettingsEditorProvider } from '../view/viewWidgetSettingsEditorProvider';
import { DefaultWidgetNames } from '../widgetNames';
import { SearchQueryWidget } from './searchQueryWidget';
import { SearchQueryWidgetSettings } from './searchQueryWidgetSettings';

@injectable()
export class SearchQueryWidgetType extends DashboardWidgetTypeBase<SearchQueryWidgetSettings> {
  //#region constructors

  constructor(
    @inject(ISearchQueryRepository$) private readonly _searchQueryRepository: ISearchQueryRepository
  ) {
    super(SearchQueryWidgetType._descriptor);
  }

  //#endregion

  //#region static

  private static readonly _descriptor = new DashboardWidgetTypeDescriptor(
    DefaultWidgetNames.SearchQuery,
    DefaultWidgetNames.SearchQueryTitle,
    {
      hidden: true,
      image: 'images-emoji-search',
      title: DefaultWidgetNames.SearchQueryTitle
    }
  );

  //#endregion

  //#region base overrides

  override createNewWidget(
    dashboard: DashboardViewModel,
    args?: DashboardWidgetCreateNewParams<SearchQueryWidgetSettings>
  ): SearchQueryWidget {
    const settings = args?.settings ?? new SearchQueryWidgetSettings();
    settings.headerCaption ??= localize(this.descriptor.title);

    return new SearchQueryWidget(this._searchQueryRepository, dashboard, args?.id, settings);
  }

  override deserializeWidget(
    storage: DashboardWidgetStorage,
    dashboard: DashboardViewModel
  ): SearchQueryWidget {
    const widget = new SearchQueryWidget(this._searchQueryRepository, dashboard, storage.id);
    widget.setStorage(storage);

    return widget;
  }

  override createWidgetByTemplate(
    templateStorage: DashboardWidgetStorage,
    dashboard: DashboardViewModel,
    _templateOptions: DashboardWidgetTemplateOptions
  ): IDashboardWidget {
    return this.deserializeWidget(templateStorage, dashboard);
  }

  override getSettingsEditorProvider(
    widget: IDashboardWidget
  ): IDashboardWidgetSettingsEditorProvider {
    if (!(widget instanceof SearchQueryWidget)) {
      throw new Error('Widget must be of type SearchQueryWidget');
    }

    return new ViewWidgetSettingsEditorProvider(widget, DefaultWidgetNames.SearchQueryTitle);
  }

  override getWidgetPreview(): object | null {
    return new PreviewWidgetTileViewModel(this.descriptor);
  }

  //#endregion
}
