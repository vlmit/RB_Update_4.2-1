import { injectable } from '@tessa/application';
import { TwoFactorAuthViewModel } from 'tessa/ui/login/component/twoFactor/twoFactorAuthViewModel';

/** Модель представления компонента для двухфакторной аутентификации на основе электронной почты. */
@injectable()
export class TwoFactorAuthEmailViewModel extends TwoFactorAuthViewModel {
  public async initialize(): Promise<void> {
    await super.initialize();

    this.fields.push(this.codeField);
    this.buttons.push(this.checkButton, this.retryButton, this.changeButton, this.cancelButton);
  }
}
