import { injectable } from '@tessa/application';
import { FieldType } from '@tessa/core';
import { Grid, GridViewModel } from 'ui/grid';
import { PlaygroundArticle, DemoForm } from 'tessa/ui/playground/chunk';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';

@injectable()
export class GridCreateArticle extends PlaygroundArticle {
  override getSettings(): PlaygroundArticleSettings {
    return {
      name: 'Controls/Grid/Create from code'
    };
  }

  override async initialize(): Promise<void> {
    this.addBlock({
      caption: 'Create simple grid',
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
            dataType: FieldType.DateTime
          },
          {
            id: 'Secondhand',
            caption: 'Secondhand',
            dataSourceKey: 'Secondhand',
            dataType: FieldType.Boolean
          }
        );

        await grid.initialize();

        const newRow = await grid.addRow({
          CarID: 5,
          CarName: 'Lada',
          DriverName: 'James',
          ReleaseDate: '2022-03-04'
        });

        newRow.cellsMap.get('Secondhand')!.setValue(true);

        grid.captionSettings.caption = 'Simple grid with cars';

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
    dataType: FieldType.String
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
    dataType: FieldType.Boolean
  }
);

await grid.initialize();

const newRow = await grid.addRow({
  CarID: 5,
  CarName: 'Lada',
  DriverName: 'James',
  ReleaseDate: '2022-03-04'
});

newRow.cellsMap.get('Secondhand')!.setValue(true);
...
<Grid viewModel={grid} />
~~~`
    });
  }
}
