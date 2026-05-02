import { injectable } from '@tessa/application';
import { ShadowPropsHelper } from '@tessa/ui';
import { Checkbox } from 'ui/checkbox/checkbox';
import { CheckboxViewModel } from 'ui/checkbox/checkboxViewModel';
import { Button } from 'ui/button/button';
import { Button as ButtonViewModel } from 'ui/button/buttonViewModel';
import { GridFactory, Grid, GridViewModel } from 'ui/grid';
import { PlaygroundArticle, DemoForm } from 'tessa/ui/playground/chunk';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';
import { MockDataProvider } from './data/mockDataProvider';

@injectable()
export class GridPermissionsArticle extends PlaygroundArticle {
  override getSettings(): PlaygroundArticleSettings {
    return {
      name: 'Controls/Grid/Setting up permissions'
    };
  }

  override async initialize(): Promise<void> {
    this.addBlock({
      caption: 'Permissions for rows adding and deleting',
      props: async () => {
        const grid = GridFactory.createDefault(MockDataProvider.getSimpleGridArgs());

        const checkbox = new CheckboxViewModel();
        checkbox.caption = 'Allow add rows';
        await checkbox.initialize();

        grid.extensionContainer.addHooks({
          gridPermissionsInitializing: async context => {
            if (context.type === 'grid') {
              context.permissionsContainer.forbidAddRows(() => !checkbox.checked);
            } else if (context.type === 'row') {
              context.permissionsContainer.forbidDelete(
                row => row.cellsMap.get('Secondhand')?.getValue<boolean>() !== true
              );
            }
          }
        });

        await grid.initialize();

        return {
          grid,
          checkbox
        };
      },
      view: ({ grid, checkbox }) => {
        return (
          <DemoForm
            customStyles={css => css`
              gap: 10px;
              min-width: 350px;
              display: flex;
              flex-direction: column;
              max-height: 500px;

              .grid-buttons {
                display: flex;
                gap: 5px;
              }
            `}
          >
            <Grid viewModel={grid} />
            <Checkbox viewModel={checkbox} />
          </DemoForm>
        );
      },
      code: `
~~~jsx
const grid = new GridViewModel();
...
const checkbox = new CheckboxViewModel();
checkbox.caption = 'Allow add rows';
await checkbox.initialize();

grid.extensionContainer.addHooks({
  gridPermissionsInitializing: async context => {
    if (context.type === 'grid') {
      context.permissionsContainer.forbidAddRows(() => !checkbox.checked);
    } else if (context.type === 'row') {
      context.permissionsContainer.forbidDelete(
        row => row.cellsMap.get('Secondhand')?.getValue<boolean>() !== true
      );
    }
  }
});

await grid.initialize();
~~~`
    });

    this.addBlock({
      caption: 'Change permissions after initialization',
      props: async () => {
        const grid = GridFactory.createDefault(MockDataProvider.getSimpleGridArgs());
        await grid.initialize();

        const buttons = this.createAllowForbidButtons(grid);

        return {
          grid,
          buttons
        };
      },
      view: ({ grid, buttons }) => {
        const buttonsComponents = buttons.map(b => <Button key={b.name} viewModel={b} />);

        return (
          <DemoForm
            customStyles={css => css`
              gap: 10px;
              min-width: 350px;
              display: flex;
              flex-direction: column;
              max-height: 500px;

              .grid-buttons {
                display: flex;
                gap: 5px;
              }
            `}
          >
            <Grid viewModel={grid} />
            <div className="grid-buttons">{buttonsComponents}</div>
          </DemoForm>
        );
      },
      code: `
~~~jsx
const forbidButton = ButtonViewModel.create({
  name: 'ForbidDelete',
  type: 'normal',
  theme: 'control',
  caption: 'Forbid to delete',
  disabled: () => !grid.selectionManager.selectedRow?.permissionsContainer?.canBeDeleted,
  buttonAction: () => {
    grid.selectionManager.selectedRow!.permissionsContainer.forbidDelete(true);
  }
});

// мы не разрешаем удалять явно, мы отзываем свой предыдущий запрет на удаление
// если по каким-то другим причинам строка не может быть удалена, то нельзя зафорсить разрешение
const allowButton = ButtonViewModel.create({
  name: 'AllowDelete',
  type: 'normal',
  theme: 'control',
  caption: 'Allow to delete',
  disabled: () =>
    grid.selectionManager.selectedRow?.permissionsContainer?.canBeDeleted === true,
  buttonAction: () => {
    grid.selectionManager.selectedRow!.permissionsContainer.forbidDelete(false);
  }
});
~~~`
    });

    this.addBlock({
      caption: 'Static permissions',
      props: async () => {
        const grid = GridFactory.createDefault(MockDataProvider.getSimpleGridArgs());

        grid.setPermissionsMode('static');

        grid.extensionContainer.addHooks({
          gridPermissionsInitializing: async context => {
            if (context.type === 'grid') {
              context.permissionsContainer.setReadonly(true);
            }
          }
        });

        await grid.initialize();

        const buttons = this.createAllowForbidButtons(grid);

        return {
          grid,
          buttons
        };
      },
      view: ({ grid, buttons }) => {
        const buttonsComponents = buttons.map(b => <Button key={b.name} viewModel={b} />);

        return (
          <DemoForm
            customStyles={css => css`
              gap: 10px;
              min-width: 350px;
              display: flex;
              flex-direction: column;
              max-height: 500px;

              .grid-buttons {
                display: flex;
                gap: 5px;
              }
            `}
          >
            <Grid viewModel={grid} />
            <div className="grid-buttons">{buttonsComponents}</div>
          </DemoForm>
        );
      },
      code: `
~~~jsx
const grid = new GridViewModel();
...
grid.setPermissionsMode('static');

grid.extensionContainer.addHooks({
  gridPermissionsInitializing: async context => {
    if (context.type === 'grid') {
      // права не будут меняться после инициализации грида, так как мы поставили режим прав 'static'
      context.permissionsContainer.setReadonly(true);
    }
  }
});

await grid.initialize();
~~~`
    });

    this.addBlock({
      caption: 'Disabled grid',
      props: async () => {
        const grid = GridFactory.createDefault(MockDataProvider.getSimpleGridArgs());

        await grid.initialize();

        const checkbox = new CheckboxViewModel();
        checkbox.caption = 'Disable grid';
        await checkbox.initialize();

        ShadowPropsHelper.add(grid, 'disabled', () => checkbox.checked);

        return {
          grid,
          checkbox
        };
      },
      view: ({ grid, checkbox }) => {
        return (
          <DemoForm
            customStyles={css => css`
              gap: 10px;
              min-width: 350px;
              display: flex;
              flex-direction: column;
              max-height: 500px;

              .grid-buttons {
                display: flex;
                gap: 5px;
              }
            `}
          >
            <Grid viewModel={grid} />
            <Checkbox viewModel={checkbox} />
          </DemoForm>
        );
      },
      code: `
~~~jsx
const grid = new GridViewModel();
...
await grid.initialize();

const checkbox = new CheckboxViewModel();
checkbox.caption = 'Disable grid';
await checkbox.initialize();

ShadowPropsHelper.add(grid, 'disabled', () => checkbox.checked);
~~~`
    });
  }

  private createAllowForbidButtons(grid: GridViewModel): ButtonViewModel[] {
    const forbidButton = ButtonViewModel.create({
      name: 'ForbidDelete',
      type: 'normal',
      theme: 'control',
      caption: 'Forbid to delete',
      disabled: () => !grid.selectionManager.selectedRow?.permissionsContainer?.canBeDeleted,
      buttonAction: () => {
        grid.selectionManager.selectedRow!.permissionsContainer.forbidDelete(true);
      }
    });

    const allowButton = ButtonViewModel.create({
      name: 'AllowDelete',
      type: 'normal',
      theme: 'control',
      caption: 'Allow to delete',
      disabled: () =>
        grid.selectionManager.selectedRow?.permissionsContainer?.canBeDeleted === true,
      buttonAction: () => {
        grid.selectionManager.selectedRow!.permissionsContainer.forbidDelete(false);
      }
    });

    return [allowButton, forbidButton];
  }
}
