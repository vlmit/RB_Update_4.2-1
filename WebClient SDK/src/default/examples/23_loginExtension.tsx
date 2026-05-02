import { SyntheticEvent } from 'react';
import { observer } from 'mobx-react-lite';
import { observable, runInAction } from 'mobx';
import { extension, injectable } from '@tessa/application';
import { ComponentsRegistry } from '@tessa/ui';
import { Visibility } from 'tessa/platform/visibility';
import { ValidationResult, ValidationResultType } from 'tessa/platform/validation';
import { showMessage, UIButton } from 'tessa/ui';
import TextField from 'ui/textField/textField';
import ControlContainer from 'ui/controlContainer/controlContainer';
import { ILoginExtensionContext, LoginExtension } from 'tessa/ui/login/loginExtension';
import { FirstFactorAuthViewModel } from 'tessa/ui/login/component/firstFactor/firstFactorAuthViewModel';
import { ILoginComponentProps } from 'tessa/ui/login/component/loginComponentViewModel';
import { LoginFormViewModel } from 'tessa/ui/login/form/loginFormViewModel';
import {
  DefaultLoginForm,
  LoginButtons,
  LoginFields,
  LoginInfo,
  LoginMessage,
  LoginTextFieldProps
} from 'tessa/ui/login/form/loginForm';
import { captchaExampleImg, captchaExampleValue } from './captchaExample';

/**
 * Позволяет добавлять дополнительные параметры и элементы управления в форму логинизации.
 *
 * Результат работы расширения:
 * В форму логинизации добавлен параметр безопасности в виде капчи и тестовая кнопка, при нажатии на которую
 * появляется сообщение в диалоговом окне.
 */
@extension()
export class ExampleLoginExtension extends LoginExtension {
  override initializing(context: ILoginExtensionContext): void {
    // Пример фабрики для создания модели представления формы окна логина.
    context.loginFormViewModelFactory = () => new ExampleLoginFormViewModel();
    // Пример фабрики для создания компонента формы окна логина.
    context.loginFormFactory = vm => <DefaultLoginForm viewModel={vm} />;
    // Пример регистрации компонента окна логина, который находится внутри формы окна логина.
    ComponentsRegistry.instance.register('ExampleLoginComponentViewModel', ExampleLoginComponent);
  }
}

export class ExampleLoginFormViewModel extends LoginFormViewModel {
  constructor(componentName = 'ExampleLoginComponentViewModel') {
    super(componentName);

    // Пример обработчика инициализации компонента для формы окна входа в систему.
    // Обработчик вызывается после создания модели представления компонента и её инициализации.
    this.onComponentInitialized.add(componentViewModel => {
      console.log(componentViewModel.message);
    });
  }

  // Дополнительная логика для модификации кастомной модели представления
}

@injectable()
export class ExampleLoginComponentViewModel extends FirstFactorAuthViewModel {
  //#region fields

  @observable
  private _captchaImage: string;

  @observable
  private _captchaField: LoginTextFieldProps;

  @observable.ref
  private _testButton: UIButton;

  //#endregion

  //#region properties

  public get captchaImage(): string {
    return this._captchaImage;
  }
  public set captchaImage(value: string) {
    runInAction(() => (this._captchaImage = value));
  }

  public get captchaField(): LoginTextFieldProps {
    return this._captchaField;
  }
  protected set captchaField(value: LoginTextFieldProps) {
    runInAction(() => (this._captchaField = value));
  }

  public get testButton(): UIButton {
    return this._testButton;
  }
  protected set testButton(value: UIButton) {
    runInAction(() => (this._testButton = value));
  }

  //#endregion

  //#region public methods

  public async initialize(): Promise<void> {
    await super.initialize();

    this.initializeCaptcha();
  }

  //#endregion

  //#region base overrides

  protected async onLogin(): Promise<void> {
    if (this.captchaLoginCheck()) {
      await super.onLogin();
    }
  }

  protected async onWinLogin(): Promise<void> {
    if (this.captchaLoginCheck()) {
      await super.onWinLogin();
    }
  }

  protected async onSAMLLogin(): Promise<void> {
    if (this.captchaLoginCheck()) {
      await super.onSAMLLogin();
    }
  }

  //#endregion

  //#region protected methods

  protected captchaValidator: (value: string) => boolean;

  protected initializeFields(): void {
    super.initializeFields();

    this.captchaField = {
      name: 'Captcha',
      type: 'text',
      value: '',
      placeholder: 'captcha',
      onChange: e => this.onCaptchaFieldChanged(e),
      visibility: () => Visibility.Visible,
      get disabled() {
        return super.inProgress;
      }
    };
  }

  protected initializeButtons(): void {
    super.initializeButtons();

    this.testButton = UIButton.create({
      name: 'TestButton',
      caption: 'TestButton',
      icon: 'icon-thin-025',
      type: 'normal',
      theme: 'secondary',
      buttonAction: () => showMessage('TestButton click!', 'Test', { OKButtonText: 'OK!' })
    });

    this.buttons.push(this.testButton);
  }

  protected initializeCaptcha(): void {
    this.captchaImage = captchaExampleImg;
    this.captchaValidator = (value: string) => value?.toLowerCase() === captchaExampleValue;
  }

  protected onCaptchaFieldChanged = (e: SyntheticEvent<HTMLInputElement>): void => {
    runInAction(() => (this.captchaField.value = e.currentTarget.value));
  };

  protected captchaLoginCheck(): boolean {
    return runInAction(() => {
      this.message = null;
      if (!this.captchaValidator(this.captchaField.value as string)) {
        this.message = ValidationResult.fromText(
          'Captcha value is wrong.',
          ValidationResultType.Error
        );
        this.captchaField.value = '';
        return false;
      }

      return true;
    });
  }

  //#endregion
}

export const ExampleLoginComponent = observer<ILoginComponentProps<ExampleLoginComponentViewModel>>(
  function ExampleLoginComponent({ viewModel }) {
    const { captchaField } = viewModel;
    const { visibility, ...props } = captchaField;
    const isVisible = visibility() === Visibility.Visible;

    return (
      <>
        <LoginInfo>
          <LoginFields fields={viewModel.fields} />
          {isVisible ? (
            <>
              <div>
                <img src={viewModel.captchaImage} />
              </div>
              <ControlContainer border="permanent">
                <TextField {...props} disabled={captchaField.disabled} />
              </ControlContainer>
            </>
          ) : null}
          <LoginMessage
            message={viewModel.message}
            localize={(alias, defaultValue) => viewModel.localize(alias, defaultValue)}
          />
        </LoginInfo>
        <LoginButtons buttons={viewModel.buttons} />
      </>
    );
  }
);
