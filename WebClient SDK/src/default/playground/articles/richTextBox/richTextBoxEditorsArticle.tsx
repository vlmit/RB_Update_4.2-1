import { injectable } from '@tessa/application';
import {
  AttachmentsModuleToken,
  BlocksModuleToken,
  CodeModuleToken,
  HeadersModuleToken,
  ImagesModuleToken,
  InlineBlocksModuleToken,
  LinksModuleToken,
  MentionsModuleToken,
  CharCounterModuleToken,
  StylesModuleToken,
  TablesModuleToken,
  WordlikeListsModuleToken
} from 'ui/richTextBox/modules/tokens';
import { RichTextBox, RichTextBoxViewModel } from 'ui/richTextBox';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';
import { DemoForm } from 'tessa/ui/playground/chunk';
import { ContentGeneratorModuleToken } from './contentGenerator/contentGeneratorModuleToken';
import { GeneratorType, GeneratorTypeKey } from './contentGenerator/contentGeneratorTypes';
import { RichTextBoxArticleBase } from './richTextBoxArticleBase';

@injectable()
export class RichTextBoxEditorsArticle extends RichTextBoxArticleBase {
  //#region base overrides

  override getSettings(): PlaygroundArticleSettings {
    return {
      name: 'Controls/RichTextBox/Editors',
      description: 'Rich text box lets users enter and edit text.'
    };
  }

  override async initialize(): Promise<void> {
    this.addBlock({
      caption: 'Default modules',
      props: async () => {
        const richTextBox = new RichTextBoxViewModel(
          this.getDefaultRichTextBoxParams(GeneratorType.All)
        );
        richTextBox.modulesContainer.withDefaultModules().add(ContentGeneratorModuleToken);
        richTextBox.info = {
          [GeneratorTypeKey]: GeneratorType.All
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
richTextBox.modulesContainer.withDefaultModules().add(ContentGeneratorModuleToken);

await richTextBox.initialize();

...

<RichTextBox viewModel={richTextBox} />
~~~`
    });

    this.addBlock({
      caption: 'Only text',
      props: async () => {
        const richTextBox = new RichTextBoxViewModel(
          this.getDefaultRichTextBoxParams(GeneratorType.OnlyText)
        );
        richTextBox.modulesContainer.add(ContentGeneratorModuleToken);
        richTextBox.info = {
          [GeneratorTypeKey]: GeneratorType.OnlyText
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
  .add(ContentGeneratorModuleToken);

await richTextBox.initialize();

...

<RichTextBox viewModel={richTextBox} />
~~~`
    });

    this.addBlock({
      caption: 'Code blocks',
      props: async () => {
        const richTextBox = new RichTextBoxViewModel(
          this.getDefaultRichTextBoxParams(GeneratorType.Code)
        );
        richTextBox.modulesContainer.add(ContentGeneratorModuleToken).add(CodeModuleToken);
        richTextBox.info = {
          [GeneratorTypeKey]: GeneratorType.Code
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
  .add(ContentGeneratorModuleToken)
  .add(CodeModuleToken);

await richTextBox.initialize();

...

<RichTextBox viewModel={richTextBox} />
~~~`
    });

    this.addBlock({
      caption: 'Styles and mentions',
      props: async () => {
        const richTextBox = new RichTextBoxViewModel(
          this.getDefaultRichTextBoxParams(GeneratorType.StylesAndMentions)
        );
        richTextBox.modulesContainer
          .add(ContentGeneratorModuleToken)
          .add(StylesModuleToken)
          .add(MentionsModuleToken);
        richTextBox.info = {
          [GeneratorTypeKey]: GeneratorType.StylesAndMentions
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
  .add(ContentGeneratorModuleToken)
  .add(StylesModuleToken)
  .add(MentionsModuleToken);

await richTextBox.initialize();

...

<RichTextBox viewModel={richTextBox} />
~~~`
    });

    this.addBlock({
      caption: 'Styles and headers',
      props: async () => {
        const richTextBox = new RichTextBoxViewModel(
          this.getDefaultRichTextBoxParams(GeneratorType.StylesAndHeaders)
        );
        richTextBox.modulesContainer
          .add(ContentGeneratorModuleToken)
          .add(StylesModuleToken)
          .add(HeadersModuleToken);
        richTextBox.info = {
          [GeneratorTypeKey]: GeneratorType.StylesAndHeaders
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
  .add(ContentGeneratorModuleToken)
  .add(StylesModuleToken)
  .add(HeadersModuleToken);

await richTextBox.initialize();

...

<RichTextBox viewModel={richTextBox} />
~~~`
    });

    this.addBlock({
      caption: 'Styles and char counter',
      props: async () => {
        const richTextBox = new RichTextBoxViewModel(
          this.getDefaultRichTextBoxParams(GeneratorType.Styles)
        );
        richTextBox.modulesContainer
          .add(ContentGeneratorModuleToken)
          .add(StylesModuleToken)
          .add(CharCounterModuleToken);
        richTextBox.info = {
          [GeneratorTypeKey]: GeneratorType.Styles
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
  .add(ContentGeneratorModuleToken)
  .add(StylesModuleToken)
  .add(RichCharCounterModuleToken);

await richTextBox.initialize();

...

<RichTextBox viewModel={richTextBox} />
~~~`
    });

    this.addBlock({
      caption: 'Blocks',
      props: async () => {
        const richTextBox = new RichTextBoxViewModel(
          this.getDefaultRichTextBoxParams(GeneratorType.Blocks)
        );
        richTextBox.modulesContainer.add(ContentGeneratorModuleToken).add(BlocksModuleToken);
        richTextBox.info = {
          [GeneratorTypeKey]: GeneratorType.Blocks
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
  .add(ContentGeneratorModuleToken)
  .add(BlocksModuleToken);

await richTextBox.initialize();

...

<RichTextBox viewModel={richTextBox} />
~~~`
    });

    this.addBlock({
      caption: 'Styles and blocks',
      props: async () => {
        const richTextBox = new RichTextBoxViewModel(
          this.getDefaultRichTextBoxParams(GeneratorType.BlocksWithStyles)
        );
        richTextBox.modulesContainer
          .add(ContentGeneratorModuleToken)
          .add(StylesModuleToken)
          .add(BlocksModuleToken);
        richTextBox.info = {
          [GeneratorTypeKey]: GeneratorType.BlocksWithStyles
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
  .add(ContentGeneratorModuleToken)
  .add(StylesModuleToken)
  .add(BlocksModuleToken);

await richTextBox.initialize();

...

<RichTextBox viewModel={richTextBox} />
~~~`
    });

    this.addBlock({
      caption: 'Blocks and inline blocks',
      props: async () => {
        const richTextBox = new RichTextBoxViewModel(
          this.getDefaultRichTextBoxParams(GeneratorType.InlineBlocks)
        );
        richTextBox.modulesContainer
          .add(ContentGeneratorModuleToken)
          .add(BlocksModuleToken)
          .add(InlineBlocksModuleToken);
        richTextBox.info = {
          [GeneratorTypeKey]: GeneratorType.InlineBlocks
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
  .add(ContentGeneratorModuleToken)
  .add(BlocksModuleToken)
  .add(InlineBlocksModuleToken);

await richTextBox.initialize();

...

<RichTextBox viewModel={richTextBox} />
~~~`
    });

    this.addBlock({
      caption: 'Lists and styles',
      props: async () => {
        const richTextBox = new RichTextBoxViewModel(
          this.getDefaultRichTextBoxParams(GeneratorType.ListsWithStyles)
        );
        richTextBox.modulesContainer
          .add(ContentGeneratorModuleToken)
          .add(StylesModuleToken)
          .add(WordlikeListsModuleToken);
        richTextBox.info = {
          [GeneratorTypeKey]: GeneratorType.ListsWithStyles
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
  .add(ContentGeneratorModuleToken)
  .add(StylesModuleToken)
  .add(WordlikeListsModuleToken);

await richTextBox.initialize();

...

<RichTextBox viewModel={richTextBox} />
~~~`
    });

    this.addBlock({
      caption: 'Blocks, lists, styles and char counter',
      props: async () => {
        const richTextBox = new RichTextBoxViewModel(
          this.getDefaultRichTextBoxParams(GeneratorType.BlocksAndListsWithStyles)
        );
        richTextBox.modulesContainer
          .add(ContentGeneratorModuleToken)
          .add(StylesModuleToken)
          .add(InlineBlocksModuleToken)
          .add(BlocksModuleToken)
          .add(CharCounterModuleToken)
          .add(WordlikeListsModuleToken);
        richTextBox.info = {
          [GeneratorTypeKey]: GeneratorType.BlocksAndListsWithStyles
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
  .add(ContentGeneratorModuleToken)
  .add(StylesModuleToken)
  .add(InlineBlocksModuleToken)
  .add(BlocksModuleToken)
  .add(CharCounterModuleToken)
  .add(WordlikeListsModuleToken);

await richTextBox.initialize();

...

<RichTextBox viewModel={richTextBox} />
~~~`
    });

    this.addBlock({
      caption: 'Attachments',
      props: async () => {
        const richTextBox = new RichTextBoxViewModel(
          this.getDefaultRichTextBoxParams(GeneratorType.Attachments)
        );
        richTextBox.modulesContainer.add(ContentGeneratorModuleToken).add(AttachmentsModuleToken);
        richTextBox.info = {
          [GeneratorTypeKey]: GeneratorType.Attachments
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
  .add(ContentGeneratorModuleToken)
  .add(AttachmentsModuleToken);

await richTextBox.initialize();

...

<RichTextBox viewModel={richTextBox} />
~~~`
    });

    this.addBlock({
      caption: 'Links and attachments',
      props: async () => {
        const richTextBox = new RichTextBoxViewModel(
          this.getDefaultRichTextBoxParams(GeneratorType.LinksAndAttachments)
        );
        richTextBox.modulesContainer
          .add(ContentGeneratorModuleToken)
          .add(AttachmentsModuleToken)
          .add(LinksModuleToken);
        richTextBox.info = {
          [GeneratorTypeKey]: GeneratorType.LinksAndAttachments
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
  .add(ContentGeneratorModuleToken)
  .add(AttachmentsModuleToken)
  .add(LinksModuleToken);

await richTextBox.initialize();

...

<RichTextBox viewModel={richTextBox} />
~~~`
    });

    this.addBlock({
      caption: 'Attachments and images',
      props: async () => {
        const richTextBox = new RichTextBoxViewModel(
          this.getDefaultRichTextBoxParams(GeneratorType.AttachmentsAndImages)
        );
        richTextBox.modulesContainer
          .add(ContentGeneratorModuleToken)
          .add(AttachmentsModuleToken)
          .add(ImagesModuleToken);
        richTextBox.info = {
          [GeneratorTypeKey]: GeneratorType.AttachmentsAndImages
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
  .add(ContentGeneratorModuleToken)
  .add(AttachmentsModuleToken)
  .add(ImagesModuleToken);

await richTextBox.initialize();

...

<RichTextBox viewModel={richTextBox} />
~~~`
    });

    this.addBlock({
      caption: 'Tables',
      props: async () => {
        const richTextBox = new RichTextBoxViewModel(
          this.getDefaultRichTextBoxParams(GeneratorType.Tables)
        );
        richTextBox.modulesContainer
          .add(ContentGeneratorModuleToken)
          .add(StylesModuleToken)
          .add(TablesModuleToken);
        richTextBox.info = {
          [GeneratorTypeKey]: GeneratorType.Tables
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
  .add(ContentGeneratorModuleToken)
  .add(StylesModuleToken)
  .add(TablesModuleToken);

await richTextBox.initialize();

...

<RichTextBox viewModel={richTextBox} />
~~~`
    });
  }

  //#endregion
}
