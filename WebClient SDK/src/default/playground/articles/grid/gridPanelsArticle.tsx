import { injectable } from '@tessa/application';
import { ShadowPropsHelper } from '@tessa/ui';
import { DemoForm, PlaygroundArticle } from 'tessa/ui/playground/chunk';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';
import { CheckboxViewModel } from 'ui/checkbox/checkboxViewModel';
import { Grid, GridFactory } from 'ui/grid';
import { Checkbox } from 'ui/checkbox/checkbox';
import { Button } from 'ui/button/buttonViewModel';
import { MockDataProvider } from './data/mockDataProvider';

@injectable()
export class GridPanelsArticle extends PlaygroundArticle {
  override getSettings(): PlaygroundArticleSettings {
    return {
      name: 'Controls/Grid/Panels',
      description:
        'By default, the grid has two action panels: the main panel and the secondary panel. You can control their visibility and position.'
    };
  }

  override async initialize(): Promise<void> {
    this.addBlock({
      caption: 'Hide main grid panel',
      props: async () => {
        const grid = GridFactory.createDefault(MockDataProvider.getSimpleGridArgs());
        await grid.initialize();

        grid.mainPanel.visibility = false;

        return {
          grid
        };
      },
      view: ({ grid }) => (
        <DemoForm
          customStyles={css => css`
            min-width: 350px;
          `}
        >
          <Grid viewModel={grid} />
        </DemoForm>
      ),
      code: `
~~~jsx
grid.mainPanel.visibility = false;
~~~`
    });

    this.addBlock({
      caption: 'Create grid without default actions at the main panel',
      description:
        'All default actions are added through grid extensions. Create base grid instead of default one if you don`t want default actions.',
      props: async () => {
        const grid = GridFactory.createBase(MockDataProvider.getSimpleGridArgs());
        await grid.initialize();

        return {
          grid
        };
      },
      view: ({ grid }) => (
        <DemoForm
          customStyles={css => css`
            min-width: 350px;
          `}
        >
          <Grid viewModel={grid} />
        </DemoForm>
      ),
      code: `
~~~jsx
const grid = GridFactory.createBase(...);
~~~`
    });

    this.addBlock({
      caption: 'Add control to the grid main panel',
      props: async () => {
        const grid = GridFactory.createDefault(MockDataProvider.getSimpleGridArgs());

        grid.extensionContainer.addHooks({
          gridInitializing: async ({ grid }) => {
            const checkbox = new CheckboxViewModel();
            checkbox.caption = 'Disable grid';
            checkbox.alias = 'DisableGridCheckbox';

            ShadowPropsHelper.add(grid, 'disabled', () => {
              return checkbox.checked;
            });

            grid.mainPanel.builder.addItem(checkbox, 'custom', {
              groupPosition: {
                itemId: 'move',
                type: 'after'
              }
            });
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
            min-width: 350px;
          `}
        >
          <Grid viewModel={grid} />
        </DemoForm>
      ),
      code: `
~~~jsx
grid.extensionContainer.addHooks({
  gridInitializing: async ({ grid }) => {
    const checkbox = new CheckboxViewModel();
    checkbox.caption = 'Disable grid';
    checkbox.alias = 'DisableGridCheckbox';

    ShadowPropsHelper.add(grid, 'disabled', () => {
      return checkbox.checked;
    });

    grid.mainPanel.builder.addItem(checkbox, 'custom', {
      groupPosition: {
        itemId: 'move',
        type: 'after'
      }
    });
  }
});
~~~`
    });

    this.addBlock({
      caption: 'Add control to the grid secondary panel',
      props: async () => {
        const grid = GridFactory.createDefault(MockDataProvider.getSimpleGridArgs());

        const button = Button.create({
          name: 'DuplicateRowButton',
          icon: 'm-copy',
          caption: 'Дублировать',
          theme: 'control',
          type: 'small',
          disabled: () => !grid.selectionManager.selectedRow,
          buttonAction: async () => {
            const selectedRow = grid.selectionManager.selectedRow;
            if (!selectedRow) {
              return;
            }

            const sourceRow = selectedRow.getSource<MockDataProvider.SimpleGridRow>();
            await grid.addRow({
              CarID: sourceRow.CarID,
              CarName: sourceRow.CarName,
              DriverName: sourceRow.DriverName,
              ReleaseDate: sourceRow.ReleaseDate,
              Secondhand: sourceRow.Secondhand
            });
          }
        });

        grid.secondaryPanel.builder.addItem(button, 'custom', {
          groupConfig: {
            crossAlign: 'center',
            align: 'start'
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
            min-width: 350px;
          `}
        >
          <Grid viewModel={grid} />
        </DemoForm>
      ),
      code: `
~~~jsx
const button = Button.create({
  name: 'DuplicateRowButton',
  icon: 'm-copy',
  caption: 'Дублировать',
  theme: 'control',
  type: 'small',
  disabled: () => !grid.selectionManager.selectedRow,
  buttonAction: () => {
    const selectedRow = grid.selectionManager.selectedRow;
    if (!selectedRow) {
      return;
    }

    const sourceRow = selectedRow.getSource<MockDataProvider.SimpleGridRow>();
    grid.addRow({
      CarID: sourceRow.CarID,
      CarName: sourceRow.CarName,
      DriverName: sourceRow.DriverName,
      ReleaseDate: sourceRow.ReleaseDate,
      Secondhand: sourceRow.Secondhand
    });
  }
});

grid.secondaryPanel.builder.addItem(button, 'custom', {
  groupConfig: {
    crossAlign: 'center',
    align: 'start'
  }
});
~~~`
    });

    this.addBlock({
      caption: 'Create grid with main top panel',
      props: async () => {
        const grid = GridFactory.createDefault(
          MockDataProvider.getSimpleGridArgs({
            mainPanelPosition: 'top'
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
            min-width: 350px;
          `}
        >
          <Grid viewModel={grid} />
        </DemoForm>
      ),
      code: `
~~~jsx
const grid = new GridViewModel({
  ...
  mainPanelPosition: 'top'
});
~~~`
    });

    this.addBlock({
      caption: 'Change main grid panel position',
      props: async () => {
        const grid = GridFactory.createDefault(MockDataProvider.getSimpleGridArgs());
        await grid.initialize();

        grid.mainPanelPosition = 'top';

        return {
          grid
        };
      },
      view: ({ grid }) => (
        <DemoForm
          customStyles={css => css`
            min-width: 350px;

            .property-grid {
              width: 100%;
            }
          `}
        >
          <Grid viewModel={grid} />
        </DemoForm>
      ),
      code: `
~~~jsx
grid.mainPanelPosition = 'top';
~~~`
    });

    this.addBlock({
      caption: 'Hide default panel actions through grid permissions',
      props: async () => {
        const grid = GridFactory.createDefault(MockDataProvider.getSimpleGridArgs());
        await grid.initialize();

        const addRowCheckbox = new CheckboxViewModel();
        addRowCheckbox.caption = 'Forbid add rows';
        await addRowCheckbox.initialize();

        const deleteRowCheckbox = new CheckboxViewModel();
        deleteRowCheckbox.caption = 'Forbid delete rows';
        await deleteRowCheckbox.initialize();

        const reorderRowCheckbox = new CheckboxViewModel();
        reorderRowCheckbox.caption = 'Forbid reorder rows';
        await reorderRowCheckbox.initialize();

        const editCheckbox = new CheckboxViewModel();
        editCheckbox.caption = 'Forbid edit';
        await editCheckbox.initialize();

        const checkboxes = [addRowCheckbox, deleteRowCheckbox, reorderRowCheckbox, editCheckbox];

        grid.permissionsContainer.forbidAddRows(() => addRowCheckbox.checked);
        grid.permissionsContainer.forbidDeleteRows(() => deleteRowCheckbox.checked);
        grid.permissionsContainer.forbidReorderRows(() => reorderRowCheckbox.checked);
        grid.permissionsContainer.setReadonly(() => editCheckbox.checked);

        return {
          grid,
          checkboxes
        };
      },
      view: ({ grid, checkboxes }) => {
        const checkboxesComponents = checkboxes.map((c, i) => <Checkbox viewModel={c} key={i} />);

        return (
          <DemoForm
            customStyles={css => css`
              min-width: 350px;
              display: flex;
              flex-direction: column;
              gap: 10px;

              .grid-article-permissions-checkboxes {
                display: flex;
                gap: 10px;
              }
            `}
          >
            <Grid viewModel={grid} />
            <div className="grid-article-permissions-checkboxes">{checkboxesComponents}</div>
          </DemoForm>
        );
      }
    });
  }
}
