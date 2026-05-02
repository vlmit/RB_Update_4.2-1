import { Profiler } from 'react';
import { injectable } from '@tessa/application';
import { Tree, TreeDataSource, TreeItem, TreeViewModel } from 'ui';
import { MenuAction } from 'tessa/ui';
import { PlaygroundArticle, DemoForm } from 'tessa/ui/playground/chunk';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';

@injectable()
export class TreePerformanceArticle extends PlaygroundArticle {
  override getSettings(): PlaygroundArticleSettings {
    return {
      name: 'Controls/Tree/Performance'
    };
  }

  override async initialize(): Promise<void> {
    this.addBlock({
      caption: 'Over nine thousands items',
      props: async () => {
        const nodes = this.generateRandomTree(10000);

        const dataSource = new TreeDataSource();
        dataSource.addItems(...nodes);

        const viewModel = new TreeViewModel(dataSource);

        viewModel.dimensions.stretchVertically = false;
        viewModel.dimensions.maxHeight = 500;

        viewModel.searchBoxVisibility = true;

        viewModel.onContextMenu.add(ctx => {
          const { actions, item } = ctx;
          actions.push(
            MenuAction.create({
              type: 'normal',
              name: 'Add',
              caption: 'Add',
              action: async () => {
                const nodeName = `node_${viewModel.items.length}`;
                const newNode: TreeItem = {
                  name: nodeName,
                  caption: nodeName,
                  parent: item.item.name
                };
                viewModel.addItem(newNode);
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

        console.time('TreeViewModel');
        await viewModel.initialize();
        console.timeEnd('TreeViewModel');

        return {
          viewModel
        };
      },
      view: ({ viewModel }) => {
        return (
          <DemoForm customStyles={css => css({ flexDirection: 'column' })}>
            <Profiler
              id="TreeComponent"
              onRender={(_id, _phase, actualTime) => {
                console.log(`Tree render: ${actualTime}ms`);
              }}
            >
              <Tree viewModel={viewModel} />
            </Profiler>
          </DemoForm>
        );
      }
    });
  }

  private generateRandomTree(numNodes: number): ReadonlyArray<TreeItem> {
    if (numNodes <= 0) {
      return [];
    }

    const nodes = new Map<string, TreeItem>();

    // Generate remaining nodes
    for (let i = 0; i <= numNodes; i++) {
      const nodeName = `node_${i}`;

      const noParent = i < 100;

      const parentNames = Array.from(nodes.keys());
      const parentName = parentNames[Math.floor(Math.random() * parentNames.length)];

      const newNode: TreeItem = {
        name: nodeName,
        caption: nodeName,
        parent: noParent ? null : parentName
      };
      nodes.set(nodeName, newNode);
    }

    return Array.from(nodes.values());
  }
}
