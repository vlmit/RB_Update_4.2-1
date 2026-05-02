import { Primitive, StringHelper } from '@tessa/core';
import {
  IView,
  IViewMetadata,
  Paging,
  SchemeType,
  SortColumn,
  ViewColumnMetadata,
  ViewMetadata,
  ViewRequest,
  ViewResult,
  ViewSubsetMetadata
} from '@tessa/platform';
import { delay } from 'tessa/platform';
import { MockDataHelper } from './mockDataHelper';

export type MockGridViewOptions = {
  paging: Paging;
  pageSize: number;
  rowsCount: number;
};

export class MockGridView implements IView {
  private readonly _options: MockGridViewOptions;
  private _metadata: IViewMetadata | null = null;
  private _data: Array<Array<Primitive>> | null = null;

  constructor(options?: MockGridViewOptions) {
    this._options = options ?? {
      paging: Paging.Optional,
      pageSize: 10,
      rowsCount: 50
    };
  }

  async getMetadata(): Promise<IViewMetadata> {
    return (this._metadata ??= this.createMetadata());
  }

  async getData(request: ViewRequest): Promise<ViewResult> {
    if (!this._data) {
      this._data = [];
      for (let i = 0; i < this._options.rowsCount; ++i) {
        this._data.push(this.generateDataRow());
      }
    }

    this._metadata ??= this.createMetadata();

    const result = new ViewResult();
    result.columns = [...this._metadata.columns.keys()];
    result.schemeTypes = [...this._metadata.columns.values().map(v => v.schemeType)];

    let data = this._data.slice();
    if (request.sortColumns.length > 0) {
      data.sort((a, b) => {
        for (const sortColumn of request.sortColumns) {
          const index = result.columns.indexOf(sortColumn.alias);
          if (index < 0) {
            continue;
          }

          const directionMultiplier = sortColumn.descending ? -1 : 1;
          const columnMetadata = this._metadata!.columns.get(sortColumn.alias)!;

          let comparisonResult = 0;

          switch (columnMetadata.schemeType) {
            case SchemeType.Int32: {
              comparisonResult = (a[index] as number) - (b[index] as number);
              break;
            }
            case SchemeType.String: {
              comparisonResult = (a[index] as string).localeCompare(b[index] as string);
              break;
            }
            case SchemeType.Boolean: {
              comparisonResult = Number(!!a[index]) - Number(!!b[index]);
              break;
            }
            case SchemeType.DateTime: {
              comparisonResult =
                new Date(a[index] as string).getTime() - new Date(b[index] as string).getTime();
              break;
            }
            default: {
              comparisonResult = 0;
            }
          }

          if (comparisonResult !== 0) {
            return comparisonResult * directionMultiplier;
          }
        }

        return 0;
      });
    }

    if (request.calculateRowCounting) {
      result.rowCount = data.length;
    }

    const pageOffsetParam = request.parameters.find(p => p.name === 'PageOffset');
    if (pageOffsetParam) {
      const pageOffset = pageOffsetParam.criteriaValues[0].values[0].value as number;

      const pageSizeParam = request.parameters.find(p => p.name === 'PageLimit');
      const pageSize =
        (pageSizeParam?.criteriaValues[0].values[0].value as number) ?? this._options.pageSize;

      data = data.slice(pageOffset - 1, pageOffset + pageSize + 1);
    }

    result.rows = data;

    await delay(500);

    return result;
  }

  private createMetadata(): ViewMetadata {
    const metadata = new ViewMetadata();
    metadata.alias = 'GridTestCars';
    metadata.caption = 'GridTestCars';
    metadata.paging = this._options.paging;
    metadata.pageLimit = this._options.pageSize;
    metadata.rowCountSubset = 'Count';

    metadata.defaultSortColumns.push(new SortColumn('CarName'));

    const columns = this.getColumns();
    for (const column of columns) {
      metadata.columns.set(column.alias, column);
    }

    const subsets = this.getSubsets();
    for (const subset of subsets) {
      metadata.subsets.set(subset.alias, subset);
    }

    return metadata;
  }

  private getColumns(): ViewColumnMetadata[] {
    const carIdColumn = new ViewColumnMetadata();
    carIdColumn.alias = 'CarID';
    carIdColumn.caption = 'ID';
    carIdColumn.schemeType = SchemeType.Int32;
    carIdColumn.sortBy = 'CarID';

    const carNameColumn = new ViewColumnMetadata();
    carNameColumn.alias = 'CarName';
    carNameColumn.caption = 'Name';
    carNameColumn.schemeType = SchemeType.String;
    carNameColumn.sortBy = 'CarName';

    const driverColumn = new ViewColumnMetadata();
    driverColumn.alias = 'DriverName';
    driverColumn.caption = 'Driver';
    driverColumn.schemeType = SchemeType.String;
    driverColumn.sortBy = 'DriverName';

    const releaseDateColumn = new ViewColumnMetadata();
    releaseDateColumn.alias = 'ReleaseDate';
    releaseDateColumn.caption = 'Release date';
    releaseDateColumn.schemeType = SchemeType.DateTime;
    releaseDateColumn.sortBy = 'ReleaseDate';

    const secondhandColumn = new ViewColumnMetadata();
    secondhandColumn.alias = 'Secondhand';
    secondhandColumn.caption = 'Secondhand';
    secondhandColumn.schemeType = SchemeType.Boolean;
    secondhandColumn.sortBy = 'Secondhand';

    return [carIdColumn, carNameColumn, driverColumn, releaseDateColumn, secondhandColumn];
  }

  private getSubsets(): ViewSubsetMetadata[] {
    const countSubset = new ViewSubsetMetadata();
    countSubset.alias = 'Count';

    return [countSubset];
  }

  private generateDataRow(): Array<Primitive> {
    const carId = MockDataHelper.getRandomInt(1, this._options.rowsCount);
    const carName = StringHelper.capitalize(MockDataHelper.getRandomString(5));
    const driverName = `${StringHelper.capitalize(MockDataHelper.getRandomString(5))} ${StringHelper.capitalize(MockDataHelper.getRandomString(6))}`;
    const releaseDate = MockDataHelper.getRandomDate(new Date(2000, 0, 1), new Date()).toString();
    const secondhand = MockDataHelper.getRandomBoolean();

    return [carId, carName, driverName, releaseDate, secondhand];
  }
}
