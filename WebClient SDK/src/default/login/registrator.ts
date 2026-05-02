import { ExtensionRegistrator, ExtensionStage } from '@tessa/application';
import { CardControlTypeRegistry } from '@tessa/platform';
import { ComponentsRegistry } from '@tessa/ui';
import { CardControlRegistry } from 'tessa/ui/cards/cardControlRegistry';
import { ILoginComponentViewModel$ } from 'tessa/ui/login/component/loginComponentViewModel';
import { ITwoFactorAuthConfigurator$ } from 'tessa/ui/login/component/twoFactor/twoFactorAuthConfigurator';

import { TwoFactorAuthDefaultTypes } from './twoFactorAuthDefaultTypes';
import { TwoFactorAuthTotpViewModel } from './totp/twoFactorAuthTotpViewModel';
import { TwoFactorAuthTotpComponent } from './totp/twoFactorAuthTotpComponent';
import { TwoFactorAuthTotpConfigurator } from './totp/twoFactorAuthTotpConfigurator';
import {
  TwoFactorAuthTotpCode,
  TwoFactorAuthTotpCodeControlType,
  TwoFactorAuthTotpCodeType,
  TwoFactorAuthTotpMetadataExtension
} from './totp/twoFactorAuthTotpCodeExtension';
import { TwoFactorAuthEmailViewModel } from './email/twoFactorAuthEmailViewModel';
import { TwoFactorAuthEmailComponent } from './email/twoFactorAuthEmailComponent';

export const LoginRegistrator: ExtensionRegistrator = {
  async registerTypes(container) {
    // Регистрация моделей представления для компонентов
    container
      .bind(ILoginComponentViewModel$)
      .to(TwoFactorAuthEmailViewModel)
      .inRequestScope()
      .whenTargetNamed(TwoFactorAuthDefaultTypes.Email);
    container
      .bind(ILoginComponentViewModel$)
      .to(TwoFactorAuthTotpViewModel)
      .inRequestScope()
      .whenTargetNamed(TwoFactorAuthDefaultTypes.TOTP);

    // Регистрация компонентов
    ComponentsRegistry.instance.register(
      TwoFactorAuthDefaultTypes.Email,
      TwoFactorAuthEmailComponent
    );
    ComponentsRegistry.instance.register(
      TwoFactorAuthDefaultTypes.TOTP,
      TwoFactorAuthTotpComponent
    );

    // Регистрация контролов карточки
    CardControlTypeRegistry.instance.register(TwoFactorAuthTotpCodeControlType);
    CardControlRegistry.instance.register(
      TwoFactorAuthTotpCodeControlType.id,
      TwoFactorAuthTotpCodeType
    );
    ComponentsRegistry.instance.register(
      TwoFactorAuthTotpCodeControlType.id,
      TwoFactorAuthTotpCode
    );

    // Регистрация конфигураторов
    container
      .bind(ITwoFactorAuthConfigurator$)
      .to(TwoFactorAuthTotpConfigurator)
      .inRequestScope()
      .whenTargetNamed(TwoFactorAuthDefaultTypes.TOTP);
  },
  async registerExtensions(container) {
    container.registerExtension({
      extension: TwoFactorAuthTotpMetadataExtension,
      stage: ExtensionStage.AfterPlatform,
      singleton: true
    });
  }
};
