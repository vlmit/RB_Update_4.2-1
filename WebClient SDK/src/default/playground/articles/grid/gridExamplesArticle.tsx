import { observer } from 'mobx-react-lite';
import { FormattingHelper } from '@tessa/core';
import { injectable } from '@tessa/application';
import { Checkbox } from 'ui';
import { CheckboxViewModel } from 'ui/checkbox/checkboxViewModel';
import { Grid, IGridCellViewModel, GridFactory } from 'ui/grid';
import { Icon } from 'ui/icon/icon';
import { TextFieldView } from 'ui/textField/textFieldView';
import { TextFieldViewModel } from 'ui/textField/textFieldViewModel';
import { showMessage } from 'tessa/ui/tessaDialog';
import { DemoForm, PlaygroundArticle } from 'tessa/ui/playground/chunk';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';
import { MockDataProvider } from './data/mockDataProvider';

@injectable()
export class GridExamplesArticle extends PlaygroundArticle {
  override getSettings(): PlaygroundArticleSettings {
    return {
      name: 'Controls/Grid/Examples'
    };
  }

  override async initialize(): Promise<void> {
    this.addBlock({
      caption: 'Selection changed event',
      props: async () => {
        const grid = GridFactory.createBase(MockDataProvider.getSimpleGridArgs());

        const textField = new TextFieldViewModel();
        textField.availability = 'readonly';
        await textField.initialize();

        grid.extensionContainer.addHooks({
          selectionChanged: context => {
            if (context.mode === 'row' && context.selected.length === 1) {
              textField.text =
                context.selected[0].cellsMap.get('CarName')?.formattedValue ?? 'empty';
            } else {
              textField.text = '';
            }
          }
        });

        await grid.initialize();

        return {
          grid,
          textField
        };
      },
      view: ({ grid, textField }) => (
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
          <TextFieldView viewModel={textField} />
        </DemoForm>
      ),
      code: `
~~~jsx
grid.extensionContainer.addHooks({
  selectionChanged: context => {
    if (context.mode === 'row' && context.selected.length === 1) {
      textField.text =
        context.selected[0].cellsMap.get('CarName')?.formattedValue ?? 'empty';
    } else {
      textField.text = '';
    }
  }
});
~~~`
    });

    this.addBlock({
      caption: 'Custom cell format',
      props: async () => {
        const grid = GridFactory.createBase(MockDataProvider.getSimpleGridArgs());

        const checkbox = new CheckboxViewModel();
        checkbox.caption = 'Show only dates';
        await checkbox.initialize();

        grid.extensionContainer.addHooks({
          cellInitializing: async context => {
            if (context.cell.column.id === 'ReleaseDate') {
              context.cell.formatters.push(cellContext => {
                const rawValue = cellContext.cell.getValue<string>();
                if (!!rawValue) {
                  cellContext.formattedValue = checkbox.checked
                    ? FormattingHelper.formatDate(rawValue)
                    : FormattingHelper.formatDateTime(rawValue);
                }
              });
            }
          }
        });

        await grid.initialize();

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
grid.extensionContainer.addHooks({
  cellInitializing: async context => {
    if (context.cell.column.id === 'ReleaseDate') {
      context.cell.formatters.push(cellContext => {
        const rawValue = cellContext.cell.getValue<string>();
        if (!!rawValue) {
          cellContext.formattedValue = checkbox.checked
            ? FormattingHelper.formatDate(rawValue)
            : FormattingHelper.formatDateTime(rawValue);
        }
      });
    }
  }
});
~~~`
    });

    this.addBlock({
      caption: 'Row click and double click',
      props: async () => {
        const grid = GridFactory.createBase(MockDataProvider.getSimpleGridArgs());

        grid.extensionContainer.addHooks({
          gridInitialized: ({ grid }) => {
            grid.styles.add(
              css => css`
                .myRow.clicked {
                  color: green;
                }
              `
            );
          },
          rowInitialized: context => {
            context.row.className.add('myRow');
          },
          rowClick: context => {
            if (context.event.altKey) {
              if (context.row.className.has('clicked')) {
                context.row.className.remove('clicked');
              } else {
                context.row.className.add('clicked');
              }
            }
          },
          rowDoubleClick: async context => {
            await showMessage(
              `The driver is ${context.row.cellsMap.get('DriverName')?.formattedValue}`
            );
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
  gridInitialized: ({ grid }) => {
    grid.styles.add(
      css => css\`
        .myRow.clicked {
          color: green;
        }
      \`
    );
  },
  rowInitialized: context => {
    context.row.className.add('myRow');
  },
  rowClick: context => {
    if (context.event.altKey) {
      if (context.row.className.has('clicked')) {
        context.row.className.remove('clicked');
      } else {
        context.row.className.add('clicked');
      }
    }
  },
  rowDoubleClick: async context => {
    await showMessage(
      \`The driver is \${context.row.cellsMap.get('DriverName')?.formattedValue}\`
    );
  }
});

await grid.initialize();
~~~`
    });

    this.addBlock({
      caption: 'Override cell content',
      props: async () => {
        const grid = GridFactory.createBase(MockDataProvider.getSimpleGridArgs());

        grid.extensionContainer.addHooks({
          cellInitialized: context => {
            if (context.cell.column.id === 'CarName') {
              context.cell.contentOverride = cell => <CarNameWithIcon cell={cell} />;
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
const CarNameWithIcon: FC<{ cell: IGridCellViewModel }> = observer(({ cell }) => {
  return (
    <div style={{ display: 'flex', gap: '5px', alignItems: 'center' }}>
      <Icon icon="ta icon-thin-262 icon-bold" size="m" />
      <span>{cell.formattedValue}</span>
    </div>
  );
});

const grid = new GridViewModel();
...
grid.extensionContainer.addHooks({
  cellInitialized: context => {
    if (context.cell.column.id === 'CarName') {
      context.cell.contentOverride = cell => <CarNameWithIcon cell={cell} />;
    }
  }
});

await grid.initialize();
~~~`
    });
  }
}

const CarNameWithIcon = observer<{ cell: IGridCellViewModel }>(({ cell }) => {
  return (
    <div style={{ display: 'flex', gap: '5px', alignItems: 'center' }}>
      <Icon icon="ta icon-thin-262 icon-bold" size="m" />
      <span>{cell.formattedValue}</span>
    </div>
  );
});
