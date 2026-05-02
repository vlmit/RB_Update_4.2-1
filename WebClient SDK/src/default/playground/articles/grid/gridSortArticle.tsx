import { injectable } from '@tessa/application';
import { DemoForm, PlaygroundArticle } from 'tessa/ui/playground/chunk';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';
import { Grid, GridFactory } from 'ui/grid';
import { MockDataProvider } from './data/mockDataProvider';

@injectable()
export class GridSortArticle extends PlaygroundArticle {
  override getSettings(): PlaygroundArticleSettings {
    return {
      name: 'Controls/Grid/Sorting'
    };
  }

  override async initialize(): Promise<void> {
    this.addBlock({
      caption: 'Prevent sorting by a particular column',
      description:
        'By default, canSort is set to true in the column metadata. To prevent sorting by a particular column, you must explicitly set it to false.',
      props: async () => {
        const grid = GridFactory.createDefault(MockDataProvider.getSimpleGridArgs());

        const column = grid.columnsMetadata.find(m => m.id === 'DriverName');
        if (column) {
          column.canSort = false;
        }

        await grid.initialize();

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
      ),
      code: `
~~~jsx
...
 const columns: IGridColumnMetadata[] = [
  {
    id: 'DriverName',
    caption: 'Driver name',
    dataSourceKey: 'DriverName',
    dataType: FieldType.String,
    canSort: false
  },
];
...
~~~`
    });

    this.addBlock({
      caption: 'Override sort rule for a column',
      props: async () => {
        const grid = GridFactory.createDefault(MockDataProvider.getSimpleGridArgs());

        const column = grid.columnsMetadata.find(m => m.id === 'DriverName');
        if (column) {
          column.sortRule = (first, second) => {
            const firstName = first.cellsMap.get('DriverName')?.getValue<string>() ?? '';
            const secondName = second.cellsMap.get('DriverName')?.getValue<string>() ?? '';

            // Сортируем по количеству букв "а" в имени
            return (
              firstName.toLowerCase().split('a').length - secondName.toLowerCase().split('a').length
            );
          };
        }

        await grid.initialize();

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
      ),
      code: `
~~~jsx
 const columns: IGridColumnMetadata[] = [
  ...
  {
    id: 'DriverName',
    caption: 'Driver name',
    dataSourceKey: 'DriverName',
    dataType: FieldType.String,
    sortRule = (first, second) => {
      const firstName = first.cellsMap.get('DriverName')?.getValue<string>() ?? '';
      const secondName = second.cellsMap.get('DriverName')?.getValue<string>() ?? '';

      // Сортируем по количеству букв "а" в имени
      return (
        firstName.toLowerCase().split('a').length - secondName.toLowerCase().split('a').length
      );
    };
  },
  ...
    ];
~~~`
    });
  }
}
