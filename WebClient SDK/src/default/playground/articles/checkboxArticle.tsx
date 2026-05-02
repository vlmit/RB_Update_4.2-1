import { injectable } from '@tessa/application';
import { ShadowPropsHelper } from '@tessa/ui';
import { Checkbox } from 'ui/checkbox/checkbox';
import { CheckboxViewModel } from 'ui/checkbox/checkboxViewModel';
import { PlaygroundArticle, DemoForm } from 'tessa/ui/playground/chunk';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';

@injectable()
export class CheckboxArticle extends PlaygroundArticle {
  override getSettings(): PlaygroundArticleSettings {
    return {
      name: 'Controls/Checkbox',
      description: 'Checkboxes allow the user to select one or more items from a set.',
      order: 1
    };
  }

  override async initialize(): Promise<void> {
    this.addBlock({
      caption: 'Basic',
      props: async () => {
        const checkbox1 = new CheckboxViewModel();
        await checkbox1.initialize();

        const checkbox2 = new CheckboxViewModel();
        await checkbox2.initialize();
        checkbox2.disabled = true;

        const checkbox3 = new CheckboxViewModel();
        await checkbox3.initialize();
        checkbox3.disabled = true;
        checkbox3.checked = true;

        return {
          checkbox1,
          checkbox2,
          checkbox3
        };
      },
      view: ({ checkbox1, checkbox2, checkbox3 }) => (
        <DemoForm
          customStyles={css => css`
            gap: 10px;
          `}
        >
          <Checkbox viewModel={checkbox1} />
          <Checkbox viewModel={checkbox2} />
          <Checkbox viewModel={checkbox3} />
        </DemoForm>
      ),
      code: `
~~~js
const checkbox1 = new CheckboxViewModel();
await checkbox1.initialize();

const checkbox2 = new CheckboxViewModel();
await checkbox2.initialize();
checkbox2.disabled = true;

const checkbox3 = new CheckboxViewModel();
await checkbox3.initialize();
checkbox3.disabled = true;
checkbox3.checked = true;
~~~`
    });

    this.addBlock({
      caption: 'Caption',
      props: async () => {
        const checkbox1 = new CheckboxViewModel();
        await checkbox1.initialize();
        checkbox1.caption = 'simple checkbox';

        const checkbox2 = new CheckboxViewModel();
        await checkbox2.initialize();
        checkbox2.caption = 'disabled checkbox';
        checkbox2.disabled = true;
        checkbox2.checked = true;

        return {
          checkbox1,
          checkbox2
        };
      },
      view: ({ checkbox1, checkbox2 }) => (
        <DemoForm
          customStyles={css => css`
            flex-direction: column;
            gap: 5px;
          `}
        >
          <Checkbox viewModel={checkbox1} />
          <Checkbox viewModel={checkbox2} />
        </DemoForm>
      ),
      code: `
~~~js
const checkbox1 = new CheckboxViewModel();
await checkbox1.initialize();
checkbox1.caption = 'simple checkbox';

const checkbox2 = new CheckboxViewModel();
await checkbox2.initialize();
checkbox2.caption = 'disabled checkbox';
checkbox2.disabled = true;
checkbox2.checked = true;
~~~`
    });

    this.addBlock({
      caption: 'Switch',
      props: async () => {
        const checkbox1 = new CheckboxViewModel();
        await checkbox1.initialize();
        checkbox1.type = 'switch';
        checkbox1.caption = 'simple switch';

        const checkbox2 = new CheckboxViewModel();
        await checkbox2.initialize();
        checkbox2.type = 'switch';
        checkbox2.caption = 'disabled switch';
        checkbox2.disabled = true;
        checkbox2.checked = true;

        return {
          checkbox1,
          checkbox2
        };
      },
      view: ({ checkbox1, checkbox2 }) => (
        <DemoForm
          customStyles={css => css`
            flex-direction: column;
            gap: 5px;
          `}
        >
          <Checkbox viewModel={checkbox1} />
          <Checkbox viewModel={checkbox2} />
        </DemoForm>
      ),
      code: `
~~~js
const checkbox1 = new CheckboxViewModel();
await checkbox1.initialize();
checkbox1.type = 'switch';
checkbox1.caption = 'simple switch';

const checkbox2 = new CheckboxViewModel();
await checkbox2.initialize();
checkbox2.type = 'switch';
checkbox2.caption = 'disabled switch';
checkbox2.disabled = true;
checkbox2.checked = true;
~~~`
    });

    this.addBlock({
      caption: 'Customization',
      description: 'Here is an example of customizing the component. Open dev-tool console (F12)',
      props: async () => {
        const checkbox1 = new CheckboxViewModel();
        checkbox1.caption = 'click me!';
        await checkbox1.initialize();

        checkbox1.onReady.add(() => console.log('checkbox ready'));
        checkbox1.styles.add(
          css => css`
            & .caption {
              color: ${checkbox1.checked ? 'red' : 'blue'};
            }
          `
        );
        checkbox1.handlersContainer.onMouseDown.add(() => console.log('checkbox mouseDown'));
        checkbox1.dataAttributes.set('my-attr', 'true');
        checkbox1.focusManager.onFocus.add(() => console.log('checkbox focused'));

        ShadowPropsHelper.add(checkbox1, 'caption', prev => {
          const caption = prev();
          return checkbox1.focusManager.isFocused ? `${caption} (focused)` : caption;
        });

        ShadowPropsHelper.add(checkbox1.tooltip, 'text', prev => {
          const text = prev();
          return checkbox1.focusManager.isFocused ? `${text} (focused)` : text;
        });

        return {
          checkbox1
        };
      },
      view: ({ checkbox1 }) => (
        <DemoForm>
          <Checkbox viewModel={checkbox1} />
        </DemoForm>
      ),
      code: `
~~~js
const checkbox1 = new CheckboxViewModel();
checkbox1.caption = 'click me!';
await checkbox1.initialize();

checkbox1.onReady.add(() => console.log('checkbox ready'));
checkbox1.styles.add(
  css =>
    css\`
      & .caption {
        color: \${checkbox1.checked ? 'red' : 'blue'};
      }
    \`
);
checkbox1.handlersContainer.onMouseDown.add(() => console.log('checkbox mouseDown'));
checkbox1.dataAttributes.set('my-attr', 'true');
checkbox1.focusManager.onFocus.add(() => console.log('checkbox focused'));

ShadowPropsHelper.add(checkbox1, 'caption', prev => {
  const caption = prev();
  return checkbox1.focusManager.isFocused ? \`\${caption} (focused)\` : caption;
});

ShadowPropsHelper.add(checkbox1.tooltip, 'text', prev => {
  const text = prev();
  return checkbox1.focusManager.isFocused ? \`\${text} (focused)\` : text;
});
~~~`
    });
  }
}
