import { extension } from '@tessa/application';
import {
  AppPanelUIExtension,
  IAppPanelUIExtensionContext
} from 'tessa/ui/appPanel/appPanelUIExtension';
import { SessionExpireAlertExtension } from './sessionExpireAlertExtension';

/** Расширение переносит кнопку "Обновить сессию" в конец списка меню пользователя. */
@extension({ name: 'SessionMenuButtonExtension' })
export class SessionMenuButtonExtension extends AppPanelUIExtension {
  async initialize(context: IAppPanelUIExtensionContext): Promise<void> {
    if (!SessionExpireAlertExtension.sessionRefreshGenerator) {
      return;
    }

    const { itemUserAccountMenuGenerators } = context.userAccount;

    const sessionRefreshGeneratorIndex = itemUserAccountMenuGenerators.indexOf(
      SessionExpireAlertExtension.sessionRefreshGenerator
    );

    if (sessionRefreshGeneratorIndex > -1) {
      const [sessionRefreshGenerator] = itemUserAccountMenuGenerators.splice(
        sessionRefreshGeneratorIndex,
        1
      );

      itemUserAccountMenuGenerators.push(sessionRefreshGenerator);
    }
  }
}
