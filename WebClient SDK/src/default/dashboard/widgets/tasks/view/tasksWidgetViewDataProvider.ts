import { SchemeType, SortColumn } from '@tessa/platform';
import {
  ViewControlDataProvider,
  ViewControlDataProviderRequest,
  ViewControlDataProviderResponse
} from 'tessa/ui/cards/controls/viewControl/viewControlDataProvider';
import { TasksWidgetViewHelper } from './tasksWidgetViewHelper';

/** Провайдер данных представления "Мои задания" для виджета {@link TasksWidget}. */
export class TasksWidgetViewDataProvider extends ViewControlDataProvider {
  //#region base overrides

  override async getDataAsync(
    request: ViewControlDataProviderRequest
  ): Promise<ViewControlDataProviderResponse> {
    TasksWidgetViewDataProvider.modifySortingColumns(request);
    const result = await super.getDataAsync(request);
    TasksWidgetViewDataProvider.initializeColumnsByDefault(result);
    return result;
  }

  //#endregion

  //#region private methods

  private static modifySortingColumns(request: ViewControlDataProviderRequest): void {
    request.sortingColumns = request.sortingColumns.map(column =>
      column.alias === TasksWidgetViewHelper.CompletionColumnName
        ? new SortColumn(TasksWidgetViewHelper.PlannedDateColumnName, column.descending)
        : column
    );
  }

  private static initializeColumnsByDefault(result: ViewControlDataProviderResponse): void {
    for (const alias of TasksWidgetViewHelper.VisibleColumns) {
      result.columns.push([alias, SchemeType.NullableString]);
      for (const row of result.rows) {
        row[alias] = null;
      }
    }
  }

  //#endregion
}
