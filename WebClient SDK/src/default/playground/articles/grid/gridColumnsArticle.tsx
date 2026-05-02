import { injectable } from '@tessa/application';
import { FieldType } from '@tessa/core';
import { ShadowPropsHelper } from '@tessa/ui';
import { Checkbox } from 'ui';
import { Button } from 'ui/button/button';
import { Button as ButtonViewModel } from 'ui/button/buttonViewModel';
import { Grid, GridViewModel, GridFactory } from 'ui/grid';
import { CheckboxViewModel } from 'ui/checkbox/checkboxViewModel';
import { PlaygroundArticle, DemoForm } from 'tessa/ui/playground/chunk';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';
import { GridColumnDisplayType } from 'components/cardElements/grid';
import { MockDataProvider } from './data/mockDataProvider';

@injectable()
export class GridColumnsArticle extends PlaygroundArticle {
  override getSettings(): PlaygroundArticleSettings {
    return {
      name: 'Controls/Grid/Working with columns'
    };
  }

  override async initialize(): Promise<void> {
    this.addBlock({
      caption: 'Specify initial columns order in metadata',
      props: async () => {
        const grid = new GridViewModel();

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
            dataType: FieldType.String
          },
          {
            id: 'DriverName',
            caption: 'Driver name',
            dataSourceKey: 'DriverName',
            dataType: FieldType.String,
            initialOrder: 10
          },
          {
            id: 'ReleaseDate',
            caption: 'Release date',
            dataSourceKey: 'ReleaseDate',
            dataType: FieldType.DateTime
          },
          {
            id: 'Secondhand',
            caption: 'Secondhand',
            dataSourceKey: 'Secondhand',
            dataType: FieldType.Boolean,
            initialOrder: -1
          }
        );

        await grid.initialize();

        await grid.addRow({
          CarID: 5,
          CarName: 'Lada',
          DriverName: 'James',
          ReleaseDate: '2022-03-04',
          Secondhand: true
        });

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
const grid = new GridViewModel();

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
    dataType: FieldType.String
  },
  {
    id: 'DriverName',
    caption: 'Driver name',
    dataSourceKey: 'DriverName',
    dataType: FieldType.String,
    initialOrder: 10
  },
  {
    id: 'ReleaseDate',
    caption: 'Release date',
    dataSourceKey: 'ReleaseDate',
    dataType: FieldType.DateTime
  },
  {
    id: 'Secondhand',
    caption: 'Secondhand',
    dataSourceKey: 'Secondhand',
    dataType: FieldType.Boolean,
    initialOrder: -1
  }
);

await grid.initialize();

await grid.addRow({
  CarID: 5,
  CarName: 'Lada',
  DriverName: 'James',
  ReleaseDate: '2022-03-04',
  Secondhand: true
});
...
<Grid viewModel={grid} />
~~~`
    });

    this.addBlock({
      caption: 'Change order in metadata before grid is initialized in extension',
      props: async () => {
        const grid = GridFactory.createBase(MockDataProvider.getSimpleGridArgs());

        grid.extensionContainer.addHooks({
          gridInitializing: async context => {
            const driverColumn = context.grid.columnsMetadata.find(cm => cm.id === 'DriverName');
            if (driverColumn) {
              driverColumn.initialOrder = 100;
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
const grid = new GridViewModel();
...

grid.extensionContainer.addHooks({
  gridInitializing: async context => {
    const driverColumn = context.grid.columnsMetadata.find(cm => cm.id === 'DriverName');
    if (driverColumn) {
      driverColumn.initialOrder = 100;
    }
  }
});

await grid.initialize();
...
<Grid viewModel={grid} />
~~~`
    });

    this.addBlock({
      caption: 'Specify initial columns visibility in metadata',
      props: async () => {
        const grid = new GridViewModel();

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
            dataType: FieldType.String
          },
          {
            id: 'DriverName',
            caption: 'Driver name',
            dataSourceKey: 'DriverName',
            dataType: FieldType.String
          },
          {
            id: 'ReleaseDate',
            caption: 'Release date',
            dataSourceKey: 'ReleaseDate',
            dataType: FieldType.DateTime,
            visibility: false
          },
          {
            id: 'Secondhand',
            caption: 'Secondhand',
            dataSourceKey: 'Secondhand',
            dataType: FieldType.Boolean
          }
        );

        await grid.initialize();

        await grid.addRow({
          CarID: 5,
          CarName: 'Lada',
          DriverName: 'James',
          ReleaseDate: '2022-03-04',
          Secondhand: true
        });

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
  ...
  {
    id: 'ReleaseDate',
    caption: 'Release date',
    dataSourceKey: 'ReleaseDate',
    dataType: FieldType.DateTime,
    visibility: false
  },
  ...
);
~~~`
    });

    this.addBlock({
      caption: 'Imperatively change column visibility',
      props: async () => {
        const grid = GridFactory.createBase(MockDataProvider.getSimpleGridArgs());
        const button = ButtonViewModel.create({
          caption: 'Hide Driver name column',
          type: 'normal',
          theme: 'primary',
          buttonAction: () => {
            const column = grid.columnsMap.get('DriverName');
            if (column) {
              column.visibility = false;
            }
          }
        });

        await grid.initialize();

        return {
          grid,
          button
        };
      },
      view: ({ grid, button }) => (
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
          <Button viewModel={button} />
        </DemoForm>
      ),
      code: `
~~~jsx
const grid = new GridViewModel();
...
const button = ButtonViewModel.create({
  caption: 'Hide Driver name column',
  type: 'normal',
  theme: 'primary',
  buttonAction: () => {
    const column = grid.columnsMap.get('DriverName');
    if (column) {
      column.visibility = false;
    }
  }
});

await grid.initialize();
~~~`
    });

    this.addBlock({
      caption: 'Change column visibility through shadow container',
      props: async () => {
        const grid = GridFactory.createBase(MockDataProvider.getSimpleGridArgs());
        await grid.initialize();

        const checkbox = new CheckboxViewModel();
        checkbox.caption = 'Hide driver column';
        await checkbox.initialize();

        const driverColumn = grid.columnsMap.get('DriverName')!;
        ShadowPropsHelper.add(driverColumn, 'visibility', () => {
          return !checkbox.checked;
        });

        return {
          grid,
          checkbox
        };
      },
      view: ({ grid, checkbox }) => (
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
          <Checkbox viewModel={checkbox} />
        </DemoForm>
      ),
      code: `
~~~jsx
const grid = new GridViewModel();
...
await grid.initialize();

const checkbox = new CheckboxViewModel();
checkbox.caption = 'Hide driver column';
await checkbox.initialize();

const driverColumn = grid.columnsMap.get('DriverName')!;
ShadowPropsHelper.add(driverColumn, 'visibility', () => {
  return !checkbox.checked;
});
~~~`
    });

    this.addBlock({
      caption: 'Add new column with overridden get value.',
      props: async () => {
        const grid = GridFactory.createBase(MockDataProvider.getSimpleGridArgs());

        grid.columnsMetadata.push({
          id: 'Outside value',
          caption: 'Value not from data source',
          dataSourceKey: '',
          dataType: FieldType.String,
          getValueRule: () => 'foo'
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
const grid = new GridViewModel();
...
grid.columnsMetadata.push({
  id: 'Outside value',
  caption: 'Value not from data source',
  dataSourceKey: '',
  dataType: FieldType.String,
  getValue: () => 'foo'
});

await grid.initialize();
~~~`
    });

    this.addBlock({
      caption: 'Change columns collection after the grid is initialized',
      props: async () => {
        const grid = GridFactory.createBase(MockDataProvider.getSimpleGridArgs());

        const button = ButtonViewModel.create({
          caption: 'Add new column',
          type: 'normal',
          theme: 'primary',
          disabled: () => grid.columns.some(c => c.id === 'Color'),
          buttonAction: async () => {
            grid.columnsMetadata.push({
              id: 'Color',
              caption: 'Color',
              dataSourceKey: 'Color',
              dataType: FieldType.String
            });

            await grid.rebuild();
          }
        });

        await grid.initialize();

        return {
          grid,
          button
        };
      },
      view: ({ grid, button }) => (
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
          <Button viewModel={button} />
        </DemoForm>
      ),
      code: `
~~~jsx
const button = ButtonViewModel.create({
  caption: 'Add new column',
  type: 'normal',
  theme: 'primary',
  disabled: () => grid.columns.some(c => c.id === 'Color'),
  buttonAction: async () => {
    grid.columnsMetadata.push({
      id: 'Color',
      caption: 'Color',
      dataSourceKey: 'Color',
      dataType: FieldType.String
    });

    await grid.rebuild();
  }
});
~~~`
    });

    this.addBlock({
      caption: 'Set formatting settings for a column',
      description: 'Set date format for a column with specifier',
      props: async () => {
        const grid = new GridViewModel();

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
            dataType: FieldType.String
          },
          {
            id: 'DriverName',
            caption: 'Driver name',
            dataSourceKey: 'DriverName',
            dataType: FieldType.String,
            initialOrder: 10
          },
          {
            id: 'ReleaseDate',
            caption: 'Release date',
            dataSourceKey: 'ReleaseDate',
            dataType: FieldType.DateTime,
            formattingSettings: {
              specifier: 'd'
            }
          },
          {
            id: 'Secondhand',
            caption: 'Secondhand',
            dataSourceKey: 'Secondhand',
            dataType: FieldType.Boolean,
            initialOrder: -1
          }
        );

        await grid.initialize();

        await grid.addRow({
          CarID: 5,
          CarName: 'Lada',
          DriverName: 'James',
          ReleaseDate: '2022-03-04',
          Secondhand: true
        });

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
const grid = new GridViewModel();

grid.columnsMetadata.push(
  ...
  {
    id: 'ReleaseDate',
    caption: 'Release date',
    dataSourceKey: 'ReleaseDate',
    dataType: FieldType.DateTime,
    formattingSettings: {
      specifier: 'd'
    }
  },
  ...
);
~~~`
    });

    this.addBlock({
      caption: 'Change formatting settings for a column',
      description: 'Set format for a column with specifier after the grid is initialized',
      props: async () => {
        const grid = new GridViewModel();

        grid.columnsMetadata.push(
          {
            id: 'FileName',
            caption: 'file name',
            dataSourceKey: 'fileName',
            dataType: FieldType.String
          },
          {
            id: 'Loading',
            caption: 'Loading (percents)',
            dataSourceKey: 'loading',
            dataType: FieldType.Int
          }
        );

        await grid.initialize();

        await grid.addRow({
          FileName: 'photo.png',
          Loading: 0.87
        });

        await grid.addRow({
          FileName: 'data.txt',
          Loading: 0.34
        });

        const percentsColumn = grid.columns.find(c => c.id === 'Loading');
        if (percentsColumn) {
          percentsColumn.formattingSettings = {
            specifier: 'P1'
          };
        }

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
const percentsColumn = grid.columns.find(c => c.id === 'Loading');
if (percentsColumn) {
  percentsColumn.formattingSettings = {
    specifier: 'P1'
  };
}
~~~`
    });

    this.addBlock({
      caption: 'Column display type',
      description: 'Column can be displayed at the top or at the bottom of the row.',
      props: async () => {
        const grid = GridFactory.createBase(MockDataProvider.getSimpleGridArgs());

        grid.extensionContainer.addHooks({
          gridInitializing: async context => {
            const driverColumn = context.grid.columnsMetadata.find(cm => cm.id === 'DriverName');
            if (driverColumn) {
              driverColumn.displayType = GridColumnDisplayType.inlineTop;
            }

            const carNameColumn = context.grid.columnsMetadata.find(cm => cm.id === 'CarName');
            if (carNameColumn) {
              carNameColumn.displayType = GridColumnDisplayType.inlineBottom;
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
column.displayType = GridColumnDisplayType.inlineTop; //GridColumnDisplayType.inlineBottom
~~~`
    });
  }
}
