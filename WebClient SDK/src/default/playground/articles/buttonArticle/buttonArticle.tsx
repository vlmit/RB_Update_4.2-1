import { FC } from 'react';
import { injectable } from '@tessa/application';
import { ShadowPropsHelper } from '@tessa/ui';
import { Button as ButtonViewModel } from 'ui/button/buttonViewModel';
import { Button } from 'ui/button/button';
import { DropdownViewModel } from 'ui/dropdown/dropdownViewModel';
import { CheckboxViewModel } from 'ui/checkbox/checkboxViewModel';
import { Checkbox } from 'ui';
import { showMessage } from 'tessa/ui/tessaDialog';
import { PlaygroundArticle, DemoForm } from 'tessa/ui/playground/chunk';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';

@injectable()
export class ButtonArticle extends PlaygroundArticle {
  override getSettings(): PlaygroundArticleSettings {
    return {
      name: 'Controls/Button'
    };
  }

  override async initialize(): Promise<void> {
    this.addBlock({
      caption: 'Buttons of different types',
      props: async () => {
        const buttons = [
          ButtonViewModel.create({
            name: 'Button1',
            caption: 'Test',
            icon: 'm-plus',
            type: 'normal',
            theme: 'primary'
          }),
          ButtonViewModel.create({
            name: 'Button2',
            caption: 'Test',
            icon: 'm-plus',
            type: 'small',
            theme: 'primary'
          }),

          ButtonViewModel.create({
            name: 'Button3',
            caption: 'Test',
            icon: 'm-plus',
            type: 'large',
            theme: 'primary'
          }),

          ButtonViewModel.create({
            name: 'Button4',
            icon: 'm-plus',
            type: 'icon',
            theme: 'primary'
          }),

          ButtonViewModel.create({
            name: 'Button5',
            icon: 'm-plus',
            type: 'icon-s',
            theme: 'primary'
          }),

          ButtonViewModel.create({
            name: 'Button6',
            caption: 'Test',
            icon: 'm-plus',
            type: 'toolbar',
            theme: 'primary'
          })
        ];

        return {
          buttons
        };
      },
      view: ({ buttons }) => {
        const buttonsComponents = buttons.map(b => <Button viewModel={b} key={b.name} />);

        return (
          <DemoForm
            customStyles={css => css`
              gap: 10px;
            `}
          >
            {buttonsComponents}
          </DemoForm>
        );
      },
      code: `
~~~js
button.type = 'normal';

button.type = 'small';

button.type = 'large';

button.type = 'icon';

button.type = 'icon-s';

button.type = 'toolbar';
~~~`
    });

    this.addBlock({
      caption: 'Buttons of different themes',
      props: async () => {
        const buttons = [
          ButtonViewModel.create({
            name: 'Button1',
            caption: 'Test',
            icon: 'm-plus',
            type: 'normal',
            theme: 'primary'
          }),
          ButtonViewModel.create({
            name: 'Button2',
            caption: 'Test',
            icon: 'm-plus',
            type: 'normal',
            theme: 'secondary'
          }),
          ButtonViewModel.create({
            name: 'Button4',
            caption: 'Test',
            icon: 'm-plus',
            type: 'normal',
            theme: 'toolbar'
          }),
          ButtonViewModel.create({
            name: 'Button5',
            caption: 'Test',
            icon: 'm-plus',
            type: 'normal',
            theme: 'control'
          }),
          ButtonViewModel.create({
            name: 'Button6',
            caption: 'Test',
            icon: 'm-plus',
            type: 'normal',
            theme: 'tab'
          }),
          ButtonViewModel.create({
            name: 'Button7',
            caption: 'Test',
            icon: 'm-plus',
            type: 'normal',
            theme: 'transparent'
          }),
          ButtonViewModel.create({
            name: 'Button8',
            caption: 'Test',
            icon: 'm-plus',
            type: 'normal',
            theme: 'dark'
          }),
          ButtonViewModel.create({
            name: 'Button9',
            caption: 'Test',
            icon: 'm-plus',
            type: 'normal',
            theme: 'green'
          }),
          ButtonViewModel.create({
            name: 'Button10',
            caption: 'Test',
            icon: 'm-plus',
            type: 'normal',
            theme: 'red'
          }),
          ButtonViewModel.create({
            name: 'Button11',
            caption: 'Test',
            icon: 'm-plus',
            type: 'normal',
            theme: 'yellow'
          })
        ];

        return {
          buttons
        };
      },
      view: ({ buttons }) => {
        const buttonsComponents = buttons.map(b => <Button viewModel={b} key={b.name} />);

        return (
          <DemoForm
            customStyles={css => css`
              gap: 10px;
              flex-wrap: wrap;
            `}
          >
            {buttonsComponents}
          </DemoForm>
        );
      },
      code: `
~~~js
button.theme = 'primary';

button.theme = 'secondary';

button.theme = 'toolbar';

button.theme = 'control';

button.theme = 'tab';

button.theme = 'transparent';

button.theme = 'dark';

button.theme = 'green';

button.theme = 'red';

button.theme = 'yellow';
~~~`
    });

    this.addBlock({
      caption: 'Buttons in different states',
      props: async () => {
        const buttons = [
          ButtonViewModel.create({
            name: 'Button1',
            caption: 'Test',
            icon: 'm-plus',
            type: 'normal',
            theme: 'primary'
          }),
          ButtonViewModel.create({
            name: 'Button2',
            caption: 'Test',
            icon: 'm-plus',
            type: 'normal',
            theme: 'primary',
            disabled: true
          }),
          ButtonViewModel.create({
            name: 'Button3',
            caption: 'Test',
            icon: 'm-plus',
            type: 'normal',
            theme: 'primary',
            active: true
          })
        ];

        return {
          buttons
        };
      },
      view: ({ buttons }) => {
        const buttonsComponents = buttons.map(b => <Button viewModel={b} key={b.name} />);

        return (
          <DemoForm
            customStyles={css => css`
              gap: 10px;
            `}
          >
            {buttonsComponents}
          </DemoForm>
        );
      },
      code: `
~~~js
button.disabled = false;

button.disabled = true;

button.active = true;
~~~`
    });

    this.addBlock({
      caption: 'Button with action',
      props: async () => {
        const button = ButtonViewModel.create({
          name: 'Button1',
          caption: 'Click me!',
          type: 'normal',
          theme: 'primary',
          buttonAction: async () => {
            await showMessage('Hello!');
          }
        });

        return {
          button
        };
      },
      view: ({ button }) => {
        return (
          <DemoForm
            customStyles={css => css`
              gap: 10px;
            `}
          >
            <Button viewModel={button} />
          </DemoForm>
        );
      },
      code: `
~~~js
button.buttonAction = async () => {
  await showMessage('Hello!');
};
~~~`
    });

    this.addBlock({
      caption: 'Set button availability and caption through the shadow',
      props: async () => {
        const checkbox = new CheckboxViewModel();
        checkbox.caption = 'Button is available';
        await checkbox.initialize();

        const button = ButtonViewModel.create({
          name: 'Button1',
          type: 'normal',
          theme: 'primary',
          buttonAction: async () => {
            await showMessage('Hello!');
          }
        });

        ShadowPropsHelper.add(button.captionSettings, 'caption', () => {
          return checkbox.checked ? 'Click me!' : 'I am not clickable :(';
        });

        ShadowPropsHelper.add(button, 'disabled', () => {
          return !checkbox.checked;
        });

        return {
          checkbox,
          button
        };
      },
      view: ({ checkbox, button }) => {
        return (
          <DemoForm
            customStyles={css => css`
              gap: 10px;
              align-items: center;
            `}
          >
            <Checkbox viewModel={checkbox} />
            <Button viewModel={button} />
          </DemoForm>
        );
      },
      code: `
~~~js
ShadowPropsHelper.add(button.caption, 'caption', () => {
  return checkbox.checked ? 'Click me!' : 'I am not clickable :(';
});

ShadowPropsHelper.add(button, 'disabled', () => {
  return !checkbox.checked;
});
~~~`
    });

    this.addBlock({
      caption: 'Set dropdown for a button',
      props: async () => {
        const dropdown = new ExampleDropdownViewModel('Hello there');

        const button = ButtonViewModel.createDropdown({
          name: 'Button1',
          caption: 'Open',
          type: 'normal',
          theme: 'primary',
          dropdown: dropdown
        });

        return {
          button
        };
      },
      view: ({ button }) => {
        return (
          <DemoForm
            customStyles={css => css`
              gap: 10px;
            `}
          >
            <Button viewModel={button} />
          </DemoForm>
        );
      },
      code: `
~~~js
const dropdown = new ExampleDropdownViewModel('Hello there');

const button = Button.createDropdown({
  name: 'Button1',
  caption: 'Open',
  type: 'normal',
  theme: 'primary',
  dropdown: dropdown
});
~~~`
    });

    this.addBlock({
      caption: 'Set menu for a button',
      props: async () => {
        const button = ButtonViewModel.createMenu({
          name: 'open',
          caption: 'open',
          type: 'normal',
          theme: 'primary',
          menuItems: () => [
            {
              button: {
                name: 'level1_1',
                caption: 'level1_1'
              },
              children: [
                {
                  button: {
                    name: 'level2_1',
                    caption: 'level2_1'
                  },
                  children: [
                    'separator',
                    {
                      button: {
                        name: 'level3_1',
                        caption: 'level3_1',
                        buttonAction: () => console.log('level3_1')
                      }
                    },
                    'separator'
                  ]
                },
                {
                  button: {
                    name: 'level2_2',
                    caption: 'level2_2',
                    icon: 'm-eye',
                    buttonAction: () => console.log('level2_2')
                  }
                }
              ]
            },
            {
              button: {
                name: 'level1_2',
                caption: 'level1_2',
                buttonAction: () => console.log('level1_2')
              }
            },
            'separator',
            'separator',
            {
              button: ButtonViewModel.create({
                name: 'level1_3',
                caption: 'level1_3',
                buttonAction: () => console.log('level1_3')
              })
            }
          ]
        });

        ButtonViewModel.modifyMenu(button, ({ container }) => {
          container.insertBefore('level1_1', {
            button: {
              name: 'level1_0',
              caption: 'level1_0',
              buttonAction: () => console.log('level1_0')
            }
          });
        });

        return {
          button
        };
      },
      view: ({ button }) => {
        return (
          <DemoForm
            customStyles={css => css`
              gap: 10px;
            `}
          >
            <Button viewModel={button} />
          </DemoForm>
        );
      },
      code: `
~~~js
const button = Button.createMenu({
  name: 'open',
  caption: 'open',
  type: 'normal',
  theme: 'primary',
  menuItems: [
    {
      button: {
        name: 'level1_1',
        caption: 'level1_1'
      },
      children: [
        {
          button: {
            name: 'level2_1',
            caption: 'level2_1'
          },
          children: [
            {
              button: {
                name: 'level3_1',
                caption: 'level3_1'
              }
            }
          ]
        },
        {
          button: {
            name: 'level2_2',
            caption: 'level2_2',
            icon: 'm-eye'
          }
        }
      ]
    },
    {
      button: {
        name: 'level1_2',
        caption: 'level1_2'
      }
    },
    'separator',
    {
      button: {
        name: 'level1_3',
        caption: 'level1_3'
      }
    }
  ]
});
~~~`
    });
  }
}

export class ExampleDropdownViewModel extends DropdownViewModel {
  constructor(text: string) {
    super();

    this.text = text;
    this.displaySettings.openDirection = 'right';
    this.displaySettings.openPosition = 'left';
    this.displaySettings.autoUp = true;
  }

  readonly text: string;
}

export const ExampleDropdownComponent: FC<{ viewModel: ExampleDropdownViewModel }> = ({
  viewModel
}) => {
  return <span style={{ padding: '10px' }}>{viewModel.text}</span>;
};
