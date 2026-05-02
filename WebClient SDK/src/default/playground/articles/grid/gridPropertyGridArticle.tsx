import { observable, runInAction } from 'mobx';
import { FieldType, StorageHelper, IStorage } from '@tessa/core';
import { injectable } from '@tessa/application';
import { GridHelper, GridHooks, IGridRowViewModel } from 'ui/grid';
import { Button } from 'ui/button/buttonViewModel';
import { PlaygroundArticle, DemoForm } from 'tessa/ui/playground/chunk';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';
import {
  PropertyGridComponent,
  PropertyGrid,
  PropertyGridDataProvider,
  PropertyGridBuilder,
  PropertyGridHelper,
  TableProperty
} from 'tessa/ui/propertyGrid';

@injectable()
export class GridPropertyGridArticle extends PlaygroundArticle {
  override getSettings(): PlaygroundArticleSettings {
    return {
      name: 'Controls/Grid/Table property',
      description: 'Table property uses grid with all the default extensions.'
    };
  }

  override async initialize(): Promise<void> {
    this.addBlock({
      caption: 'Create property grid with table property',
      props: async () => {
        const propertyGrid = createCharacterPropertyGrid();
        await propertyGrid.initialize();

        return {
          propertyGrid
        };
      },
      view: ({ propertyGrid }) => (
        <DemoForm
          customStyles={css => css`
            min-width: 350px;

            .property-grid {
              width: 100%;
            }
          `}
        >
          <PropertyGridComponent modal={false} viewModel={propertyGrid} />
        </DemoForm>
      ),
      code: `
~~~jsx
const data = observable.object({
  characters: [
    {
      id: 0,
      name: 'John Snow',
      age: 23,
      living: true
    },
    {
      id: 1,
      name: 'Sansa Stark',
      age: 25,
      living: true
    },
    {
      id: 2,
      name: 'Arya Stark',
      age: 16,
      living: true
    },
    {
      id: 3,
      name: 'Ned Stark',
      age: 50,
      living: false
    }
  ]
});

const columns: IGridColumnMetadata[] = [
  {
    id: 'ID',
    caption: 'ID',
    dataSourceKey: 'id',
    dataType: FieldType.Int
  },
  {
    id: 'Name',
    caption: 'Name',
    dataSourceKey: 'name',
    dataType: FieldType.String
  },
  {
    id: 'Age',
    caption: 'Age',
    dataSourceKey: 'age',
    dataType: FieldType.Int
  },
  {
    id: 'Living',
    caption: 'Living',
    dataSourceKey: 'living',
    dataType: FieldType.Boolean
  }
];

const propertyGridData = new PropertyGridDataProvider(data, true);

const propertyGrid = PropertyGridBuilder.create(propertyGridData)
  .startGroup({ caption: 'Main Properties', order: 2 })
  .addTableProperty({
    data: propertyGridData,
    alias: 'characters',
    caption: 'Game of Thrones characters',
    columns: columns
  })
  .onGridCreated(grid => {
    grid.leftCaption = false;
  })
  .build();

await propertyGrid.initialize();
~~~`
    });

    this.addBlock({
      caption: 'Open row editor on double click',
      props: async () => {
        const hooks: GridHooks = {
          rowDoubleClick: async context => {
            const row = context.row.getSource<IStorage>();
            const rowClone = observable.object(StorageHelper.clone(row));
            const rowPropertyGrid = createCharacterRowPropertyGrid(rowClone);
            await rowPropertyGrid.initialize();

            const result = await PropertyGridHelper.showDialog(rowPropertyGrid);
            if (result) {
              runInAction(() => {
                StorageHelper.setStorage(rowClone, row);
              });
            }
          }
        };

        const propertyGrid = createCharacterPropertyGrid({
          hooks: [hooks]
        });
        await propertyGrid.initialize();

        return {
          propertyGrid
        };
      },
      view: ({ propertyGrid }) => (
        <DemoForm
          customStyles={css => css`
            min-width: 350px;

            .property-grid {
              width: 100%;
            }
          `}
        >
          <PropertyGridComponent modal={false} viewModel={propertyGrid} />
        </DemoForm>
      ),
      code: `
~~~jsx
 const hooks: GridHooks = {
  rowDoubleClick: async context => {
    const row = context.row.sourceRow as IStorage;
    const rowClone = observable.object(StorageHelper.clone(row));
    const propertyGridData = new PropertyGridDataProvider(rowClone, false);
    const rowPropertyGrid = PropertyGridBuilder.create(propertyGridData)
      .addNumericProperty({
        data: propertyGridData,
        alias: 'id',
        caption: 'Identifier',
        controlType: 'Integer',
        onInitialized: async ({ control }) => {
          control.minValue = 0;
        }
      })
      .addTextProperty({
        data: propertyGridData,
        alias: 'name',
        caption: 'Name'
      })
      .addNumericProperty({
        data: propertyGridData,
        alias: 'age',
        caption: 'Age',
        controlType: 'Integer',
        onInitialized: async ({ control }) => {
          control.minValue = 1;
          control.maxValue = 100;
        }
      })
      .addBooleanProperty({
        data: propertyGridData,
        alias: 'alive',
        caption: 'Alive'
      })
      .onGridCreated(grid => {
        grid.toolbarVisibility = false;
        grid.title = 'Row editor';
      })
      .build();

    await rowPropertyGrid.initialize();
    const result = await showViewModelDialog(rowPropertyGrid, PropertyGridDialog);
    if (result) {
      runInAction(() => {
        StorageHelper.setStorage(rowClone, row);
      });
    }
  }
};
...
builder.addTableProperty({
  data: propertyGridData,
  alias: 'characters',
  caption: 'Game of Thrones characters',
  columns: columns,
  hooks: [hooks]
});
~~~`
    });

    this.addBlock({
      caption: 'Open row editor for a new row right after adding',
      props: async () => {
        const hooks: GridHooks = {
          rowDoubleClick: async context => {
            await showCharacterRowEditor(context.row);
          },
          gridInitializing: async ({ grid }) => {
            const addButton = grid.mainPanel.items.find(
              i => i instanceof Button && i.name === 'AddRowButton'
            ) as Button;
            if (addButton) {
              addButton.buttonAction = async () => {
                if (!addButton.disabled) {
                  const newRow = await grid.addRow({
                    ID: null,
                    Name: null,
                    Age: null,
                    Alive: true
                  });

                  await showCharacterRowEditor(newRow);
                }
              };
            }
          }
        };

        const propertyGrid = createCharacterPropertyGrid({
          hooks: [hooks]
        });
        await propertyGrid.initialize();

        return {
          propertyGrid
        };
      },
      view: ({ propertyGrid }) => (
        <DemoForm
          customStyles={css => css`
            min-width: 350px;

            .property-grid {
              width: 100%;
            }
          `}
        >
          <PropertyGridComponent modal={false} viewModel={propertyGrid} />
        </DemoForm>
      ),
      code: `
~~~jsx
...
builder.addTableProperty({
  data: propertyGridData,
  alias: 'characters',
  caption: 'Game of Thrones characters',
  columns: columns,
  hooks: [{
    gridInitializing: async ({ grid }) => {
      const addButton = grid.mainPanel.items.find(
        i => i instanceof Button && i.name === 'AddRowButton'
      ) as Button;
      if (addButton) {
        addButton.buttonAction = async () => {
          if (!addButton.disabled) {
            const newRow = await grid.addRow({
              ID: null,
              Name: null,
              Age: null,
              Alive: true
            });

            await showCharacterRowEditor(newRow);
          }
        };
      }
    }
  }]
});
~~~`
    });
  }
}

type Character = {
  id: number;
  name: string;
  age: number;
  alive: boolean;
};

async function showCharacterRowEditor(row: IGridRowViewModel): Promise<void> {
  const sourceRow = row.getSource<IStorage>();
  const rowClone = observable.object(StorageHelper.clone(sourceRow));
  const rowPropertyGrid = createCharacterRowPropertyGrid(rowClone);
  await rowPropertyGrid.initialize();

  const result = await PropertyGridHelper.showDialog(rowPropertyGrid);
  if (result) {
    runInAction(() => {
      StorageHelper.setStorage(rowClone, sourceRow);
    });
  }
}

function createCharacterPropertyGrid(args?: {
  hooks?: GridHooks[];
  propertyInitialized?: (property: TableProperty) => Promise<void>;
  propertyInitializing?: (property: TableProperty) => Promise<void>;
}): PropertyGrid {
  const data = observable.object({
    characters: [
      {
        id: 0,
        name: 'John Snow',
        age: 23,
        alive: true
      },
      {
        id: 1,
        name: 'Sansa Stark',
        age: 25,
        alive: true
      },
      {
        id: 2,
        name: 'Arya Stark',
        age: 16,
        alive: true
      },
      {
        id: 3,
        name: 'Ned Stark',
        age: 50,
        alive: false
      }
    ]
  });

  const propertyGridData = new PropertyGridDataProvider(data, false);

  return PropertyGridBuilder.create(propertyGridData)
    .startGroup({ caption: 'Main Properties', order: 2 })
    .addTableProperty({
      data: propertyGridData,
      alias: 'characters',
      caption: 'Game of Thrones characters',
      tableOptions: {
        columnsMetadata: GridHelper.ensureTypedMetadata<Character>([
          {
            id: 'ID',
            caption: 'ID',
            dataSourceKey: 'id',
            dataType: FieldType.Int
          },
          {
            id: 'Name',
            caption: 'Name',
            dataSourceKey: 'name',
            dataType: FieldType.String
          },
          {
            id: 'Age',
            caption: 'Age',
            dataSourceKey: 'age',
            dataType: FieldType.Int
          },
          {
            id: 'Alive',
            caption: 'Alive',
            dataSourceKey: 'alive',
            dataType: FieldType.Boolean
          }
        ]),
        multiselect: true
      },
      hooks: args?.hooks,
      onInitialized: args?.propertyInitialized,
      onInitializing: args?.propertyInitializing
    })
    .onGridCreated(grid => {
      grid.toolbarVisibility = false;
      grid.leftCaption = false;
    })
    .build();
}

function createCharacterRowPropertyGrid(row: IStorage): PropertyGrid {
  const propertyGridData = new PropertyGridDataProvider(row, false);

  return PropertyGridBuilder.create(propertyGridData)
    .addNumericProperty({
      data: propertyGridData,
      alias: 'id',
      caption: 'Identifier',
      controlType: 'Integer',
      onInitialized: async ({ control }) => {
        control.minValue = 0;
      }
    })
    .addTextProperty({
      data: propertyGridData,
      alias: 'name',
      caption: 'Name'
    })
    .addNumericProperty({
      data: propertyGridData,
      alias: 'age',
      caption: 'Age',
      controlType: 'Integer',
      onInitialized: async ({ control }) => {
        control.minValue = 1;
        control.maxValue = 100;
      }
    })
    .addBooleanProperty({
      data: propertyGridData,
      alias: 'alive',
      caption: 'Alive'
    })
    .onGridCreated(grid => {
      grid.toolbarVisibility = false;
      grid.title = 'Row editor';
    })
    .build();
}
