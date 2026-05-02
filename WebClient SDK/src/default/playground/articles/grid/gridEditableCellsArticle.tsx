import { observable, runInAction } from 'mobx';
import { FieldType, Guid } from '@tessa/core';
import { injectable } from '@tessa/application';
import { ShadowPropsHelper } from '@tessa/ui';
import {
  AutocompleteDataViewContext,
  AutocompleteMode,
  IAutocompleteDataConverter,
  IAutocompleteRecord
} from 'ui/autocomplete';
import { NumberFieldViewModel } from 'ui/textField';
import {
  AutocompleteCellEditor,
  CellEditorActualSettings,
  Grid,
  GridDataSource,
  GridEditMode,
  GridFactory,
  GridRowEditorButtonsExtension,
  IGridColumnMetadata
} from 'ui/grid';
import { SelectViewModel } from 'ui/select/selectViewModel';
import { Select } from 'ui/select/select';
import { MenuAction } from 'tessa/ui';
import { DemoForm, PlaygroundArticle } from 'tessa/ui/playground/chunk';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';
import { MockDataProvider } from './data/mockDataProvider';

@injectable()
export class GridEditableCellsArticle extends PlaygroundArticle {
  override getSettings(): PlaygroundArticleSettings {
    return {
      name: 'Controls/Grid/Edit cell content in place'
    };
  }

  override async initialize(): Promise<void> {
    this.addBlock({
      caption: 'Set editor type and mode for columns',
      props: async () => {
        const grid = GridFactory.createDefault(MockDataProvider.getEditableGridArgs());

        grid.extensionContainer.addHooks({
          cellEditorResolved: async context => {
            if (context.cell.column.id === 'CarName') {
              runInAction(() => {
                context.editor.options.popover.horizontalAlign = 'center';
              });
            }
          },
          cellEditorControlCreated: async context => {
            if (
              context.cell.column.id === 'Speed' &&
              context.control instanceof NumberFieldViewModel
            ) {
              context.control.maxValue = 60;
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
      ),
      code: `
~~~jsx
grid.columnsMetadata.push(
  {
    id: 'CarID',
    caption: 'ID',
    dataSourceKey: 'CarID',
    dataType: FieldType.Int
  },
  {
    id: 'CarName',
    caption: 'Name',
    dataSourceKey: 'CarName',
    dataType: FieldType.String,
    editorSettings: {
      editorType: 'string'
    }
  },
  {
    id: 'DriverName',
    caption: 'Driver name',
    dataSourceKey: 'DriverName',
    dataType: FieldType.String,
    editorSettings: {
      editorType: 'string',
      mode: 'switch'
    }
  },
  {
    id: 'ReleaseDate',
    caption: 'Release date',
    dataSourceKey: 'ReleaseDate',
    dataType: FieldType.DateTime,
    editorSettings: {
      editorType: 'datetime'
    }
  },
  {
    id: 'Speed',
    caption: 'Speed',
    dataSourceKey: 'Speed',
    dataType: FieldType.Int,
    editorSettings: {
      editorType: 'integer'
    }
  },
  {
    id: 'Secondhand',
    caption: 'Secondhand',
    dataSourceKey: 'Secondhand',
    dataType: FieldType.Boolean,
    editorSettings: {
      editorType: 'boolean'
    }
  }
);
~~~`
    });

    this.addBlock({
      caption: 'Forbid to edit cells in the last row',
      props: async () => {
        const grid = GridFactory.createDefault(
          MockDataProvider.getEditableGridArgs({
            selectionMode: 'cell'
          })
        );

        await grid.initialize();

        grid.rows[grid.rows.length - 1].permissionsContainer.forbidEdit(true);

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
grid.rows[grid.rows.length - 1].permissionsContainer.forbidEdit(true);
~~~`
    });

    this.addBlock({
      caption: 'Forbid to edit cells and remove editors in the last row',
      props: async () => {
        const grid = GridFactory.createDefault(
          MockDataProvider.getEditableGridArgs({
            selectionMode: 'cell'
          })
        );

        grid.extensionContainer.addHooks({
          async cellEditorFactoryResolving(context) {
            if (context.cell.row.cellsMap.get('CarID')?.getValue<number>() === 4) {
              context.editorSettings = null;
            }
          }
        });

        await grid.initialize();

        grid.rows[grid.rows.length - 1].permissionsContainer.forbidEdit(true);

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
grid.extensionContainer.addHooks({
  cellEditorFactoryResolving(context) {
    context.editorSettings = null;
  }
});

await grid.initialize();

grid.rows[grid.rows.length - 1].permissionsContainer.forbidEdit(true);
~~~`
    });

    this.addBlock({
      caption: 'Add autocomplete editor to the cell',
      description:
        'The autocomplete editor is not automatically resolved by the cell, so you need to set it from the extension.',
      props: async () => {
        const columns: IGridColumnMetadata[] = [
          {
            id: 'User',
            caption: 'User',
            dataSourceKey: 'user',
            dataType: FieldType.Unknown
          },
          {
            id: 'Documents',
            caption: 'Documents',
            dataSourceKey: 'documents',
            dataType: FieldType.Unknown
          }
        ];

        const dataSource = new GridDataSource<TestObject>(
          observable.array([
            {
              user: {
                id: '3db19fa0-228a-497f-873a-0250bf0a4ccb',
                name: 'Admin'
              },
              documents: []
            },
            {
              user: null,
              documents: []
            }
          ])
        );

        const grid = GridFactory.createDefault({
          dataSource: dataSource,
          options: {
            columnsMetadata: columns,
            selectionMode: 'cell'
          }
        });

        grid.extensionContainer.addHooks({
          cellInitializing: async context => {
            if (context.cell.column.id === 'User') {
              context.cell.formatters.push(cellContext => {
                const rawValue = cellContext.cell.getValue<TestReference>();
                cellContext.formattedValue = rawValue?.name ?? '<empty>';
              });
            } else if (context.cell.column.id === 'Documents') {
              context.cell.formatters.push(cellContext => {
                const rawValue = cellContext.cell.getValue<TestReference[]>();
                let formatted = rawValue?.map(d => d.name).join(';');
                if (!formatted) {
                  formatted = '<empty>';
                }

                cellContext.formattedValue = formatted;
              });
            }
          },
          cellEditorFactoryResolving: async context => {
            if (context.cell.column.id === 'User') {
              context.editorSettings = {
                get settings(): CellEditorActualSettings {
                  return {
                    editorType: 'autocomplete',
                    mode: 'popover'
                  };
                },
                createEditor: args => {
                  return new AutocompleteCellEditor(
                    args.editorManager,
                    args.cell,
                    args.dataSource,
                    'popover',
                    () =>
                      new AutocompleteDataViewContext({
                        unique: true,
                        viewAlias: 'Users',
                        idColumn: 'UserID',
                        nameColumn: 'UserName',
                        parameterAlias: 'Name',
                        multiple: false
                      }),
                    new TestReferenceDataConverter()
                  );
                }
              };
            } else if (context.cell.column.id === 'Documents') {
              context.editorSettings = {
                get settings(): CellEditorActualSettings {
                  return {
                    editorType: 'autocomplete',
                    mode: 'popover'
                  };
                },
                createEditor: args => {
                  const editor = new AutocompleteCellEditor(
                    args.editorManager,
                    args.cell,
                    args.dataSource,
                    'popover',
                    () =>
                      new AutocompleteDataViewContext({
                        unique: true,
                        viewAlias: 'RefDocumentsLookup',
                        idColumn: 'DocID',
                        nameColumn: 'DocDescription',
                        parameterAlias: 'Description',
                        refSection: 'DocRefsSection',
                        multiple: true,
                        maxItemsCount: 15
                      }),
                    new TestReferenceRowDataConverter()
                  );

                  editor.onControlCreate.add(args => {
                    args.control.mode = AutocompleteMode.NonDroppable;
                  });

                  return editor;
                }
              };
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
      caption: 'Row editing',
      description: 'Start and finish editing cells in a row by button click',
      props: async () => {
        const metadata: IGridColumnMetadata[] = [
          {
            id: 'Num1',
            caption: '1',
            dataSourceKey: 'num1',
            dataType: FieldType.Int,
            editorSettings: {
              editorType: 'integer'
            }
          },
          {
            id: 'Num2',
            caption: '2',
            dataSourceKey: 'num2',
            dataType: FieldType.Int,
            editorSettings: {
              editorType: 'integer'
            }
          },
          {
            id: 'Num3',
            caption: '3',
            dataSourceKey: 'num3',
            dataType: FieldType.Int,
            editorSettings: {
              editorType: 'integer'
            }
          },
          {
            id: 'Num4',
            caption: '4',
            dataSourceKey: 'num4',
            dataType: FieldType.Int,
            editorSettings: {
              editorType: 'integer'
            }
          },
          {
            id: 'Comment',
            caption: 'Comment',
            dataSourceKey: 'comment',
            dataType: FieldType.String,
            editorSettings: {
              editorType: 'string'
            }
          },
          {
            id: 'ButtonsColumn',
            dataSourceKey: '',
            caption: '',
            dataType: FieldType.Unknown,
            initialOrder: Number.MAX_SAFE_INTEGER
          }
        ];

        const dataSource = new GridDataSource(
          observable.array([
            {
              num1: 45,
              num2: 92,
              num3: 23,
              num4: 1,
              comment: ''
            },
            {
              num1: 5,
              num2: 987,
              num3: 86,
              num4: 33,
              comment: 'add later'
            },
            {
              num1: 91,
              num2: 61,
              num3: 42,
              num4: 1444,
              comment: 'edit'
            },
            {
              num1: 772,
              num2: 793,
              num3: 33,
              num4: 0,
              comment: ''
            }
          ])
        );

        const grid = GridFactory.createDefault({
          dataSource,
          options: {
            columnsMetadata: metadata,
            editMode: 'row'
          }
        });

        grid.extensionContainer.addExtension(GridRowEditorButtonsExtension, {
          settings: {
            columnId: 'ButtonsColumn'
          }
        });

        grid.extensionContainer.addHooks({
          cellEditorControlCreated: async context => {
            if (
              context.cell.column.id === 'Num1' &&
              context.control instanceof NumberFieldViewModel
            ) {
              context.control.maxValue = 100;
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
      caption: 'Grid edit modes',
      description: 'Grid behavior in different edit modes',
      props: async () => {
        const metadata: IGridColumnMetadata[] = [
          {
            id: 'Num1',
            caption: '1',
            dataSourceKey: 'num1',
            dataType: FieldType.Int,
            editorSettings: {
              editorType: 'integer'
            }
          },
          {
            id: 'Num2',
            caption: '2',
            dataSourceKey: 'num2',
            dataType: FieldType.Int,
            editorSettings: {
              editorType: 'integer'
            }
          },
          {
            id: 'Num3',
            caption: '3',
            dataSourceKey: 'num3',
            dataType: FieldType.Int,
            editorSettings: {
              editorType: 'integer'
            }
          },
          {
            id: 'Num4',
            caption: '4',
            dataSourceKey: 'num4',
            dataType: FieldType.Int,
            editorSettings: {
              editorType: 'integer'
            }
          },
          {
            id: 'Comment',
            caption: 'Comment',
            dataSourceKey: 'comment',
            dataType: FieldType.String,
            editorSettings: {
              editorType: 'string'
            }
          },
          {
            id: 'ButtonsColumn',
            dataSourceKey: '',
            caption: '',
            dataType: FieldType.Unknown,
            initialOrder: Number.MAX_SAFE_INTEGER
          }
        ];

        const dataSource = new GridDataSource(
          observable.array([
            {
              num1: 45,
              num2: 92,
              num3: 23,
              num4: 1,
              comment: ''
            },
            {
              num1: 5,
              num2: 987,
              num3: 86,
              num4: 33,
              comment: 'add later'
            },
            {
              num1: 91,
              num2: 61,
              num3: 42,
              num4: 1444,
              comment: 'edit'
            },
            {
              num1: 772,
              num2: 793,
              num3: 33,
              num4: 0,
              comment: ''
            }
          ])
        );

        const grid = GridFactory.createDefault({
          dataSource,
          options: {
            columnsMetadata: metadata
          }
        });

        grid.extensionContainer.addExtension(GridRowEditorButtonsExtension, {
          settings: {
            columnId: 'ButtonsColumn'
          }
        });

        const selector = new SelectViewModel();
        selector.selectType = 'toolbar';
        selector.values.push(
          MenuAction.create({
            name: 'cell',
            caption: 'cell',
            action: () => {
              selector.value = 'cell';
            }
          }),
          MenuAction.create({
            name: 'row',
            caption: 'row',
            action: () => {
              selector.value = 'row';
            }
          }),
          MenuAction.create({
            name: 'mixed',
            caption: 'mixed',
            action: () => {
              selector.value = 'mixed';
            }
          })
        );

        selector.value = grid.editMode;

        ShadowPropsHelper.add(grid, 'editMode', () => (selector.value as GridEditMode) ?? 'cell');

        await grid.initialize();

        return {
          grid,
          selector
        };
      },
      view: ({ grid, selector }) => (
        <DemoForm
          customStyles={css => css`
            gap: 10px;
            min-width: 350px;
            display: flex;
            flex-direction: column;
            max-height: 500px;
          `}
        >
          <div style={{ alignSelf: 'flex-start' }}>
            <Select viewModel={selector} caption="Grid edit mode" />
          </div>
          <Grid viewModel={grid} />
        </DemoForm>
      )
    });
  }
}

type TestReference = {
  id: string;
  name: string;
};

type TestReferenceRow = TestReference & {
  rowId: string;
};

type TestObject = {
  user: TestReference | null;
  documents: TestReferenceRow[];
};

class TestReferenceDataConverter implements IAutocompleteDataConverter<TestReference> {
  toRecord(value: TestReference): IAutocompleteRecord {
    return {
      id: value.id,
      name: value.name
    };
  }

  fromRecord(record: IAutocompleteRecord): TestReference {
    return {
      id: record.id as string,
      name: record.name ?? ''
    };
  }
}

class TestReferenceRowDataConverter implements IAutocompleteDataConverter<TestReferenceRow> {
  toRecord(value: TestReferenceRow): IAutocompleteRecord {
    return {
      id: value.id,
      name: value.name,
      data: {
        rowId: value.rowId
      }
    };
  }

  fromRecord(record: IAutocompleteRecord): TestReferenceRow {
    let rowId = record.data?.rowId as string;
    if (!rowId) {
      rowId = Guid.newGuid();
    }

    return {
      id: record.id as string,
      name: record.name ?? '',
      rowId: rowId
    };
  }
}
