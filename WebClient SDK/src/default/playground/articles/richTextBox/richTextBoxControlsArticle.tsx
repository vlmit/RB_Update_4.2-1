import { observable, reaction, runInAction } from 'mobx';
import { observer } from 'mobx-react-lite';
import { injectable } from '@tessa/application';
import {
  CheckBoxNode,
  RichCheckBoxViewModel
} from 'ui/richTextBox/modules/controls/chunk/checkbox';
import {
  ChecklistModuleToken,
  ControlsModuleToken,
  StylesModuleToken
} from 'ui/richTextBox/modules/tokens';
import { RichTextBox, RichTextBoxViewModel } from 'ui/richTextBox';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';
import { DemoForm } from 'tessa/ui/playground/chunk';
import { ContentGeneratorModuleToken } from './contentGenerator/contentGeneratorModuleToken';
import { GeneratorType, GeneratorTypeKey } from './contentGenerator/contentGeneratorTypes';
import { CheckboxStrikethroughVisitor } from './controls/checkboxStrikethroughVisitor';
import { RichTextBoxArticleBase } from './richTextBoxArticleBase';

@injectable()
export class RichTextBoxControlsArticle extends RichTextBoxArticleBase {
  //#region base overrides

  override getSettings(): PlaygroundArticleSettings {
    return {
      name: 'Controls/RichTextBox/Controls',
      description: 'Rich text box with controls.'
    };
  }

  override async initialize(): Promise<void> {
    this.addBlock({
      caption: 'All controls',
      props: async () => {
        const richTextBox = new RichTextBoxViewModel(
          this.getDefaultRichTextBoxParams(GeneratorType.AllControls)
        );
        richTextBox.modulesContainer
          .add(ContentGeneratorModuleToken)
          .add(ControlsModuleToken)
          .add(StylesModuleToken)
          .add(ChecklistModuleToken);
        richTextBox.info = {
          [GeneratorTypeKey]: GeneratorType.AllControls
        };

        await richTextBox.initialize();

        return {
          richTextBox
        };
      },
      view: ({ richTextBox }) => (
        <DemoForm
          customStyles={css => css`
            gap: 10px;
          `}
        >
          <RichTextBox viewModel={richTextBox} />
        </DemoForm>
      ),
      code: `
~~~jsx
const richTextBox = new RichTextBoxViewModel(params);
richTextBox.modulesContainer
  .add(ControlsModuleToken)
  .add(StylesModuleToken)
  .add(ChecklistModuleToken);

await richTextBox.initialize();

...

<RichTextBox viewModel={richTextBox} />
~~~`
    });

    this.addBlock({
      caption: 'Checkboxes with strikethrough',
      description: 'Checkboxes, when you click on which the text behind them is strikethrough.',
      props: async () => {
        const richTextBox = new RichTextBoxViewModel(
          this.getDefaultRichTextBoxParams(GeneratorType.StrikethroughCheckboxes)
        );
        richTextBox.modulesContainer
          .add(ContentGeneratorModuleToken)
          .add(StylesModuleToken)
          .add(ControlsModuleToken);
        richTextBox.info = {
          [GeneratorTypeKey]: GeneratorType.StrikethroughCheckboxes
        };

        await richTextBox.initialize();

        this.disposeList.add(
          richTextBox.nodesManager.onAddNode.add(node => {
            if (!(node instanceof CheckBoxNode)) {
              return;
            }

            this.disposeList.add(
              reaction(
                () => node.checked,
                () => {
                  node.visit(
                    new CheckboxStrikethroughVisitor(
                      node,
                      richTextBox.nodesManager.provider,
                      richTextBox.editor
                    )
                  );
                }
              )
            );
          })
        );

        return {
          richTextBox
        };
      },
      view: ({ richTextBox }) => (
        <DemoForm
          customStyles={css => css`
            gap: 10px;
          `}
        >
          <RichTextBox viewModel={richTextBox} />
        </DemoForm>
      ),
      code: `
~~~jsx
const richTextBox = new RichTextBoxViewModel(params);
richTextBox.modulesContainer
  .add(StylesModuleToken)
  .add(ControlsModuleToken);

await richTextBox.initialize();

richTextBox.nodesManager.onAddNode.add(node => {
  if (!(node instanceof CheckBoxNode)) {
    return;
  }

  reaction(
    () => node.checked,
    () => {
      node.visit(
        new CheckboxTextFinderVisitor(
          node,
          richTextBox.nodesManager.provider,
          richTextBox.editor
        )
      );
    }
  );
});

...

<RichTextBox viewModel={richTextBox} />
~~~`
    });

    this.addBlock({
      caption: 'Checkboxes counter',
      description: 'Counting the total number of checkboxes and checked checkboxes.',
      props: async () => {
        const richTextBox = new RichTextBoxViewModel(
          this.getDefaultRichTextBoxParams(GeneratorType.CheckboxesCounter)
        );
        richTextBox.modulesContainer.add(ContentGeneratorModuleToken).add(ControlsModuleToken);
        richTextBox.info = {
          [GeneratorTypeKey]: GeneratorType.CheckboxesCounter
        };

        await richTextBox.initialize();

        const checkboxCount = observable.box(0);
        const checkedCheckboxCount = observable.box(0);

        this.disposeList.add(
          richTextBox.nodesManager.onAddNode.add(node => {
            const checkBoxViewModel = node.tryGetViewModel() as RichCheckBoxViewModel;
            if (!checkBoxViewModel) {
              return;
            }

            runInAction(() => {
              checkboxCount.set(checkboxCount.get() + 1);
              if (checkBoxViewModel.checked) {
                checkedCheckboxCount.set(checkedCheckboxCount.get() + 1);
              }
            });

            this.disposeList.add(
              checkBoxViewModel.onChange.add(() => {
                const value = checkedCheckboxCount.get();
                runInAction(() =>
                  checkedCheckboxCount.set(checkBoxViewModel.checked ? value + 1 : value - 1)
                );
              })
            );
          }),
          richTextBox.nodesManager.onRemoveNode.add(node => {
            const checkBoxViewModel = node.tryGetViewModel() as RichCheckBoxViewModel;
            if (!checkBoxViewModel) {
              return;
            }

            runInAction(() => {
              checkboxCount.set(checkboxCount.get() - 1);
              if (checkBoxViewModel.checked) {
                checkedCheckboxCount.set(checkedCheckboxCount.get() - 1);
              }
            });
          })
        );

        return {
          richTextBox,
          checkboxCount,
          checkedCheckboxCount
        };
      },
      view: observer(({ richTextBox, checkboxCount, checkedCheckboxCount }) => {
        return (
          <DemoForm
            customStyles={css => css`
              flex-direction: column;
              gap: 10px;
            `}
          >
            <span>
              Checked {checkedCheckboxCount.get()} out of {checkboxCount.get()}
            </span>
            <div>
              <RichTextBox viewModel={richTextBox} />
            </div>
          </DemoForm>
        );
      }),
      code: `
~~~jsx
const richTextBox = new RichTextBoxViewModel(params);
richTextBox.modulesContainer
  .add(ControlsModuleToken);

await richTextBox.initialize();

const checkboxCount = observable.box(0);
const checkedCheckboxCount = observable.box(0);

richTextBox.nodesManager.onAddNode.add(node => {
  const checkBoxViewModel = node.tryGetViewModel() as RichCheckBoxViewModel;
  if (!checkBoxViewModel) {
    return;
  }

  runInAction(() => {
    checkboxCount.set(checkboxCount.get() + 1);
    if (checkBoxViewModel.checked) {
      checkedCheckboxCount.set(checkedCheckboxCount.get() + 1);
    }
  });

    checkBoxViewModel.onChange.add(() => {
      const value = checkedCheckboxCount.get();
      runInAction(() =>
        checkedCheckboxCount.set(checkBoxViewModel.checked ? value + 1 : value - 1)
      );
    });
}),
richTextBox.nodesManager.onRemoveNode.add(node => {
  const checkBoxViewModel = node.tryGetViewModel() as RichCheckBoxViewModel;
  if (!checkBoxViewModel) {
    return;
  }

  runInAction(() => {
    checkboxCount.set(checkboxCount.get() - 1);
    if (checkBoxViewModel.checked) {
      checkedCheckboxCount.set(checkedCheckboxCount.get() - 1);
    }
  });
});

...

<div>
  Checked {checkedCheckboxCount.get()} out of {checkboxCount.get()}
</div>
<RichTextBox viewModel={richTextBox} />
~~~`
    });
  }

  override dispose(): void {
    super.dispose();

    this.disposeList.dispose();
  }

  //#endregion
}
