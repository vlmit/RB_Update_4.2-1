import { observable } from 'mobx';
import { FieldType, StringHelper } from '@tessa/core';
import { GridDataSource, GridCreateArgs, GridCreateOptions, IGridColumnMetadata } from 'ui/grid';
import { MockDataHelper } from './mockDataHelper';

export namespace MockDataProvider {
  export type SimpleGridRow = {
    CarID: number;
    CarName: string;
    DriverName: string;
    ReleaseDate: string;
    Secondhand: boolean;
    Color?: string;
    Speed?: number;
  };

  export function getSimpleGridArgs(options?: GridCreateOptions): GridCreateArgs {
    const columns: IGridColumnMetadata[] = [
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
    ];

    const dataSource = new GridDataSource<SimpleGridRow>(
      observable.array([
        {
          CarID: 1,
          CarName: 'Lada',
          DriverName: 'James',
          ReleaseDate: '2022-03-04',
          Secondhand: true,
          Color: 'red'
        },
        {
          CarID: 2,
          CarName: 'Kia',
          DriverName: 'Anne',
          ReleaseDate: '2014-02-01',
          Secondhand: false,
          Color: 'silver'
        },
        {
          CarID: 3,
          CarName: 'Mercedes',
          DriverName: 'Oliver',
          ReleaseDate: '2023-10-24',
          Secondhand: false,
          Color: 'deep blue'
        },
        {
          CarID: 4,
          CarName: 'BMW',
          DriverName: 'Maria',
          ReleaseDate: '2016-07-15',
          Secondhand: true,
          Color: 'pink'
        }
      ])
    );

    return {
      dataSource: dataSource,
      options: {
        ...options,
        columnsMetadata: columns
      }
    };
  }

  export function getEditableGridArgs(options?: GridCreateOptions): GridCreateArgs {
    const columns: IGridColumnMetadata[] = [
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
    ];

    const dataSource = new GridDataSource<SimpleGridRow>(
      observable.array(
        [
          {
            CarID: 1,
            CarName: 'Lada',
            DriverName: 'James',
            ReleaseDate: '2022-03-04',
            Secondhand: true,
            Speed: 30,
            Color: 'red'
          },
          {
            CarID: 2,
            CarName: 'Kia',
            DriverName: 'Anne',
            ReleaseDate: '2014-02-01',
            Secondhand: false,
            Speed: 35,
            Color: 'silver'
          },
          {
            CarID: 3,
            CarName: 'Mercedes',
            DriverName: 'Oliver',
            ReleaseDate: '2023-10-24',
            Secondhand: false,
            Speed: 40,
            Color: 'deep blue'
          },
          {
            CarID: 4,
            CarName: 'BMW',
            DriverName: 'Maria',
            ReleaseDate: '2016-07-15',
            Secondhand: true,
            Speed: 50,
            Color: 'pink'
          }
        ],
        { deep: true }
      )
    );

    return {
      dataSource: dataSource,
      options: {
        ...options,
        columnsMetadata: columns
      }
    };
  }

  export function getRandomGridArgs(rowsCount = 20, options?: GridCreateOptions): GridCreateArgs {
    const columns: IGridColumnMetadata[] = [
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
        id: 'Speed',
        caption: 'Speed',
        dataSourceKey: 'Speed',
        dataType: FieldType.Int
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
    ];

    const data: SimpleGridRow[] = [];
    for (let i = 0; i < rowsCount; ++i) {
      data.push({
        CarID: MockDataHelper.getRandomInt(1, rowsCount),
        CarName: StringHelper.capitalize(MockDataHelper.getRandomString(5)),
        DriverName: `${StringHelper.capitalize(MockDataHelper.getRandomString(5))} ${StringHelper.capitalize(MockDataHelper.getRandomString(6))}`,
        ReleaseDate: MockDataHelper.getRandomDate(new Date(2000, 0, 1), new Date()).toString(),
        Secondhand: MockDataHelper.getRandomBoolean(),
        Speed: MockDataHelper.getRandomInt(5, 60)
      });
    }

    const dataSource = new GridDataSource<SimpleGridRow>(observable.array(data));

    return {
      dataSource: dataSource,
      options: {
        ...options,
        columnsMetadata: columns
      }
    };
  }
}
