import { observable, runInAction } from 'mobx';
import { inject, injectable } from '@tessa/application';
import {
  IView,
  IViewMetadata,
  IViewSpecialParameters,
  IViewSpecialParameters$,
  Paging,
  SortColumn,
  ViewRequest,
  ViewResultRow
} from '@tessa/platform';
import { DemoForm, PlaygroundArticle } from 'tessa/ui/playground/chunk';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';
import {
  Grid,
  GridFactory,
  GridPagingState,
  GridSortingState,
  GridCellValueType,
  GridDataRequestModifier,
  GridDataSortingColumnInfo,
  IGridColumnMetadata,
  IGridDataController,
  IGridDataRequest,
  AddSearchGridExtension
} from 'ui/grid';
import { MockGridView } from './data/mockGridView';

@injectable()
export class GridServerDataArticle extends PlaygroundArticle {
  constructor(
    @inject(IViewSpecialParameters$)
    private readonly _viewSpecialParameters: IViewSpecialParameters
  ) {
    super();
  }

  override getSettings(): PlaygroundArticleSettings {
    return {
      name: 'Controls/Grid/Loading data from server'
    };
  }

  override async initialize(): Promise<void> {
    this.addBlock({
      caption: 'Loading data from the view',
      props: async () => {
        const view = new MockGridView();
        const metadata = await view.getMetadata();

        const columns = [...metadata.columns.values()].map<IGridColumnMetadata>(mc => ({
          id: mc.alias,
          caption: mc.caption,
          dataSourceKey: mc.alias,
          dataType: mc.schemeType.fieldType
        }));

        const controller = new ViewDataController(view, this._viewSpecialParameters);

        const grid = GridFactory.createDefault({
          dataSource: controller,
          options: {
            columnsMetadata: columns
          }
        });

        grid.extensionContainer.removeExtension(AddSearchGridExtension);

        await grid.initialize();

        grid.permissionsContainer.forbidReorderRows(true);

        return {
          grid
        };
      },
      view: ({ grid }) => (
        <DemoForm
          customStyles={css => css`
            gap: 10px;
            min-width: 350px;
            display: flex;
            flex-direction: column;
            max-height: 500px;
          `}
        >
          <Grid viewModel={grid} />
        </DemoForm>
      )
    });
  }
}

class ViewDataController implements IGridDataController<ViewResultRow> {
  //#region fields

  private readonly _rows = observable.array<ViewResultRow>([], { deep: false });

  @observable.ref
  private _loading = false;

  //#region sorting

  private _viewMetadata: IViewMetadata | null = null;

  private _defaultSortingColumns: ReadonlyArray<GridDataSortingColumnInfo>;

  private _allowOptional = false;

  private _pagingState: GridPagingState | null = null;

  //#endregion

  //#region ctor

  constructor(
    private readonly _view: IView,
    private readonly _viewSpecialParameters: IViewSpecialParameters
  ) {
    this.sortingState = new GridSortingState();
  }

  //#endregion

  //#region props

  get loading(): boolean {
    return this._loading;
  }
  private set loading(value: boolean) {
    runInAction(() => {
      this._loading = value;
    });
  }

  get rows(): ViewResultRow[] {
    return this._rows;
  }

  readonly sortingState: GridSortingState;

  get pagingState(): GridPagingState | null {
    return this._pagingState;
  }

  get allowOptionalPaging(): boolean {
    return this._allowOptional;
  }

  //#endregion

  //#region IGridDataSource methods

  getValue(row: ViewResultRow, columnKey: string): GridCellValueType {
    return row.get(columnKey);
  }

  async initialize(): Promise<void> {
    this._viewMetadata = await this._view.getMetadata();

    if (this._viewMetadata.paging !== Paging.No) {
      this._pagingState = new GridPagingState();
      this._pagingState.pageSize = this._viewMetadata.pageLimit;
      this._allowOptional = this._viewMetadata.paging === Paging.Optional;
    }

    this._defaultSortingColumns = this._viewMetadata!.defaultSortColumns.map(sc => ({
      columnKey: sc.alias,
      direction: sc.descending ? 'descending' : 'ascending'
    }));

    this.sortingState.sortingColumns = this._defaultSortingColumns;

    await this.loadData();
  }

  //#endregion

  //#region methods

  async loadData(modifyRequest?: GridDataRequestModifier): Promise<void> {
    const pagingEnabledDefault = this._pagingState?.pagingEnabled ?? false;

    const dataRequest: IGridDataRequest = {
      currentPage:
        pagingEnabledDefault && this._pagingState?.currentPage ? this._pagingState.currentPage : 1,
      pageSize: this._pagingState?.pageSize ?? 0,
      pagingEnabled: pagingEnabledDefault,
      sortingColumns: this.sortingState.sortingColumns.slice()
    };

    modifyRequest?.(dataRequest);

    const request = new ViewRequest(this._viewMetadata);

    if (dataRequest.sortingColumns) {
      const validColumns: GridDataSortingColumnInfo[] = [];

      for (const column of dataRequest.sortingColumns) {
        const metadata = this._viewMetadata?.columns.get(column.columnKey);
        if (metadata && metadata.sortBy) {
          request.sortColumns.push(
            new SortColumn(column.columnKey, column.direction === 'descending')
          );
          validColumns.push(column);
        }
      }

      dataRequest.sortingColumns = validColumns;

      if (
        dataRequest.sortingColumns.length !== this.sortingState.sortingColumns.length ||
        !dataRequest.sortingColumns.every(sc =>
          this.sortingState.sortingColumns.find(
            column => sc.columnKey === column.columnKey && sc.direction === column.direction
          )
        )
      ) {
        dataRequest.currentPage = 1;
      }
    }

    const pagingEnabled = !this._allowOptional || dataRequest.pagingEnabled;

    if (this._pagingState) {
      if (pagingEnabled) {
        this._viewSpecialParameters.providePageOffsetParameter(
          request.parameters,
          Paging.Always,
          dataRequest.currentPage ?? this._pagingState.currentPage,
          dataRequest.pageSize,
          false
        );
        this._viewSpecialParameters.providePageLimitParameter(
          request.parameters,
          Paging.Always,
          dataRequest.pageSize + 1,
          false
        );
      }

      request.calculateRowCounting = true;
    }

    try {
      this.loading = true;

      const result = await this._view!.getData(request);
      const rows = result.getRowsAsMap();

      if (!rows.length) {
        return;
      }

      const pageRows =
        this._pagingState && pagingEnabled ? rows.slice(0, this._pagingState.pageSize) : rows;

      runInAction(() => {
        this._rows.length = 0;
        this._rows.push(...pageRows);

        if (this._pagingState) {
          this._pagingState.currentPage = dataRequest.currentPage;
          this._pagingState.pageSize = dataRequest.pageSize;
          this._pagingState.hasNextPage = rows.length > this._pagingState.pageSize;
          this._pagingState.hasPreviousPage = this._pagingState.currentPage > 1;
          this._pagingState.totalRows = result.rowCount;
          this._pagingState.pageCount =
            Math.ceil(result.rowCount / this._pagingState.pageSize) ?? null;
          this._pagingState.pagingEnabled = pagingEnabled;
        }

        this.sortingState.sortingColumns =
          dataRequest.sortingColumns.length > 0
            ? dataRequest.sortingColumns.slice()
            : this._defaultSortingColumns.slice();
      });
    } finally {
      this.loading = false;
    }
  }

  //#endregion
}
