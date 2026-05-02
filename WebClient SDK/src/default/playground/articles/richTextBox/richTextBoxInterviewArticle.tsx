import { injectable } from '@tessa/application';
import { ControlsModuleToken } from 'ui/richTextBox/modules/tokens';
import { RichTextBox, RichTextBoxViewModel } from 'ui/richTextBox';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';
import { DemoForm } from 'tessa/ui/playground/chunk';
import { ContentGeneratorModuleToken } from './contentGenerator/contentGeneratorModuleToken';
import { GeneratorType, GeneratorTypeKey } from './contentGenerator/contentGeneratorTypes';
import { RichTextBoxArticleBase } from './richTextBoxArticleBase';

@injectable()
export class RichTextBoxInterviewArticle extends RichTextBoxArticleBase {
  //#region base overrides

  override getSettings(): PlaygroundArticleSettings {
    return {
      name: 'Controls/RichTextBox/Interview',
      description: 'Rich text box example for interview.'
    };
  }

  override async initialize(): Promise<void> {
    this.addBlock({
      caption: 'Interview',
      props: async () => {
        const richTextBox = new RichTextBoxViewModel(
          this.getDefaultRichTextBoxParams(GeneratorType.Interview)
        );
        richTextBox.spellCheck = true;
        richTextBox.modulesContainer
          .withDefaultModules()
          .add(ControlsModuleToken)
          .add(ContentGeneratorModuleToken);
        richTextBox.info = {
          [GeneratorTypeKey]: GeneratorType.Interview
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
      )
    });
  }

  //#endregion
}
