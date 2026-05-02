import { injectable } from '@tessa/application';
import { Checkbox } from 'ui/checkbox/checkbox';
import { CheckboxViewModel } from 'ui/checkbox/checkboxViewModel';
import {
  Grid,
  AddSearchGridExtension,
  GridDefaultSearchStrategy,
  GridFactory,
  GridSearchControllerToken,
  GridSearchViewModel
} from 'ui/grid';
import { DemoForm, PlaygroundArticle } from 'tessa/ui/playground/chunk';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';
import { MockDataProvider } from './data/mockDataProvider';

@injectable()
export class GridSearchArticle extends PlaygroundArticle {
  override getSettings(): PlaygroundArticleSettings {
    return {
      name: 'Controls/Grid/Filter and search'
    };
  }

  override async initialize(): Promise<void> {
    this.addBlock({
      caption: 'Add filter to the grid',
      description: 'Filter rows where release date is after 2020',
      props: async () => {
        const checkbox = new CheckboxViewModel();
        checkbox.caption = 'Filter';
        await checkbox.initialize();

        const grid = GridFactory.createBase(MockDataProvider.getSimpleGridArgs());
        await grid.initialize();

        grid.filterManager.addFilter(row => {
          if (!checkbox.checked) {
            return true;
          }

          const dateString = row.cellsMap.get('ReleaseDate')?.getValue<string>();
          if (!dateString) {
            return false;
          }

          const date = new Date(dateString);
          return date.getFullYear() > 2020;
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
grid.filterManager.addFilter(row => {
  if (!checkbox.checked) {
    return true;
  }

  const dateString = row.cellsMap.get('ReleaseDate')?.getValue<string>();
  if (!dateString) {
    return false;
  }

  const date = new Date(dateString);
  return date.getFullYear() > 2020;
  });
~~~`
    });

    this.addBlock({
      caption: 'Custom highlighting',
      description: 'Highlight first letter of text in every cell',
      props: async () => {
        const grid = GridFactory.createBase(MockDataProvider.getSimpleGridArgs());
        await grid.initialize();

        grid.highlightManager.addHighlightRule(cell => {
          if (!!cell.formattedValue && cell.formattedValue.length > 1) {
            return [{ from: 0, length: 1, className: 'highlight-blue' }];
          }

          return null;
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

            table.grid-table .highlighted-text.highlight-blue {
              background: #66aee96b;
            }
          `}
        >
          <Grid viewModel={grid} />
        </DemoForm>
      ),
      code: `
~~~jsx
grid.highlightManager.addHighlightRule(cell => {
  if (!!cell.formattedValue && cell.formattedValue.length > 1) {
    return [{ from: 0, length: 1, className: 'highlight-blue' }];
  }

  return null;
});
~~~`
    });

    this.addBlock({
      caption: 'Default search',
      description: `Search functionality is added by AddSearchGridExtension. It resolves GridSearchController and adds search box to the panel.
         \r\nThis extension is already added if you create default grid with the GridFactory, but you can also add it to a base grid yourself.`,
      props: async () => {
        const grid = GridFactory.createBase(MockDataProvider.getSimpleGridArgs());
        grid.extensionContainer.addExtension(AddSearchGridExtension);

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
grid.extensionContainer.addExtension(AddSearchGridExtension);
 `
    });

    this.addBlock({
      caption: 'Disable search in grid',
      description: 'Disabling through search manager disables grid search functionality.',
      props: async () => {
        const grid = GridFactory.createDefault(MockDataProvider.getSimpleGridArgs());
        await grid.initialize();

        const searchController = grid.getController(GridSearchControllerToken);
        if (searchController) {
          searchController.available = false;
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
const searchController = grid.getController(GridSearchControllerToken);
if (searchController) {
  searchController.available = false;
}
`
    });

    this.addBlock({
      caption: 'Disable only search box on the panel',
      description: 'Grid search is still can be called.',
      props: async () => {
        const grid = GridFactory.createDefault(MockDataProvider.getSimpleGridArgs());
        await grid.initialize();

        const search = grid.mainPanel.items.find(i => i instanceof GridSearchViewModel);
        if (search) {
          search.disabled = true;
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
const search = panel.items.find(i => i instanceof GridSearchViewModel);
if (search) {
  search.disabled = true;
}`
    });

    this.addBlock({
      caption: 'Make search case sensitive',
      props: async () => {
        const grid = GridFactory.createDefault(MockDataProvider.getSimpleGridArgs());

        grid.extensionContainer.addHooks({
          gridInitializing: async context => {
            const searchController = context.grid.getController(GridSearchControllerToken);
            if (searchController) {
              searchController.setSearchStrategy(new GridDefaultSearchStrategy(true));
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
grid.extensionContainer.addHooks({
  gridInitialized: async context => {
    const searchController = context.grid.getController(GridSearchControllerToken);
    if (searchController) {
      searchController.setSearchStrategy(new GridDefaultSearchStrategy(true));
    }
  }
});
`
    });
  }
}
