import { injectable } from '@tessa/application';
import { UIButton, UIButtonComponent } from 'tessa/ui';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';
import { PlaygroundArticle, DemoForm } from 'tessa/ui/playground/chunk';

@injectable()
export class ExampleArticle extends PlaygroundArticle {
  override getSettings(): PlaygroundArticleSettings {
    return {
      name: '42_Example',
      description: 'This is an example article provided by 42_playgroundArticleRegistrator'
    };
  }

  override async initialize(): Promise<void> {
    this.addBlock({
      caption: 'UIButton',
      description: 'A test button. Open your browser console to see the output.',
      props: async () => {
        const button = UIButton.create({
          name: 'Test button',
          caption: 'Test button',
          theme: 'primary',
          type: 'normal',
          icon: 'icon-thin-285',
          buttonAction: () => console.log('test button clicked')
        });
        return {
          button
        };
      },
      view: ({ button }) => (
        <DemoForm
          customStyles={css => css`
            gap: 10px;
          `}
        >
          <UIButtonComponent viewModel={button} />
        </DemoForm>
      ),
      code: `
~~~js
const button = UIButton.create({
  name: 'Test button',
  caption: 'Test button',
  theme: 'primary',
  type: 'normal',
  icon: 'icon-thin-285',
  buttonAction: () => console.log('test button pressed')
});
~~~`
    });

    this.addBlock({
      caption: 'Another block',
      description:
        "Multiple blocks can be added to a single article. There's no limit except common sense."
    });
  }
}
