import { StorageHelper } from '@tessa/core';
import { injectable } from '@tessa/application';
import { DemoForm, PlaygroundArticle } from 'tessa/ui/playground/chunk';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';
import { AutocompleteProperty } from 'tessa/ui/propertyGrid';
import {
  DefaultGridSettings,
  DefaultSortingColumnSettings,
  Grid,
  GridFactory,
  GridHelper,
  GridSettings,
  GridSettingsControllerToken,
  GridSettingsViewContext,
  GridSortDirection,
  GridSortingSelector,
  GridSortingSettingsHandler,
  GridViewModel,
  IGridSettingsHandler,
  IGridSettingsView,
  LocalStorageGridSettingsProvider
} from 'ui/grid';
import { MockDataProvider } from './data/mockDataProvider';

@injectable()
export class GridSettingsArticle extends PlaygroundArticle {
  override getSettings(): PlaygroundArticleSettings {
    return {
      name: 'Controls/Grid/Settings'
    };
  }

  override async initialize(): Promise<void> {
    this.addBlock({
      caption: 'Create grid with settings enabled, by default they are disabled.',
      props: async () => {
        const grid = GridFactory.createDefault(MockDataProvider.getSimpleGridArgs());

        await grid.initialize();

        const controller = grid.getController(GridSettingsControllerToken);
        if (controller) {
          controller.available = true;
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
await grid.initialize();

const controller = grid.getController(GridSettingsControllerToken);
if (controller) {
  controller.available = true;
}
~~~`
    });

    this.addBlock({
      caption: 'Save settings to a local storage',
      description:
        'By default, settings are not stored anywhere. You need to manually specify settings provider.',
      props: async () => {
        const grid = GridFactory.createDefault(MockDataProvider.getSimpleGridArgs());

        grid.extensionContainer.addHooks({
          gridInitializing: async context => {
            const controller = context.grid.getController(GridSettingsControllerToken);
            if (controller) {
              controller.available = true;
              controller.setProvider(
                // In order to use LocalStorageGridSettingsProvider you need to specify unique key for a grid
                new LocalStorageGridSettingsProvider('GridArticle_Settings_Grid')
              );
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
  gridInitializing: async context => {
    const controller = context.grid.getController(GridSettingsControllerToken);
    if (controller) {
      controller.available = true;
      controller.setProvider(
        // In order to use LocalStorageGridSettingsProvider you need to specify unique key for a grid
        new LocalStorageGridSettingsProvider('GridArticle_Settings_Grid')
      );
    }
  }
});
~~~`
    });

    this.addBlock({
      caption: 'Extend settings',
      description:
        'To specify custom settings, you need to define a settings handler and add it to the settings manager. This example adds second level of sorting.',
      props: async () => {
        const grid = GridFactory.createDefault(MockDataProvider.getSimpleGridArgs());

        grid.extensionContainer.addHooks({
          gridInitializing: async context => {
            const controller = context.grid.getController(GridSettingsControllerToken);
            if (controller) {
              context.grid.sortManager.sortByRule = (column, columns, e) =>
                GridHelper.calculateSortingColumns(column, columns, e, 2);

              controller.available = true;
              const firstLevelSorting = controller.handlers.find(
                handlerInfo => handlerInfo.handler instanceof GridSortingSettingsHandler
              );
              if (firstLevelSorting) {
                controller.addSettingsHandler(
                  new SecondLevelSortingGridSettingsHandler(),
                  (firstLevelSorting.order ?? 0) + 1
                );
              }
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
      )
    });
  }
}

export class SecondLevelSortingGridSettingsHandler implements IGridSettingsHandler {
  //#region fields

  protected _selector: GridSortingSelector | null = null;

  //#endregion

  //#region Settings handler

  async initialize(_grid: GridViewModel): Promise<void> {
    // в обработчике первого уровня сортировке уже находится нужная логика
  }

  async applySettings(_grid: GridViewModel, _settings: GridSettings): Promise<void> {
    // в обработчике первого уровня сортировке уже находится нужная логика
  }

  async buildSettings(_grid: GridViewModel, _settings: GridSettings): Promise<void> {
    // в обработчике первого уровня сортировке уже находится нужная логика
  }

  async modifySettingsView(
    grid: GridViewModel,
    settings: GridSettings,
    context: GridSettingsViewContext
  ): Promise<void> {
    const wrapper = new DefaultGridSettings(settings);
    const sortColumn = wrapper.sortingColumns.length > 1 ? wrapper.sortingColumns[1] : null;
    const gridColumn = sortColumn ? grid.columnsMap.get(sortColumn.columnId) : null;
    const direction: GridSortDirection | null = sortColumn
      ? sortColumn.ascending
        ? 'ascending'
        : 'descending'
      : null;

    this._selector = new GridSortingSelector(
      grid,
      'SecondLevelSorting',
      '$Views_ColumnSettings_SecondLevelSorting',
      gridColumn,
      direction
    );

    context.builder.addProperty(this._selector.autocomplete).onGridCreated(async grid => {
      const firstLevelProperty = grid.findProperty<AutocompleteProperty>('FirstLevelSorting');
      if (!firstLevelProperty) {
        this._selector!.autocomplete.visibility = false;
        return;
      }

      const firstLevelSorting = StorageHelper.tryGet<GridSortingSelector>(
        firstLevelProperty.info,
        'sortingSelector'
      );

      if (!firstLevelSorting) {
        this._selector!.autocomplete.visibility = false;
        return;
      }

      this._selector!.parent = firstLevelSorting;
      await this._selector!.initialize();
    });
  }

  async handleSettingsViewSave(
    _grid: GridViewModel,
    settings: GridSettings,
    _view: IGridSettingsView
  ): Promise<void> {
    const wrapper = new DefaultGridSettings(settings);
    if (this._selector?.selectedColumn && this._selector.autocomplete.visibility) {
      const column = new DefaultSortingColumnSettings();
      column.columnId = this._selector.selectedColumn.id;
      column.ascending = this._selector.direction === 'ascending';
      wrapper.sortingColumns.push(column);
    }

    this._selector?.dispose();
    this._selector = null;
  }

  dispose(): void {
    this._selector?.dispose();
  }

  //#endregion
}
