import { observable } from 'mobx';
import { FieldType } from '@tessa/core';
import { injectable } from '@tessa/application';
import {
  GridOrderStrategyContext,
  GridRowMoveDirection,
  IGridColumnMetadata,
  IGridOrderStrategy,
  IGridRowViewModel,
  Grid,
  GridFactory,
  GridDataSource,
  FieldGridOrderStrategy
} from 'ui/grid';
import { DemoForm, PlaygroundArticle } from 'tessa/ui/playground/chunk';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';
import { MockDataProvider } from './data/mockDataProvider';

@injectable()
export class GridRowsReorderArticle extends PlaygroundArticle {
  override getSettings(): PlaygroundArticleSettings {
    return {
      name: 'Controls/Grid/Rows reordering'
    };
  }

  override async initialize(): Promise<void> {
    this.addBlock({
      caption: 'Default reordering',
      description:
        'By default rows order is based on the row position in the array. Default reordering utilizes data source swapRows method.',
      props: async () => {
        const grid = GridFactory.createDefault(
          MockDataProvider.getSimpleGridArgs({
            multiselect: true
          })
        );

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
      )
    });

    this.addBlock({
      caption: 'Specifying order field',
      description:
        'If the row order is determined by a field, you can specify it in the grid options. Data source must support setValue method in that case because order manager needs to change order field.',
      props: async () => {
        const metadata: IGridColumnMetadata[] = [
          {
            id: 'Order',
            caption: 'Order',
            dataSourceKey: 'order',
            dataType: FieldType.Int
          },
          {
            id: 'Name',
            caption: 'Name',
            dataSourceKey: 'name',
            dataType: FieldType.String
          }
        ];

        const dataSource = new GridDataSource(
          observable.array([
            {
              order: 0,
              name: 'Москва'
            },
            {
              order: 1,
              name: 'Ставрополь'
            },
            {
              order: 2,
              name: 'Саратов'
            },
            {
              order: 3,
              name: 'Ростов-на-Дону'
            },
            {
              order: 4,
              name: 'Махачкала'
            },
            {
              order: 5,
              name: 'Пенза'
            }
          ])
        );

        const grid = GridFactory.createDefault({
          dataSource,
          options: {
            columnsMetadata: metadata,
            multiselect: true,
            orderSetting: 'order'
          }
        });

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
const grid = GridFactory.createDefault({
  dataSource,
  options: {
    columnsMetadata: metadata,
    multiselect: true,
    orderSetting: 'order'
  }
});
~~~`
    });

    this.addBlock({
      caption: 'Start order from the 1 using the default behavior',
      description:
        'By default, order starts from 0. You can specify default order strategy with different start order.',
      props: async () => {
        const metadata: IGridColumnMetadata[] = [
          {
            id: 'Order',
            caption: 'Order',
            dataSourceKey: 'order',
            dataType: FieldType.Int
          },
          {
            id: 'Name',
            caption: 'Name',
            dataSourceKey: 'name',
            dataType: FieldType.String
          }
        ];

        const dataSource = new GridDataSource(
          observable.array([
            {
              order: 1,
              name: 'Москва'
            },
            {
              order: 2,
              name: 'Ставрополь'
            },
            {
              order: 3,
              name: 'Саратов'
            },
            {
              order: 4,
              name: 'Ростов-на-Дону'
            },
            {
              order: 5,
              name: 'Махачкала'
            },
            {
              order: 6,
              name: 'Пенза'
            }
          ])
        );

        const grid = GridFactory.createDefault({
          dataSource,
          options: {
            columnsMetadata: metadata,
            multiselect: true,
            orderSetting: new FieldGridOrderStrategy('order', 1)
          }
        });

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
const grid = GridFactory.createDefault({
  dataSource,
  options: {
    columnsMetadata: metadata,
    multiselect: true,
    orderSetting: new FieldGridOrderStrategy('order', 1)
  }
});
~~~`
    });

    this.addBlock({
      caption: 'Reordering when some of the rows can`t be reordered.',
      props: async () => {
        const grid = GridFactory.createDefault(
          MockDataProvider.getSimpleGridArgs({
            multiselect: true
          })
        );

        grid.extensionContainer.addHooks({
          gridPermissionsInitializing: async context => {
            if (context.type === 'grid') {
              context.permissionsContainer.forbidAddRows(true);
            } else if (
              context.type === 'row' &&
              context.row.cellsMap.get('CarID')?.getValue<number>() === 2
            ) {
              context.permissionsContainer.forbidReorder(true);
            }
          }
        });

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
      )
    });

    this.addBlock({
      caption: 'Custom order strategy that does not support reordering at all.',
      props: async () => {
        const grid = GridFactory.createDefault(
          MockDataProvider.getSimpleGridArgs({
            multiselect: true,
            orderSetting: new CustomOrderStrategy()
          })
        );

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
const grid = new GridViewModel({
  dataSource,
  options: {
    columnsMetadata: metadata,
    orderSetting: new CustomOrderStrategy()
  }
});
~~~`
    });
  }
}

export class CustomOrderStrategy implements IGridOrderStrategy {
  canReorder(_context: GridOrderStrategyContext, _row?: IGridRowViewModel): boolean {
    return false;
  }

  sort(context: GridOrderStrategyContext): ReadonlyArray<IGridRowViewModel> {
    return context.rows;
  }

  moveRows(
    _rows: ReadonlyArray<IGridRowViewModel>,
    _direction: GridRowMoveDirection,
    _context: GridOrderStrategyContext
  ): void {}

  handleRowsAdding(
    _rows: ReadonlyArray<IGridRowViewModel>,
    _context: GridOrderStrategyContext
  ): void {}

  handleRowsRemoving(
    _rows: ReadonlyArray<IGridRowViewModel>,
    _context: GridOrderStrategyContext
  ): void {}
}
