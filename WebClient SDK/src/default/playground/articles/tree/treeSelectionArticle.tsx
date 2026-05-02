import { injectable } from '@tessa/application';
import { Tree, TreeViewModel } from 'ui';
import { PlaygroundArticle, DemoForm } from 'tessa/ui/playground/chunk';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';

import { dataSource } from './mock/data';

@injectable()
export class TreeSelectionArticle extends PlaygroundArticle {
  override getSettings(): PlaygroundArticleSettings {
    return {
      name: 'Controls/Tree/Selection',
      description:
        'This implementation provides a flexible item selection system for tree structures, supporting both single and multiple selection modes while preserving UI state integrity.',
      order: 2
    };
  }

  override async initialize(): Promise<void> {
    this.addBlock({
      caption: '',
      props: async () => {
        const viewModel = new TreeViewModel(dataSource);
        await viewModel.initialize();

        const viewModel2 = new TreeViewModel(dataSource);
        await viewModel2.initialize();
        viewModel2.selectionSettings.isMultiplySelection = true;
        viewModel2.onSelectionChanged.add(({ items }) => {
          console.log(items);
        });

        return {
          viewModel,
          viewModel2
        };
      },
      view: ({ viewModel, viewModel2 }) => (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
          <p style={{ fontWeight: 'bold' }}>Single</p>

          <DemoForm
            customStyles={css => css({ flexDirection: 'column', margin: 'initial !important' })}
          >
            <Tree viewModel={viewModel} />
          </DemoForm>

          <p style={{ fontWeight: 'bold' }}>Multiply</p>

          <DemoForm
            customStyles={css => css({ flexDirection: 'column', margin: 'initial !important' })}
          >
            <Tree viewModel={viewModel2} />
          </DemoForm>
        </div>
      )
    });
  }
}
