import { injectable } from '@tessa/application';
import { showError } from 'tessa/ui/tessaDialog';
import { RichTextBox, RichTextBoxViewModel } from 'ui/richTextBox';
import { DemoForm } from 'tessa/ui/playground/chunk';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';
import { GeneratorType } from './contentGenerator/contentGeneratorTypes';
import { FakeCardArticleDataSource } from './fakeCard/fakeCardArticleDataSource';
import { FakeCardArticleModuleToken } from './fakeCard/fakeCardArticleModuleToken';
import { FakeCardService } from './fakeCard/fakeCardService';
import { RichTextBoxArticleBase } from './richTextBoxArticleBase';

@injectable()
export class RichTextBoxFakeCardArticle extends RichTextBoxArticleBase {
  //#region base overrides

  override getSettings(): PlaygroundArticleSettings {
    return {
      name: 'Controls/RichTextBox/Fake card',
      description: 'Rich text box with link handler overrides and fake card data source.'
    };
  }

  override async initialize(): Promise<void> {
    const fakeCardService = new FakeCardService();

    this.addBlock({
      caption: 'Fake card',
      description:
        'An example with image and file link management, as well as a fake card data source.',
      props: async () => {
        const card = await fakeCardService.getCard();

        const { dependenciesFactory, extensionContainer, moduleSettingsProvider } =
          this.getDefaultRichTextBoxParams(GeneratorType.None);
        const richTextBox = new RichTextBoxViewModel({
          extensionContainer,
          dependenciesFactory,
          moduleSettingsProvider,
          dataSource: new FakeCardArticleDataSource(card, fakeCardService)
        });
        richTextBox.modulesContainer.withDefaultModules().add(FakeCardArticleModuleToken);
        richTextBox.info = {
          ['CardID']: card.id,
          ['FakeService']: fakeCardService
        };

        await richTextBox.initialize();

        this.disposeList.add(
          richTextBox.linkProcessor.uriOpening.add(async e => {
            if (e.cancel) {
              return;
            }

            if (e.uriString.startsWith('https://test')) {
              e.cancel = true;
              await showError(`A link beginning with 'https://test' cannot be processed`);
            }
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
            flex-direction: column;
          `}
        >
          <RichTextBox viewModel={richTextBox} />
        </DemoForm>
      ),
      code: `
~~~jsx
const card = await fakeCardService.getCard();

const richTextBox = new RichTextBoxViewModel(deps);
richTextBox.modulesContainer.withDefaultModules().add(LinkArticleModuleToken);
richTextBox.info = {
  ['CardID']: card.id,
  ['FakeService']: fakeCardService
};

await richTextBox.initialize();

this.disposeList.add(
  richTextBox.linkProvider.uriOpening.add(async e => {
    if (e.uriString.startsWith('https://test')) {
      e.cancel = true;
      await showError(\`A link beginning with 'https://test' cannot be processed\`);
    }
  })
);

...

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
