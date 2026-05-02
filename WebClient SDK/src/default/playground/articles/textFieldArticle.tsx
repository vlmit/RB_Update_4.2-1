import Decimal from 'decimal.js';
import { inject, injectable } from '@tessa/application';
import { IPasswordSettingsManager, IPasswordSettingsManager$ } from '@tessa/platform';
import { UIButton } from 'tessa/ui/uiButton';
import { showMessage } from 'tessa/ui/tessaDialog/show';
import { TextFieldView } from 'ui/textField/textFieldView';
import { TextFieldViewModel } from 'ui/textField/textFieldViewModel';
import { FloatFieldViewModel } from 'ui/textField/floatFieldViewModel';
import { NumberFieldViewModel } from 'ui/textField/numberFieldViewModel';
import { IntegerFieldViewModel } from 'ui/textField/integerFieldViewModel';
import { DecimalFieldViewModel } from 'ui/textField/decimalFieldViewModel';
import { PasswordFieldViewModel } from 'ui/textField/passwordFieldViewModel';
import { InputToolbarComplexAffix, InputToolbarAffixFactory } from 'ui/textField/inputToolbarAffix';
import { PlaygroundArticle, DemoForm } from 'tessa/ui/playground/chunk';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';

@injectable()
export class TextFieldArticle extends PlaygroundArticle {
  constructor(
    @inject(IPasswordSettingsManager$)
    private readonly _passwordSettingsManager: IPasswordSettingsManager
  ) {
    super();
  }

  override getSettings(): PlaygroundArticleSettings {
    return {
      name: 'Controls/Text Field',
      description: 'Text Fields let users enter and edit text.'
    };
  }

  override async initialize(): Promise<void> {
    this.addBlock({
      caption: 'Basic',
      props: async () => {
        const textField1 = new TextFieldViewModel();
        await textField1.initialize();
        textField1.spellCheck = true;
        textField1.placeholder = 'Enter correct text ...';

        const textField2 = new TextFieldViewModel();
        await textField2.initialize();
        textField2.minLength = 2;
        textField2.maxLength = 32;
        textField2.text = 'Length min/max restriction';
        textField2.tooltip.text = 'Available text length is 2-32';

        const textField3 = new TextFieldViewModel();
        await textField3.initialize();
        textField3.text = 'Default padding with font style';
        textField3.themeOptions.padding = 'default';

        return {
          textField1,
          textField2,
          textField3
        };
      },
      view: ({ textField1, textField2, textField3 }) => (
        <DemoForm customStyles={css => css({ flexDirection: 'column', gap: '5px' })}>
          <TextFieldView viewModel={textField1} />
          <TextFieldView viewModel={textField2} />
          <TextFieldView viewModel={textField3} />
        </DemoForm>
      )
    });

    this.addBlock({
      caption: 'Availability',
      props: async () => {
        const textFieldButton = this.createSmileButton();

        const textField1 = new TextFieldViewModel();
        await textField1.initialize();
        textField1.availability = 'disabled';
        textField1.text = 'Disabled with default buttons';
        textField1.toolbar.buttons.add(textFieldButton);
        textField1.toolbar.buttons.availability = 'default';

        const textField2 = new TextFieldViewModel();
        await textField2.initialize();
        textField2.availability = 'readonly';
        textField2.text = 'Readonly with enabled buttons';
        textField2.toolbar.buttons.add(textFieldButton);
        textField2.toolbar.buttons.availability = 'enabled';

        const textField3 = new TextFieldViewModel();
        await textField3.initialize();
        textField3.availability = 'enabled';
        textField3.text = 'Enabled with default buttons';
        textField3.toolbar.buttons.add(textFieldButton);
        textField3.toolbar.buttons.availability = 'default';

        const textField4 = new TextFieldViewModel();
        await textField4.initialize();
        textField4.availability = 'enabled';
        textField4.text = 'Enabled with disabled buttons';
        textField4.toolbar.buttons.add(textFieldButton);
        textField4.toolbar.buttons.availability = 'disabled';

        return {
          textField1,
          textField2,
          textField3,
          textField4
        };
      },
      view: ({ textField1, textField2, textField3, textField4 }) => (
        <DemoForm customStyles={css => css({ flexDirection: 'column', gap: '5px' })}>
          <TextFieldView viewModel={textField1} />
          <TextFieldView viewModel={textField2} />
          <TextFieldView viewModel={textField3} />
          <TextFieldView viewModel={textField4} />
        </DemoForm>
      )
    });

    this.addBlock({
      caption: 'Decoration',
      props: async () => {
        const textField1 = new TextFieldViewModel();
        await textField1.initialize();
        textField1.placeholder = 'Field with prefix';
        textField1.toolbar.prefixes.add(InputToolbarAffixFactory.create('HEX'));

        const textField2 = new TextFieldViewModel();
        await textField2.initialize();
        textField2.placeholder = 'Field with suffix';
        textField2.toolbar.suffixes.add(InputToolbarAffixFactory.create('px'));

        const textField3 = new TextFieldViewModel();
        await textField3.initialize();
        textField3.placeholder = 'Field with prefixes';
        textField3.toolbar.prefixes.add(
          new InputToolbarComplexAffix([
            InputToolbarAffixFactory.create('HEX'),
            InputToolbarAffixFactory.create('RGB')
          ])
        );

        const textField4 = new TextFieldViewModel();
        await textField4.initialize();
        textField4.placeholder = 'Field with suffixes';
        textField4.toolbar.suffixes.add(
          new InputToolbarComplexAffix([
            InputToolbarAffixFactory.create('px'),
            InputToolbarAffixFactory.create('em')
          ])
        );

        const textField5 = new TextFieldViewModel();
        await textField5.initialize();
        textField5.placeholder = 'Field with prefix and suffix';
        textField5.toolbar.prefixes.add(InputToolbarAffixFactory.create('m-eye', true));
        textField5.toolbar.suffixes.add(InputToolbarAffixFactory.create('m-eye', true));

        const textField6 = new TextFieldViewModel();
        await textField6.initialize();
        textField6.placeholder = 'Field with prefix, suffix and button';
        textField6.toolbar.prefixes.add(InputToolbarAffixFactory.create('HEX'));
        textField6.toolbar.suffixes.add(InputToolbarAffixFactory.create('px'));
        textField6.toolbar.buttons.add(this.createSmileButton());

        return {
          textField1,
          textField2,
          textField3,
          textField4,
          textField5,
          textField6
        };
      },
      view: ({ textField1, textField2, textField3, textField4, textField5, textField6 }) => (
        <DemoForm customStyles={css => css({ flexDirection: 'column', gap: '5px' })}>
          <TextFieldView viewModel={textField1} />
          <TextFieldView viewModel={textField2} />
          <TextFieldView viewModel={textField3} />
          <TextFieldView viewModel={textField4} />
          <TextFieldView viewModel={textField5} />
          <TextFieldView viewModel={textField6} />
        </DemoForm>
      )
    });

    this.addBlock({
      caption: 'Multiline',
      props: async () => {
        const textField1 = new TextFieldViewModel();
        await textField1.initialize();
        textField1.minRows = 3;
        textField1.maxRows = 10;

        return { textField1 };
      },
      view: ({ textField1 }) => (
        <DemoForm customStyles={css => css({ flexDirection: 'column', gap: '5px' })}>
          <TextFieldView viewModel={textField1} />
        </DemoForm>
      )
    });

    this.addBlock({
      caption: 'Password',
      props: async () => {
        const passwordField1 = new PasswordFieldViewModel(this._passwordSettingsManager);
        await passwordField1.initialize();
        passwordField1.canShowPassword = true;

        return { passwordField1 };
      },
      view: ({ passwordField1 }) => (
        <DemoForm customStyles={css => css({ flexDirection: 'column', gap: '5px' })}>
          <TextFieldView viewModel={passwordField1} />
        </DemoForm>
      )
    });

    this.addBlock({
      caption: 'Integer',
      props: async () => {
        const input1 = new IntegerFieldViewModel();
        await input1.initialize();
        input1.value = 100500;

        const input2 = new IntegerFieldViewModel();
        await input2.initialize();
        input2.minValue = 10;
        input2.maxValue = 10000;
        input2.separateGroups = true;
        input2.syncCommittedChanges = true;

        const input3 = new IntegerFieldViewModel();
        await input3.initialize();
        input3.notifyMode = 'blur';
        input3.validationContainer.isManual = true;
        input3.toolbar.buttons.add(this.createValidateButton(input3));

        return {
          input1,
          input2,
          input3
        };
      },
      view: ({ input1, input2, input3 }) => (
        <DemoForm customStyles={css => css({ flexDirection: 'column', gap: '5px' })}>
          <TextFieldView viewModel={input1} />
          <TextFieldView viewModel={input2} />
          <TextFieldView viewModel={input3} />
        </DemoForm>
      )
    });

    this.addBlock({
      caption: 'Float',
      props: async () => {
        const input1 = new FloatFieldViewModel();
        await input1.initialize();
        input1.value = 100500.5045;

        const input2 = new FloatFieldViewModel();
        await input2.initialize();
        input2.minValue = 10.5;
        input2.maxValue = 10000.76;
        input2.separateGroups = true;
        input2.syncCommittedChanges = true;

        const input3 = new FloatFieldViewModel();
        await input3.initialize();
        input3.notifyMode = 'blur';
        input3.validationContainer.isManual = true;
        input3.toolbar.buttons.add(this.createValidateButton(input3));

        return {
          input1,
          input2,
          input3
        };
      },
      view: ({ input1, input2, input3 }) => (
        <DemoForm customStyles={css => css({ flexDirection: 'column', gap: '5px' })}>
          <TextFieldView viewModel={input1} />
          <TextFieldView viewModel={input2} />
          <TextFieldView viewModel={input3} />
        </DemoForm>
      )
    });

    this.addBlock({
      caption: 'Decimal',
      props: async () => {
        const input1 = new DecimalFieldViewModel();
        await input1.initialize();
        input1.value = '550500.42';

        const input2 = new DecimalFieldViewModel();
        await input2.initialize();
        input2.minValue = 10.5;
        input2.maxValue = 10000.76;
        input2.separateGroups = true;
        input2.groupSeparator = ' ';
        input2.decimalSeparator = ',';
        input2.digitsAfterSeparator = 3;
        input2.syncCommittedChanges = true;

        const input3 = new DecimalFieldViewModel();
        await input3.initialize();
        input3.notifyMode = 'blur';
        input3.validationContainer.isManual = true;
        input3.toolbar.buttons.add(this.createValidateButton(input3));

        return {
          input1,
          input2,
          input3
        };
      },
      view: ({ input1, input2, input3 }) => (
        <DemoForm customStyles={css => css({ flexDirection: 'column', gap: '5px' })}>
          <TextFieldView viewModel={input1} />
          <TextFieldView viewModel={input2} />
          <TextFieldView viewModel={input3} />
        </DemoForm>
      )
    });
  }

  private createValidateButton(field: NumberFieldViewModel): UIButton {
    return UIButton.create({
      type: 'small',
      theme: 'secondary',
      caption: 'Validate',
      buttonAction: () => {
        const { text, validationContainer } = field;
        validationContainer.validate(new Decimal(text), text);
      }
    });
  }

  private createSmileButton(): UIButton {
    return UIButton.create({
      icon: 'm-emoji',
      type: 'small',
      theme: 'secondary',
      buttonAction: () => showMessage('Button click!')
    });
  }
}
