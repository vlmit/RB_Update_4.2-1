import { injectable } from '@tessa/application';
import { GridFactory, Grid } from 'ui/grid';
import { PlaygroundArticle, DemoForm } from 'tessa/ui/playground/chunk';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';
import { MockDataProvider } from './data/mockDataProvider';

@injectable()
export class GridSelectionArticle extends PlaygroundArticle {
  override getSettings(): PlaygroundArticleSettings {
    return {
      name: 'Controls/Grid/Selection'
    };
  }

  override async initialize(): Promise<void> {
    this.addBlock({
      caption: 'Row selection',
      props: async () => {
        const grid = GridFactory.createBase(
          MockDataProvider.getSimpleGridArgs({
            selectionMode: 'row'
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
const grid = new GridViewModel({
  options: {
    selectionMode: 'row'
  }
});
...
await grid.initialize();
...

grid.selectionManager.setSelectionMode('row');
~~~`
    });

    this.addBlock({
      caption: 'Multiple row selection',
      props: async () => {
        const grid = GridFactory.createBase(
          MockDataProvider.getSimpleGridArgs({
            selectionMode: 'row',
            multiselect: true
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
const grid = new GridViewModel({
  options: {
    selectionMode: 'row',
    multiselect: true
  }
});
...
await grid.initialize();
...

grid.selectionManager.setMultiselect(true);
~~~`
    });

    this.addBlock({
      caption: 'Hide row selection checkboxes',
      props: async () => {
        const grid = GridFactory.createBase(
          MockDataProvider.getSimpleGridArgs({
            selectionMode: 'row'
          })
        );
        await grid.initialize();

        grid.hideSelectionCheckboxes = true;

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
grid.hideSelectionCheckboxes = true;
~~~`
    });

    this.addBlock({
      caption: 'Show inline row selection checkboxes',
      props: async () => {
        const grid = GridFactory.createBase(
          MockDataProvider.getSimpleGridArgs({
            selectionMode: 'row'
          })
        );
        await grid.initialize();

        grid.selectionCheckboxType = 'inline';

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
grid.selectionCheckboxType = 'inline';
~~~`
    });

    this.addBlock({
      caption: 'Cell selection',
      props: async () => {
        const grid = GridFactory.createBase(
          MockDataProvider.getSimpleGridArgs({
            selectionMode: 'cell'
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
const grid = new GridViewModel({
  options: {
    selectionMode: 'cell'
  }
});
...
await grid.initialize();
...

grid.selectionManager.setSelectionMode('cell');
~~~`
    });

    this.addBlock({
      caption: 'Multiple cell selection',
      props: async () => {
        const grid = GridFactory.createBase(
          MockDataProvider.getSimpleGridArgs({
            selectionMode: 'cell',
            multiselect: true
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
const grid = new GridViewModel({
  options: {
    selectionMode: 'cell',
    multiselect: true
  }
});
...
await grid.initialize();
...

grid.selectionManager.setMultiselect(true);
~~~`
    });
  }
}
