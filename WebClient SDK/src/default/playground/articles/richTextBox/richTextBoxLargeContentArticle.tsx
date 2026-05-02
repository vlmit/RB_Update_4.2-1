import { injectable } from '@tessa/application';
import { ControlsModuleToken } from 'ui/richTextBox/modules/tokens';
import { RichTextBox, RichTextBoxViewModel } from 'ui/richTextBox';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';
import { DemoForm } from 'tessa/ui/playground/chunk';
import { ContentGeneratorModuleToken } from './contentGenerator/contentGeneratorModuleToken';
import { GeneratorType, GeneratorTypeKey } from './contentGenerator/contentGeneratorTypes';
import { LargeContentRichAttachmentContainer } from './largeContent/largeContentRichAttachmentContainer';
import { RichTextBoxArticleBase } from './richTextBoxArticleBase';

@injectable()
export class RichTextBoxLargeContentArticle extends RichTextBoxArticleBase {
  //#region props

  private readonly _attachmentsContainer = new LargeContentRichAttachmentContainer();

  //#endregion

  //#region base overrides

  override getSettings(): PlaygroundArticleSettings {
    return {
      name: 'Controls/RichTextBox/Large content',
      description: 'Rich text box with large content and all extensions.'
    };
  }

  override async initialize(): Promise<void> {
    this.addBlock({
      caption: 'Large content',
      description: 'Lots of text with pictures, mentions, blocks, lists and more.',
      props: async () => {
        const richTextBox = new RichTextBoxViewModel(
          this.getDefaultRichTextBoxParams(GeneratorType.LargeContent, {
            attachmentsContainer: this._attachmentsContainer
          })
        );
        richTextBox.spellCheck = true;
        richTextBox.modulesContainer
          .withDefaultModules()
          .add(ContentGeneratorModuleToken)
          .add(ControlsModuleToken);
        richTextBox.info = {
          [GeneratorTypeKey]: GeneratorType.LargeContent
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
richTextBox.spellCheck = true;
richTextBox.modulesContainer
  .withDefaultModules()
  .add(ContentGeneratorModuleToken)
  .add(ControlsModuleToken)
  .add(LargeContentArticleModuleToken)
  .remove(RichCharCounterModuleToken);

await richTextBox.initialize();

...

<RichTextBox viewModel={richTextBox} />
~~~`
    });
  }

  //#endregion
}
