import { injectable } from '@tessa/application';
import { MentionsModuleToken } from 'ui/richTextBox/modules/tokens';
import { RichTextBox, RichTextBoxViewModel } from 'ui/richTextBox';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';
import { DemoForm } from 'tessa/ui/playground/chunk';
import { ContentGeneratorModuleToken } from './contentGenerator/contentGeneratorModuleToken';
import { GeneratorType, GeneratorTypeKey } from './contentGenerator/contentGeneratorTypes';
import { LegacyRichAttachmentContainer } from './legacy/legacyRichAttachmentContainer';
import { RichTextBoxArticleBase } from './richTextBoxArticleBase';

@injectable()
export class RichTextBoxLegacyRichArticle extends RichTextBoxArticleBase {
  //#region props

  private readonly _attachmentsContainer = new LegacyRichAttachmentContainer();

  //#endregion

  //#region base overrides

  override getSettings(): PlaygroundArticleSettings {
    return {
      name: 'Controls/RichTextBox/Legacy'
    };
  }

  override async initialize(): Promise<void> {
    this.addBlock({
      caption: 'Legacy',
      props: async () => {
        const richTextBox = new RichTextBoxViewModel(
          this.getDefaultRichTextBoxParams(GeneratorType.LegacyRichContent, {
            attachmentsContainer: this._attachmentsContainer
          })
        );
        richTextBox.modulesContainer
          .withDefaultModules()
          .add(MentionsModuleToken)
          .add(ContentGeneratorModuleToken);
        richTextBox.info = {
          [GeneratorTypeKey]: GeneratorType.LegacyRichContent
        };
        await richTextBox.initialize();

        return {
          richTextBox
        };
      },
      view: ({ richTextBox }) => (
        <DemoForm
          customStyles={css => css`
            flex-direction: column;
            gap: 5px;
          `}
        >
          <RichTextBox viewModel={richTextBox} />
        </DemoForm>
      )
    });
  }

  //#endregion
}
