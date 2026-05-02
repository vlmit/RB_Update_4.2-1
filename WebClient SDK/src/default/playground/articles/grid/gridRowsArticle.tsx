import { injectable } from '@tessa/application';
import { FieldType } from '@tessa/core';
import {
  GridHelper,
  GridDataSource,
  Grid,
  GridFactory,
  GridViewModel,
  GridCellValueType
} from 'ui/grid';
import { PlaygroundArticle, DemoForm } from 'tessa/ui/playground/chunk';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';

@injectable()
export class GridRowsArticle extends PlaygroundArticle {
  override getSettings(): PlaygroundArticleSettings {
    return {
      name: 'Controls/Grid/Working with rows'
    };
  }

  override async initialize(): Promise<void> {
    this.addBlock({
      caption: 'Drag rows',
      props: async () => {
        const { left, right } = createTables();

        left.extensionContainer.addHooks({
          rowInitialized: context => {
            context.row.draggable = true;
            context.row.handlersContainer.onDragStart.add(event => {
              event.dataTransfer.setData('rowid', context.row.id);
            });
          }
        });

        // drop возможен только если таблица не пустая
        right.extensionContainer.addHooks({
          gridInitialized: context => {
            context.grid.handlersContainer.onDrop.add(async event => {
              event.preventDefault();

              if (!event.dataTransfer) {
                return;
              }

              const rowId = event.dataTransfer.getData('rowid');
              if (!rowId) {
                return;
              }

              const row = left.rows.find(row => row.id === rowId);
              if (!row) {
                return;
              }

              const data: Map<string, GridCellValueType> = new Map();

              for (const column of left.columns) {
                data.set(column.id, row.cellsMap.get(column.id)?.getValue());
              }

              await context.grid.addRow(data);
              left.deleteRows(row.id);
            });

            context.grid.handlersContainer.onDragOver.add(event => {
              if (!event.dataTransfer) {
                return;
              }

              const types = event.dataTransfer.types;
              if (!types.includes('rowid')) {
                return;
              }

              event.preventDefault();
            });
          }
        });

        await left.initialize();
        await right.initialize();

        return {
          left,
          right
        };
      },
      view: ({ left, right }) => (
        <DemoForm
          customStyles={css => css`
            gap: 10px;
            min-width: 350px;
            display: flex;
            flex-direction: column;
          `}
        >
          <Grid viewModel={left} />
          <Grid viewModel={right} />
        </DemoForm>
      )
    });
  }
}

type Task = {
  title: string;
  dueDate: string;
  priority: string;
};

function createTables(): { left: GridViewModel; right: GridViewModel } {
  const metadata = GridHelper.ensureTypedMetadata<Task>([
    {
      id: 'Title',
      caption: 'Задача',
      dataSourceKey: 'title',
      dataType: FieldType.String
    },
    {
      id: 'DueDate',
      caption: 'Выполнить до',
      dataSourceKey: 'dueDate',
      dataType: FieldType.DateTime,
      formattingSettings: {
        specifier: 'd'
      }
    },
    {
      id: 'Priority',
      caption: 'Приоритет',
      dataSourceKey: 'priority',
      dataType: FieldType.String
    }
  ]);

  const todoDataSource = new GridDataSource<Task>([
    {
      title: 'Прочитать хорошую книгу',
      dueDate: '2025-08-02',
      priority: 'Низкий'
    },
    {
      title: 'Погулять в парке',
      dueDate: '2025-06-15',
      priority: 'Средний'
    },
    {
      title: 'Приготовить ужин',
      dueDate: '2025-06-11',
      priority: 'Высокий'
    },
    {
      title: 'Помыть окна',
      dueDate: '2025-06-23',
      priority: 'Средний'
    },
    {
      title: 'Подстричься',
      dueDate: '2025-06-17',
      priority: 'Низкий'
    },
    {
      title: 'Купить корм для кошки',
      dueDate: '2025-06-18',
      priority: 'Высокий'
    },
    {
      title: 'Запустить стиральную машину',
      dueDate: '2025-06-11',
      priority: 'Высокий'
    }
  ]);

  const todoTable = GridFactory.createDefault({
    dataSource: todoDataSource,
    options: {
      columnsMetadata: metadata
    }
  });

  todoTable.captionSettings.caption = 'TO DO';

  const doneTable = GridFactory.createDefault({
    options: {
      columnsMetadata: metadata
    }
  });

  doneTable.captionSettings.caption = 'DONE';

  return {
    left: todoTable,
    right: doneTable
  };
}
