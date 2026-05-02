import { injectable } from '@tessa/application';
import { ValidationResult, ValidationResultType } from '@tessa/core';
import { RichEditorHelper, RichStorage, RichTextBox, RichTextBoxViewModel } from 'ui/richTextBox';
import { LinksModuleToken } from 'ui/richTextBox/modules/tokens';
import { TextFieldView } from 'ui/textField/textFieldView';
import { TextFieldViewModel } from 'ui/textField/textFieldViewModel';
import { DemoForm } from 'tessa/ui/playground/chunk';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';
import { GeneratorType } from './contentGenerator/contentGeneratorTypes';
import { RichTextBoxArticleBase } from './richTextBoxArticleBase';

@injectable()
export class RichTextBoxModesArticle extends RichTextBoxArticleBase {
  //#region base overrides

  override getSettings(): PlaygroundArticleSettings {
    return {
      name: 'Controls/RichTextBox/Modes'
    };
  }

  override async initialize(): Promise<void> {
    this.addBlock({
      caption: 'Control with placeholders',
      props: async () => {
        const textField = new TextFieldViewModel();
        await textField.initialize();
        textField.spellCheck = true;
        textField.placeholder = 'Enter correct text ...';

        const richTextBox = new RichTextBoxViewModel(
          this.getDefaultRichTextBoxParams(GeneratorType.None)
        );
        richTextBox.controlMode.simple = true;
        richTextBox.controlMode.maxRows = 1;
        await richTextBox.initialize();

        richTextBox.editMode = 'edit';
        richTextBox.spellCheck = true;
        richTextBox.placeholder = 'Enter correct text ...';

        return {
          textField,
          richTextBox
        };
      },
      view: ({ textField, richTextBox }) => (
        <DemoForm
          customStyles={css => css`
            flex-direction: column;
            gap: 5px;
          `}
        >
          <div>Text field:</div>
          <TextFieldView viewModel={textField} />
          <div>Rich text box:</div>
          <RichTextBox viewModel={richTextBox} />
        </DemoForm>
      )
    });

    this.addBlock({
      caption: 'Availability',
      props: async () => {
        const textField = new TextFieldViewModel();
        await textField.initialize();
        textField.availability = 'readonly';
        textField.text = 'Readonly text';

        const richTextBox = new RichTextBoxViewModel(
          this.getDefaultRichTextBoxParams(GeneratorType.None)
        );
        richTextBox.controlMode.simple = true;
        richTextBox.controlMode.maxRows = 1;
        await richTextBox.initialize();

        richTextBox.readOnly = true;

        const storage = new RichStorage();
        storage.text = RichEditorHelper.createSimpleContent('Readonly text');
        richTextBox.dataSource.setValue(storage);

        return {
          textField,
          richTextBox
        };
      },
      view: ({ textField, richTextBox }) => (
        <DemoForm
          customStyles={css => css`
            flex-direction: column;
            gap: 5px;
          `}
        >
          <div>Text field:</div>
          <TextFieldView viewModel={textField} />
          <div>Rich text box:</div>
          <RichTextBox viewModel={richTextBox} />
        </DemoForm>
      )
    });

    this.addBlock({
      caption: 'Multiline',
      props: async () => {
        const textField = new TextFieldViewModel();
        await textField.initialize();
        textField.minRows = 3;
        textField.maxRows = 10;

        const richTextBox = new RichTextBoxViewModel(
          this.getDefaultRichTextBoxParams(GeneratorType.None)
        );
        richTextBox.controlMode.simple = true;
        richTextBox.controlMode.minRows = 3;
        richTextBox.controlMode.maxRows = 10;

        await richTextBox.initialize();

        richTextBox.editMode = 'edit';

        return {
          textField,
          richTextBox
        };
      },
      view: ({ textField, richTextBox }) => (
        <DemoForm
          customStyles={css => css`
            flex-direction: column;
            gap: 5px;
          `}
        >
          <div>Text field:</div>
          <TextFieldView viewModel={textField} />
          <div>Rich text box:</div>
          <RichTextBox viewModel={richTextBox} />
        </DemoForm>
      )
    });

    this.addBlock({
      caption: 'Validation',
      props: async () => {
        const textField = new TextFieldViewModel();
        textField.validationContainer.add(context => {
          if (!context.value?.length) {
            context.addResult(
              ValidationResult.fromText('$UI_Cards_ErrorText', ValidationResultType.Error)
            );
          } else {
            context.addResult(ValidationResult.empty);
          }
        });
        await textField.initialize();

        const richTextBox = new RichTextBoxViewModel(
          this.getDefaultRichTextBoxParams(GeneratorType.None)
        );
        richTextBox.validationContainer.add(context => {
          const result = RichEditorHelper.getOnlyTextContentFromHtml(context.value.text);
          if (!result.length) {
            context.addResult(
              ValidationResult.fromText('$UI_Cards_ErrorText', ValidationResultType.Error)
            );
          } else {
            context.addResult(ValidationResult.empty);
          }
        });
        richTextBox.validationContainer.isEnabled = true;
        richTextBox.controlMode.simple = true;
        richTextBox.controlMode.maxRows = 1;
        await richTextBox.initialize();

        richTextBox.focusManager.onBlur.add(async () => {
          await richTextBox.commit();
        });

        richTextBox.editMode = 'edit';

        return {
          textField,
          richTextBox
        };
      },
      view: ({ textField, richTextBox }) => (
        <DemoForm
          customStyles={css => css`
            flex-direction: column;
            gap: 5px;
          `}
        >
          <div>Text field:</div>
          <TextFieldView viewModel={textField} />
          <div>Rich text box (triggers when blur):</div>
          <RichTextBox viewModel={richTextBox} />
        </DemoForm>
      )
    });

    this.addBlock({
      caption: 'Links',
      props: async () => {
        const richTextBox = new RichTextBoxViewModel(
          this.getDefaultRichTextBoxParams(GeneratorType.None)
        );
        richTextBox.controlMode.simple = true;
        richTextBox.controlMode.maxRows = 1;
        richTextBox.modulesContainer.add(LinksModuleToken);
        await richTextBox.initialize();

        richTextBox.editMode = 'edit';

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
