import { injectable } from '@tessa/application';
import { Tree, TreeDataSource, TreeViewModel } from 'ui';
import { PlaygroundArticle, DemoForm } from 'tessa/ui/playground/chunk';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';
import { MenuAction } from 'tessa/ui';
import { dataSource, nodes } from './mock/data';

@injectable()
export class TreeArticle extends PlaygroundArticle {
  override getSettings(): PlaygroundArticleSettings {
    return {
      name: 'Controls/Tree/Basic',
      description:
        'A hierarchical UI element that displays nested data structures (like file directories or organizational charts) using collapsible parent-child nodes.',
      order: 1
    };
  }

  override async initialize(): Promise<void> {
    this.addBlock({
      caption: '',
      props: async () => {
        const viewModel = new TreeViewModel(dataSource);
        await viewModel.initialize();

        return {
          viewModel
        };
      },
      view: ({ viewModel }) => (
        <DemoForm customStyles={css => css({ flexDirection: 'column' })}>
          <Tree viewModel={viewModel} />
        </DemoForm>
      )
    });

    this.addBlock({
      caption: 'Context menu',
      props: async () => {
        const dataSource = new TreeDataSource();
        dataSource.addItems(...nodes);

        const viewModel = new TreeViewModel(dataSource);
        viewModel.treeIconSettings.expandIconVariant = 'arrow';
        await viewModel.initialize();

        viewModel.onContextMenu.add(ctx => {
          const { actions, item } = ctx;
          actions.push(
            MenuAction.create({
              type: 'normal',
              name: 'Select',
              caption: 'Select',
              action: async () => {
                viewModel.selectItem(item);
              }
            }),
            MenuAction.create({
              type: 'normal',
              name: 'Delete',
              caption: 'Delete',
              action: async () => {
                viewModel.removeItems(item);
              }
            })
          );
        });

        return {
          viewModel
        };
      },
      view: ({ viewModel }) => (
        <DemoForm customStyles={css => css({ flexDirection: 'column' })}>
          <Tree viewModel={viewModel} />
        </DemoForm>
      )
    });
  }
}
