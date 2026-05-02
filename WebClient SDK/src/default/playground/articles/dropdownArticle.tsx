import { useState, useRef, useEffect } from 'react';
import { injectable } from '@tessa/application';
import { PlaygroundArticle, DemoForm } from 'tessa/ui/playground/chunk';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';
import { Dropdown, Select } from 'ui';
import { MenuAction } from 'tessa/ui/menuAction';
import { UIButton, UIButtonComponent } from 'tessa/ui/uiButton';
import { Button } from 'ui/button/button';
import { DropdownItem } from 'ui/dropdown/dropdownItem';

@injectable()
export class DropdownArticle extends PlaygroundArticle {
  override getSettings(): PlaygroundArticleSettings {
    return {
      name: 'Dropdown'
    };
  }

  override async initialize(): Promise<void> {
    this.addBlock({
      caption: 'Multiple popovers with buttons',
      props: async () => {
        const button = UIButton.create({
          name: 'open',
          caption: 'open',
          type: 'small',
          theme: 'control',
          child: [
            UIButton.create({
              name: 'level1_1',
              caption: 'level1_1',
              child: [
                UIButton.create({
                  name: 'level2_1',
                  caption: 'level2_1',
                  child: [
                    UIButton.create({
                      name: 'level3_1',
                      caption: 'level3_1'
                    })
                  ]
                }),
                UIButton.create({
                  name: 'level2_2',
                  caption: 'level2_2'
                })
              ]
            }),
            UIButton.create({
              name: 'level1_2',
              caption: 'level1_2'
            }),
            UIButton.create({
              name: 'level1_3',
              caption: 'level1_3'
            })
          ]
        });

        return {
          button
        };
      },
      view: ({ button }) => (
        <DemoForm>
          <UIButtonComponent viewModel={button} />
        </DemoForm>
      )
    });

    this.addBlock({
      caption: 'Multiple popovers with select',
      props: async () => {
        const menuActions = [
          MenuAction.create({
            name: 'level1_1',
            caption: 'level1_1',
            children: [MenuAction.create({ name: 'level2_1', caption: 'level2_1' })]
          }),
          MenuAction.create({
            name: 'level1_2',
            caption: 'level1_2'
          }),
          MenuAction.create({
            name: 'level1_3',
            caption: 'level1_3'
          })
        ];

        const button = UIButton.create({
          name: 'open',
          caption: 'open',
          type: 'small',
          theme: 'control'
        });

        return {
          button,
          menuActions
        };
      },
      view: ({ button, menuActions }) => (
        <MultiplePopoversView button={button} menuActions={menuActions} />
      )
    });

    this.addBlock({
      caption: 'Popover without rootObject',
      props: async () => {
        const menuActions = [
          MenuAction.create({
            name: 'level1_1',
            caption: 'level1_1'
          }),
          MenuAction.create({
            name: 'level1_2',
            caption: 'level1_2'
          }),
          MenuAction.create({
            name: 'level1_3',
            caption: 'level1_3'
          })
        ];

        return {
          menuActions
        };
      },
      view: ({ menuActions }) => <PopoverWithoutRootView menuActions={menuActions} />
    });
  }
}

const MultiplePopoversView = ({
  button,
  menuActions
}: {
  button: UIButton;
  menuActions: MenuAction[];
}) => {
  const [isOpened, setIsOpened] = useState(false);
  const [value, setValue] = useState<string>(menuActions[0].caption!);
  const btnRef = useRef<HTMLButtonElement>(null);

  useEffect(() => {
    button.onMouseDown = () => setIsOpened(prev => !prev);
    menuActions.forEach(a => {
      if (!a.children.length) {
        a.action = () => setValue(a.name ?? '');
      }
    });
  }, [button, menuActions]);

  return (
    <DemoForm>
      <Button viewModel={button} ref={btnRef} />

      <Dropdown
        className="dropdown"
        isOpened={isOpened}
        rootElement={btnRef.current}
        autoDirection
        openPosition="right"
        openDirection="left"
        closeEvent="mousedown"
        cssTransitionEnabled
        onOutsideClick={() => setIsOpened(false)}
      >
        <Select border="corners" selectType="normal" values={menuActions} value={value} />
      </Dropdown>
    </DemoForm>
  );
};

const PopoverWithoutRootView = ({ menuActions }: { menuActions: MenuAction[] }) => {
  const [showBtn, setShowBtn] = useState(true);
  const [isOpened, setIsOpened] = useState(false);
  const btnRef = useRef<HTMLButtonElement | null>(null);

  const handleBtn = () => {
    setIsOpened(prev => !prev);

    if (!isOpened) {
      setTimeout(() => {
        setShowBtn(false);

        btnRef.current = null;
      }, 2000);
    }
    setTimeout(() => {
      setShowBtn(true);
    }, 4000);
  };

  return (
    <>
      <DemoForm>
        {showBtn && (
          <Button className="button-small button-theme-control" ref={btnRef} onClick={handleBtn}>
            <span>open</span>
          </Button>
        )}
        <Dropdown
          className="dropdown"
          isOpened={isOpened}
          rootElement={btnRef.current}
          // rootElement={showBtn ? btnRef.current : null}
          autoDirection
          openPosition="right"
          openDirection="left"
          closeEvent="mousedown"
          cssTransitionEnabled
          onOutsideClick={() => setIsOpened(false)}
          closeWithoutRoot={true}
        >
          {menuActions.map((ac, i) => (
            <DropdownItem menuAction={ac} key={i} />
          ))}
        </Dropdown>
      </DemoForm>
    </>
  );
};
