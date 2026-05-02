import { injectable } from '@tessa/application';
import { Tree, TreeDataSource, TreeItem, TreeViewModel } from 'ui';
import { PlaygroundArticle, DemoForm } from 'tessa/ui/playground/chunk';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';
import { Grid } from 'ui/grid';
import { nodes, createTable } from './mock/data';

@injectable()
export class TreeDragNDropArticle extends PlaygroundArticle {
  override getSettings(): PlaygroundArticleSettings {
    return {
      name: 'Controls/Tree/Drag-N-Drop',
      order: 3
    };
  }

  override async initialize(): Promise<void> {
    this.addBlock({
      caption: 'Drop',
      description:
        'Enables drop support for tree items. Highlights empty tree areas during drag operations and allows dropping items to add new elements.',
      props: async () => {
        const dataSource = new TreeDataSource();
        dataSource.addItems(...nodes);
        const viewModel = new TreeViewModel(dataSource);
        await viewModel.initialize();
        viewModel.dropEnabled = true;

        const gridViewModel = await createTable();

        viewModel.onDropItem.add(async ctx => {
          const { targetItem, event } = ctx;

          const transferItem = event.dataTransfer.getData('data');

          if (!transferItem) {
            return;
          }

          const row = gridViewModel.rows.find(x => x.id === transferItem);
          const sourceRow = row && row.sourceRow;

          if (
            sourceRow &&
            typeof row?.sourceRow === 'object' &&
            'CarName' in row?.sourceRow &&
            typeof row.sourceRow.CarName === 'string'
          ) {
            const name = row?.sourceRow.CarName;

            if (viewModel.items.find(item => item.item.name === name)) {
              console.log(`The tree already have item with name ${name}`);
              return;
            }

            const newTreeItem: TreeItem = {
              name: name,
              caption: name,
              parent: targetItem?.item.name,
              icon: 'icon-thin-262'
            };

            await viewModel.addItem(newTreeItem);
            if (targetItem && !targetItem.isExpanded) {
              targetItem.toggleExpand();
            }
          }
        });

        return {
          viewModel,
          gridViewModel
        };
      },
      view: ({ viewModel, gridViewModel }) => (
        <article style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
          <DemoForm
            customStyles={css =>
              css({
                flexDirection: 'column',
                margin: 'initial !important',
                justifyContent: 'space-between',
                gap: '20px'
              })
            }
          >
            <Tree viewModel={viewModel} />
            <Grid viewModel={gridViewModel} />
          </DemoForm>
        </article>
      )
    });
    this.addBlock({
      caption: 'Empty tree',
      description:
        'Define the text shown in place of tree content when no nodes exist. Specify emptyMessage prop',
      props: async () => {
        const dataSource_2 = new TreeDataSource();
        const viewModel_2 = new TreeViewModel(dataSource_2);
        await viewModel_2.initialize();
        viewModel_2.dropEnabled = true;

        const gridViewModel_2 = await createTable();

        viewModel_2.onDropItem.add(async ctx => {
          const { targetItem, event } = ctx;

          const transferItem = event.dataTransfer.getData('data');

          if (!transferItem) {
            return;
          }

          const row = gridViewModel_2.rows.find(x => x.id === transferItem);
          const sourceRow = row && row.sourceRow;

          if (
            sourceRow &&
            typeof row?.sourceRow === 'object' &&
            'CarName' in row?.sourceRow &&
            typeof row.sourceRow.CarName === 'string'
          ) {
            const name = row?.sourceRow.CarName;

            if (viewModel_2.items.find(item => item.item.name === name)) {
              console.warn(`The tree already have item with name ${name}`);
              return;
            }

            const newTreeItem: TreeItem = {
              name: name,
              caption: name,
              parent: targetItem?.item.name
            };

            await viewModel_2.addItem(newTreeItem);
            if (targetItem && !targetItem.isExpanded) {
              targetItem.toggleExpand();
            }
          }
        });

        return {
          viewModel_2,
          gridViewModel_2
        };
      },
      view: ({ viewModel_2, gridViewModel_2 }) => (
        <DemoForm
          customStyles={css =>
            css({
              flexDirection: 'column',
              margin: 'initial !important',
              justifyContent: 'space-between',
              gap: '20px'
            })
          }
        >
          <Tree viewModel={viewModel_2} />
          <Grid viewModel={gridViewModel_2} />
        </DemoForm>
      )
    });
  }
}
