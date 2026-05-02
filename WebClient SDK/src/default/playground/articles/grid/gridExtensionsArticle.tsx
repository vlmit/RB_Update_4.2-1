import { observer } from 'mobx-react-lite';
import { FieldType } from '@tessa/core';
import { extension, ExtensionStage, injectable } from '@tessa/application';
import {
  AddDefaultPanelExtension,
  Grid,
  GridCellCreateOptions,
  GridCellViewModel,
  GridExtension,
  GridExtensionsHelper,
  GridFactory,
  IGridCellCreateContext
} from 'ui/grid';
import { Icon } from 'ui/icon/icon';
import { Button as ButtonViewModel } from 'ui/button/buttonViewModel';
import { Button } from 'ui/button/button';
import { styled } from 'tessa/ui';
import { DemoForm, PlaygroundArticle } from 'tessa/ui/playground/chunk';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';
import { IconSelector, IconSelector$ } from 'tessa/ui/iconSelector';
import { MockDataProvider } from './data/mockDataProvider';

@injectable()
export class GridExtensionsArticle extends PlaygroundArticle {
  override getSettings(): PlaygroundArticleSettings {
    return {
      name: 'Controls/Grid/Hooks and extensions',
      description: `There are two ways to extend the behavior of the grid and react to different events.
      \r\nThe simplest way is to use hooks. Grid gooks is an object that allows centralized events subscribing.
      \r\nIt's just a structure that can be created on the spot. It has limited capabilities and is not resolved by a grid,
      \r\nso different dependencies should be passed to it from the calling site. However, in most cases, this isn't necessary,
      \r\nso the hooks is the most suitable way. Also, hooks can be added at any time, event after the grid has been initialized.
      \r\nAnother options is full extension. Both extensions ans hooks share the same base interface, so all the events methods are
      \r\navailable in both. However, an extension must be a class with @extension decorator. It supports settings that can be
      \r\nmodified before the grid initialized. Extension is created by the grid, so it can have dependencies in its constructor, which
      \r\nwill be resolved during extension creation. Extensions can also be deleted and traced.
      \r\nIn simpler terms, extension is a completed, named piece of logic for a specific functionality. All the default behavior, such as
      \r\nsearch and settings, are added by extensions.
      \r\nIf you need logic that is specific to your grid and does not require any dependencies, opt for the hooks.
      \r\nIf you need reusable customizable behavior, use the extensions.`
    };
  }

  override async initialize(): Promise<void> {
    this.addBlock({
      caption: 'Hooks',
      description: 'You can add hooks anywhere and at any time.',
      props: async () => {
        const grid = GridFactory.createBase(MockDataProvider.getSimpleGridArgs());

        grid.extensionContainer.addHooks({
          rowClick: context => {
            console.log(`Row clicked: ${context.row.id}`);
          },
          gridInitialized: context => {
            context.grid.extensionContainer.addHooks({
              selectionChanged: _context => {
                console.log('Selection changed');
              }
            });
          }
        });

        await grid.initialize();

        grid.extensionContainer.addHooks({
          rowDoubleClick: context => {
            console.log(`Row double clicked: ${context.row.id}`);
          }
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
 grid.extensionContainer.addHooks({
  rowClick: context => {
    console.log(\`Row clicked: \${context.row.id}\`);
  },
  gridInitialized: context => {
    context.grid.extensionContainer.addHooks({
      selectionChanged: _context => {
        console.log('Selection changed');
      }
    });
  }
});

await grid.initialize();

grid.extensionContainer.addHooks({
  rowDoubleClick: context => {
    console.log(\`Row double clicked: \${context.row.id}\`);
  }
});
~~~`
    });

    this.addBlock({
      caption: 'Add platform extension',
      description: 'You can create base grid and add all the necessary extension yourself.',
      props: async () => {
        const grid = GridFactory.createBase(MockDataProvider.getSimpleGridArgs());

        // Adds default buttons to the grid panel
        grid.extensionContainer.addExtension(AddDefaultPanelExtension);

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
const grid = GridFactory.createBase(...);

// Adds default buttons to the grid panel
grid.extensionContainer.addExtension(AddDefaultPanelExtension);

await grid.initialize();
~~~`
    });

    this.addBlock({
      caption: 'Add platform extension and specify its settings',
      props: async () => {
        const grid = GridFactory.createBase(MockDataProvider.getSimpleGridArgs());

        grid.extensionContainer.addExtension(AddDefaultPanelExtension, {
          settings: {
            mainPanel: false
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
const grid = GridFactory.createBase(...);

grid.extensionContainer.addExtension(AddDefaultPanelExtension, {
  settings: {
    mainPanel: false
  }
});

await grid.initialize();
~~~`
    });

    this.addBlock({
      caption: 'Specify extension stage',
      description: `All the hooks and extensions are added with AfterPlatform stage by default.
        \r\nYou can specify different stage and order for the extension.`,
      props: async () => {
        const grid = GridFactory.createBase(MockDataProvider.getSimpleGridArgs());

        grid.extensionContainer.addExtension(AddDefaultPanelExtension, {
          stage: ExtensionStage.Platform
        });

        grid.extensionContainer.addHooks({
          // Will execute with AfterPlatform stage, can't specify different stage for hooks
          gridInitializing: async ({ grid }) => {
            const addButton = grid.mainPanel.items.find(
              i => i instanceof ButtonViewModel && i.name === 'AddRowButton'
            ) as ButtonViewModel;

            if (addButton) {
              addButton.buttonAction = async () => {
                console.log('Add button override');
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
      ),
      code: `
~~~jsx
grid.extensionContainer.addExtension(AddDefaultPanelExtension, {
  stage: ExtensionStage.Platform
});

grid.extensionContainer.addHooks({
  // Will execute with AfterPlatform stage, can't specify different stage for hooks
  gridInitializing: async ({ grid }) => {
    const addButton = grid.mainPanel.items.find(
      i => i instanceof Button && i.name === 'AddRowButton'
    ) as Button;

    if (addButton) {
      addButton.buttonAction = async () => {
        console.log('Add button override');
      };
    }
  }
});

await grid.initialize();
~~~`
    });

    this.addBlock({
      caption: 'You can modify extension settings if the extension wasn`t added by you.',
      props: async () => {
        // createDefault method adds default extensions
        const grid = GridFactory.createDefault(MockDataProvider.getSimpleGridArgs());

        grid.extensionContainer.modifyExtension(AddDefaultPanelExtension, {
          mainPanel: false
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
// createDefault method adds default extensions
const grid = GridFactory.createDefault(...);

grid.extensionContainer.modifyExtension(AddDefaultPanelExtension, {
  mainPanel: false
});

await grid.initialize();
~~~`
    });

    this.addBlock({
      caption: 'Add several extensions with the same type.',
      description:
        'Extensions are accessed via the name from the @extension decorator. You can override this name.',
      props: async () => {
        const grid = GridFactory.createBase(MockDataProvider.getSimpleGridArgs());

        grid.extensionContainer
          .addExtension(AddDefaultPanelExtension)
          .addExtension(AddDefaultPanelExtension, {
            nameOverride: 'AddDefaultPanelExtensionCustom'
          });

        grid.extensionContainer.modifyExtension(
          AddDefaultPanelExtension,
          {
            mainPanel: false
          },
          'AddDefaultPanelExtensionCustom'
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
const grid = GridFactory.createBase(GridArticleHelper.getSimpleGridArgs());

grid.extensionContainer
  .addExtension(AddDefaultPanelExtension)
  .addExtension(AddDefaultPanelExtension, {
    nameOverride: 'AddDefaultPanelExtensionCustom'
  });

grid.extensionContainer.modifyExtension(
  AddDefaultPanelExtension,
  {
    mainPanel: false
  },
  'AddDefaultPanelExtensionCustom'
);

await grid.initialize();
~~~`
    });

    this.addBlock({
      caption: 'Add custom extension.',
      props: async () => {
        const grid = GridFactory.createBase(MockDataProvider.getSimpleGridArgs());

        grid.columnsMetadata.push({
          id: 'Logo',
          caption: 'Car logo',
          dataSourceKey: 'Logo',
          dataType: FieldType.String
        });

        grid.extensionContainer.addExtension(SelectIconGridExtension, {
          settings: {
            columnId: 'Logo'
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
 grid.extensionContainer.addExtension(SelectIconGridExtension, {
  settings: {
    columnId: 'Logo'
  }
});
~~~`
    });
  }
}

type SelectIconGridExtensionSettings = {
  columnId: string;
};

@extension({ name: 'SelectIconGridExtension' })
class SelectIconGridExtension extends GridExtension<SelectIconGridExtensionSettings> {
  constructor(@IconSelector$() private readonly _iconSelector: IconSelector) {
    super();
  }

  cellCreate = (context: IGridCellCreateContext): void => {
    if (context.options.column.id === this._settings!.columnId) {
      context.cell = new SelectIconCellViewModel(context.options, this._iconSelector);
    }
  };

  // Получаем переданные настройки и возвращаем финальные
  protected override applySettingsCore(
    settings: Partial<SelectIconGridExtensionSettings> | null
  ): SelectIconGridExtensionSettings | null {
    return {
      columnId: GridExtensionsHelper.assertSettingsParamNotNull(this, settings?.columnId)
    };
  }
}

class SelectIconCellViewModel extends GridCellViewModel {
  //#region ctor

  constructor(options: GridCellCreateOptions, iconSelector: IconSelector) {
    super(options);

    this.openDialogButton = ButtonViewModel.create({
      name: 'iconSelectorBtn',
      icon: 'icon-thin-334',
      caption: 'Select',
      type: 'normal',
      theme: 'secondary',
      buttonAction: async () => {
        const result = await iconSelector({
          useSearchBox: true,
          multiSelect: false,
          showIconCaption: true
        });

        if (result && result.length > 0) {
          this.setValue(result[0].icon);
        }
      }
    });

    this._contentOverride = _ => <SelectIconCell cell={this} />;
  }

  //#endregion

  //#region props

  readonly openDialogButton: ButtonViewModel;

  //#endregion
}

export const StyledDiv = styled.div`
  display: flex;
  gap: 5px;
  align-items: center;
  justify-content: center;
`;

const SelectIconCell = observer<{ cell: SelectIconCellViewModel }>(({ cell }) => {
  const value = cell.getValue<string>();
  return (
    <StyledDiv className="grid-extension-select-icon">
      {value && <Icon icon={value} size="m" />}
      {!value && <Button viewModel={cell.openDialogButton} />}
    </StyledDiv>
  );
});
