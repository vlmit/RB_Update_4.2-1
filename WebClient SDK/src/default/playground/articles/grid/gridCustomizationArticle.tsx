import { FC } from 'react';
import { observable } from 'mobx';
import { injectable } from '@tessa/application';
import { FieldType } from '@tessa/core';
import { Button } from 'ui/button/button';
import { Button as ButtonViewModel } from 'ui/button/buttonViewModel';
import {
  Grid,
  GridCellViewModel,
  GridDataSource,
  GridUserInfoExtension,
  GridViewModel,
  GridCellCreateOptions,
  GridCellValueType,
  IGridColumnMetadata,
  GridFactory
} from 'ui/grid';
import { PlaygroundArticle, DemoForm } from 'tessa/ui/playground/chunk';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';
import { MockDataProvider } from './data/mockDataProvider';

@injectable()
export class GridCustomizationArticle extends PlaygroundArticle {
  override getSettings(): PlaygroundArticleSettings {
    return {
      name: 'Controls/Grid/Customize grid'
    };
  }

  override async initialize(): Promise<void> {
    this.addBlock({
      caption: 'Display control buttons in a cell',
      props: async () => {
        const grid = GridFactory.createBase(MockDataProvider.getSimpleGridArgs());
        grid.columnsMetadata.push({
          id: 'ButtonsColumn',
          dataSourceKey: '',
          caption: '',
          dataType: FieldType.Unknown,
          initialOrder: Number.MAX_SAFE_INTEGER
        });

        grid.extensionContainer.addHooks({
          cellCreate: context => {
            if (context.options.column.id === 'ButtonsColumn') {
              context.cell = new ButtonsCellViewModel(context.options);
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
class ButtonsCellViewModel extends GridCellViewModel {
  //#region ctor

  constructor(options: GridCellCreateOptions) {
    super(options);

    this.deleteButton = ButtonViewModel.create({
      type: 'icon',
      theme: 'transparent',
      icon: 'm-trash',
      buttonAction: () => {
        this._grid.deleteRow(this.row.id);
      }
    });

    this._contentOverride = _ => <ButtonsCell cell={this} />;
  }

  //#endregion

  //#region props

  readonly deleteButton: ButtonViewModel;

  //#endregion

  //#region methods

  protected override getValueCore(): GridCellValueType {
    return null;
  }

  protected override getFormattedValueCore(): string | null {
    return null;
  }

  //#endregion
}

const ButtonsCell: FC<{ cell: ButtonsCellViewModel }> = ({ cell }) => {
  return (
    <div>
      <Button viewModel={cell.deleteButton} />
    </div>
  );
};

...
const grid = new GridViewModel();
...
grid.extensionContainer.addHooks({
  cellCreate: context => {
    if (context.options.column.id === 'ButtonsColumn') {
      context.cell = new ButtonsCellViewModel(context.options);
    }
  }
});

await grid.initialize();
~~~`
    });

    this.addBlock({
      caption: 'Display user avatar and user info popover in a cell',
      props: async () => {
        const metadata: IGridColumnMetadata[] = [
          {
            id: 'FileName',
            caption: 'File',
            dataSourceKey: 'fileName',
            dataType: FieldType.String
          },
          {
            id: 'ModifiedById',
            caption: 'ModifiedByID',
            dataSourceKey: 'modifiedById',
            dataType: FieldType.Guid,
            visibility: false
          },
          {
            id: 'ModifiedByName',
            caption: 'Modified by',
            dataSourceKey: 'modifiedByName',
            dataType: FieldType.String
          }
        ];

        const dataSource = new GridDataSource(
          observable.array([
            {
              fileName: 'contract.docx',
              modifiedById: '3db19fa0-228a-497f-873a-0250bf0a4ccb',
              modifiedByName: 'Admin'
            },
            {
              fileName: 'additionalInfo.pdf',
              modifiedById: '3db19fa0-228a-497f-873a-0250bf0a4ccb',
              modifiedByName: 'Admin'
            }
          ])
        );

        const grid = new GridViewModel({
          dataSource,
          options: {
            columnsMetadata: metadata
          }
        });

        grid.extensionContainer.addExtension(GridUserInfoExtension, {
          settings: {
            targetColumnId: 'ModifiedByName',
            userId: 'ModifiedById',
            modifyAvatar: avatar => {
              avatar.size = 'xs';
            },
            modifyUserInfo: userInfo => {
              userInfo.size = 'compact';
            }
          }
        });

        await grid.initialize();

        return {
          grid
        };
      },
      view: ({ grid }) => {
        return (
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
        );
      },
      code: `
~~~jsx
grid.extensionContainer.addExtension(GridUserInfoExtension, {
  settings: {
    targetColumnId: 'ModifiedByName',
    userId: 'ModifiedById',
    modifyAvatar: avatar => {
      avatar.size = 'xs';
    },
    modifyUserInfo: userInfo => {
      userInfo.size = 'compact';
    }
  }
});
~~~`
    });
  }
}

class ButtonsCellViewModel extends GridCellViewModel {
  //#region ctor

  constructor(options: GridCellCreateOptions) {
    super(options);

    this.deleteButton = ButtonViewModel.create({
      type: 'icon',
      theme: 'transparent',
      icon: 'm-trash',
      buttonAction: () => {
        this._grid.deleteRows(this.row.id);
      }
    });

    this._contentOverride = _ => <ButtonsCell cell={this} />;
  }

  //#endregion

  //#region props

  readonly deleteButton: ButtonViewModel;

  //#endregion

  //#region methods

  protected override getValueCore(): GridCellValueType {
    return null;
  }

  protected override getFormattedValueCore(): string | null {
    return null;
  }

  //#endregion
}

const ButtonsCell: FC<{ cell: ButtonsCellViewModel }> = ({ cell }) => {
  return (
    <div>
      <Button viewModel={cell.deleteButton} />
    </div>
  );
};
