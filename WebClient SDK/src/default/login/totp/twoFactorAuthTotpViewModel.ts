import { injectable } from '@tessa/application';
import { TwoFactorAuthViewModel } from 'tessa/ui/login/component/twoFactor/twoFactorAuthViewModel';

/**
 * Модель представления компонента для двухфакторной аутентификации
 * с использованием одноразового пароля на основе времени.
 */
@injectable()
export class TwoFactorAuthTotpViewModel extends TwoFactorAuthViewModel {
  public async initialize(): Promise<void> {
    await super.initialize();

    this.fields.push(this.codeField);
    this.buttons.push(this.checkButton, this.changeButton, this.cancelButton);
  }
}
