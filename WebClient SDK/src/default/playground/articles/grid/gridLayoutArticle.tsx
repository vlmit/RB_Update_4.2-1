import { FC } from 'react';
import { injectable } from '@tessa/application';
import { DemoForm, PlaygroundArticle } from 'tessa/ui/playground/chunk';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';
import { MockDataProvider } from './data/mockDataProvider';
import { IGridLayout } from 'components/cardElements/grid';
import { GridFactory, GridViewModel, DefaultGridLayouts, Grid } from 'ui/grid';

@injectable()
export class GridLayoutArticle extends PlaygroundArticle {
  override getSettings(): PlaygroundArticleSettings {
    return {
      name: 'Controls/Grid/Layout',
      description: 'You can override appearance of the grid through custom layouts.'
    };
  }

  override async initialize(): Promise<void> {
    this.addBlock({
      caption: 'Create grid without header',
      description: 'Custom grid component based on the default one.',
      props: async () => {
        const grid = GridFactory.createBase(MockDataProvider.getSimpleGridArgs());

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
          <HeadlessGrid viewModel={grid} />
        </DemoForm>
      ),
      code: `
~~~jsx
type HeadlessGridProps = {
  viewModel: GridViewModel;
};

const EmptyHeader = () => {
  return null;
};

const HeadlessGridLayouts = DefaultGridLayouts.map(layout => {
  const headless: IGridLayout = {
    ...layout,
    headerRow: EmptyHeader
  };

  return headless;
});

const HeadlessGrid: FC<HeadlessGridProps> = ({ viewModel }) => {
  return <Grid viewModel={viewModel} layouts={HeadlessGridLayouts} />;
};
...
<HeadlessGrid viewModel={grid} />
~~~`
    });
  }
}

type HeadlessGridProps = {
  viewModel: GridViewModel;
};

const EmptyHeader = () => {
  return null;
};

const HeadlessGridLayouts = DefaultGridLayouts.map(layout => {
  const headless: IGridLayout = {
    ...layout,
    headerRow: EmptyHeader
  };

  return headless;
});

export const HeadlessGrid: FC<HeadlessGridProps> = ({ viewModel }) => {
  return <Grid viewModel={viewModel} layouts={HeadlessGridLayouts} />;
};
