import { injectable } from '@tessa/application';
import { DemoForm, PlaygroundArticle } from 'tessa/ui/playground/chunk';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';
import { Grid, GridFactory, GridPagingViewModel } from 'ui/grid';
import { MockDataProvider } from './data/mockDataProvider';

@injectable()
export class GridPagingArticle extends PlaygroundArticle {
  override getSettings(): PlaygroundArticleSettings {
    return {
      name: 'Controls/Grid/Paging'
    };
  }

  override async initialize(): Promise<void> {
    this.addBlock({
      caption: 'Enable paging',
      description: 'Paging is disabled by default, so it needs to be turned on manually.',
      props: async () => {
        const grid = GridFactory.createDefault(MockDataProvider.getRandomGridArgs());
        await grid.initialize();

        grid.pagingManager.available = true;

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
grid.pagingManager.available = true;
~~~`
    });

    this.addBlock({
      caption: 'Change page size',
      props: async () => {
        const grid = GridFactory.createDefault(MockDataProvider.getRandomGridArgs());
        await grid.initialize();

        grid.pagingManager.available = true;
        await grid.pagingManager.applyOptions({
          pageSize: 2
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
await grid.pagingManager.applyOptions({
  pageSize: 2
});
~~~`
    });

    this.addBlock({
      caption: 'Allow optional paging',
      props: async () => {
        const grid = GridFactory.createDefault(MockDataProvider.getRandomGridArgs());
        await grid.initialize();

        grid.pagingManager.available = true;
        grid.pagingManager.allowOptional = true;

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
grid.pagingManager.allowOptional = true;
~~~`
    });

    this.addBlock({
      caption: 'Hide rows count',
      props: async () => {
        const grid = GridFactory.createDefault(MockDataProvider.getRandomGridArgs());

        grid.extensionContainer.addHooks({
          gridInitialized: ({ grid }) => {
            const paging = grid.mainPanel.items.find(i => i instanceof GridPagingViewModel);
            if (paging) {
              paging.rowCountVisibility = false;
            }
          }
        });

        await grid.initialize();

        grid.pagingManager.available = true;

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
  gridInitialized: ({ grid }) => {
    const paging = grid.mainPanel.items.find(i => i instanceof GridPagingViewModel);
    if (paging) {
      paging.rowCountVisibility = false;
    }
  }
});
~~~`
    });

    this.addBlock({
      caption: 'Always show pager',
      description:
        'By default, pager is only visible when there is more than one page. This behavior can be changed in GridPagingViewModel',
      props: async () => {
        const grid = GridFactory.createDefault(MockDataProvider.getRandomGridArgs(9));

        grid.extensionContainer.addHooks({
          gridInitialized: ({ grid }) => {
            const paging = grid.mainPanel.items.find(i => i instanceof GridPagingViewModel);
            if (paging) {
              paging.showMode = 'always';
            }
          }
        });

        await grid.initialize();

        grid.pagingManager.available = true;

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
  gridInitialized: ({ grid }) => {
    const paging = grid.mainPanel.items.find(i => i instanceof GridPagingViewModel);
    if (paging) {
      paging.showMode = 'always';
    }
  }
});
~~~`
    });
  }
}
