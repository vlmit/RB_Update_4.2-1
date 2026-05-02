import { observer } from 'mobx-react-lite';
import { injectable } from '@tessa/application';
import { PlaygroundArticle, DemoForm } from 'tessa/ui/playground/chunk';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';
import { SliderViewModel } from 'ui/slider/sliderViewModel';
import { Slider } from 'ui/slider';

@injectable()
export class SliderArticle extends PlaygroundArticle {
  override getSettings(): PlaygroundArticleSettings {
    return {
      name: 'Controls/Slider'
    };
  }

  override async initialize(): Promise<void> {
    this.addBlock({
      caption: 'Default',
      props: async () => {
        const viewModel = new SliderViewModel();
        await viewModel.initialize();

        return {
          viewModel
        };
      },
      view: ({ viewModel }) => (
        <DemoForm customStyles={css => css({ flexDirection: 'column' })}>
          <Slider viewModel={viewModel} />
        </DemoForm>
      )
    });
    this.addBlock({
      caption: 'Custom values',
      props: async () => {
        const viewModel = new SliderViewModel();
        viewModel.step = 10;
        viewModel.max = 500;
        viewModel.min = 0;
        viewModel.value = 100;

        await viewModel.initialize();

        return {
          viewModel
        };
      },
      view: observer(({ viewModel }) => (
        <DemoForm customStyles={css => css({ flexDirection: 'column' })}>
          <div
            style={{ backgroundColor: '#344764', height: '50px', width: `${viewModel.value}px` }}
          ></div>
          <Slider viewModel={viewModel} />
        </DemoForm>
      ))
    });

    this.addBlock({
      caption: 'Disabled',
      props: async () => {
        const viewModel = new SliderViewModel();
        viewModel.value = 70;
        viewModel.disabled = true;

        await viewModel.initialize();

        return {
          viewModel
        };
      },
      view: ({ viewModel }) => (
        <DemoForm customStyles={css => css({ flexDirection: 'column' })}>
          <Slider viewModel={viewModel} />
        </DemoForm>
      )
    });
  }
}
