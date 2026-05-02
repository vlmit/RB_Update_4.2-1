import { IStorage } from '@tessa/core';
import { SortColumn, SortDirection } from '@tessa/platform';
import { ViewControlDataProviderRequest } from 'tessa/ui/cards/controls/viewControl/viewControlDataProvider';

export class ApiAccessTokensViewSorter {
  //#region constructors

  constructor(request: ViewControlDataProviderRequest) {
    this._sortingColumns = [];
    this._request = request;
  }

  //#endregion

  //#region fields

  private readonly _sortingColumns: SortColumn[];
  private readonly _request: ViewControlDataProviderRequest;

  //#endregion

  //#region public members

  initialize(): void {
    for (const column of this._request.sortingColumns) {
      const sortingColumn = new SortColumn(column.alias, column.sortDirection);
      this._sortingColumns.push(sortingColumn);
    }
  }

  //#endregion

  //#region IComparer Members

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  sort = (firstStorage: IStorage<any>, secondStorage: IStorage<any>): number => {
    if (!firstStorage && !secondStorage) {
      return 0;
    } else if (!firstStorage) {
      return -1;
    } else if (!secondStorage) {
      return 1;
    } else if (!this._sortingColumns.length) {
      return 0;
    }

    for (const sortingColumn of this._sortingColumns) {
      let comparsion = -0;
      const firstValue = firstStorage[sortingColumn.alias] ?? '';
      const secondValue = secondStorage[sortingColumn.alias] ?? '';
      if (typeof firstValue === 'string') {
        comparsion = firstValue.localeCompare(secondValue);
      } else if (typeof firstValue === 'boolean') {
        comparsion = Number(firstValue) - Number(secondValue);
      } else {
        comparsion = firstValue - secondValue;
      }
      if (comparsion === 0) {
        continue;
      }
      return sortingColumn.sortDirection === SortDirection.Ascending ? comparsion : -comparsion;
    }
    return 0;
  };

  //#endregion
}
