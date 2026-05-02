import { extension, inject } from '@tessa/application';
import { MenuAction } from 'tessa/ui/menuAction';
import {
  AppPanelUIExtension,
  IAppPanelUIExtensionContext
} from 'tessa/ui/appPanel/appPanelUIExtension';
import { ISettingsPage, ISettingsPage$ } from 'tessa/shared/settingsPage';

// Расширение SettingsAppMenuActionExtension добавляет новый пункт меню в раздел "Мои настройки" мобильного приложения Tessa.
// Этот пункт позволяет пользователю быстро перейти к странице настроек приложения.
// Реализация основана на наследовании AppPanelUIExtension и использовании DI для получения страницы настроек.
// В методе initialize расширение ищет пункт меню "MySettings" и добавляет к нему дочерний пункт с действием открытия настроек.
@extension({ name: 'SettingsAppMenuActionMobileClientExtension' })
export class SettingsAppMenuActionMobileClientExtension extends AppPanelUIExtension {
  constructor(
    @inject(ISettingsPage$, { optional: true }) private readonly _settingsPage: ISettingsPage | null
  ) {
    super();
  }

  // Метод инициализации расширения, вызывается при запуске
  async initialize(context: IAppPanelUIExtensionContext): Promise<void> {
    // Добавление генератора пользовательского меню в массив генераторов
    context.userAccount.itemUserAccountMenuGenerators.push(ctx => {
      // Поиск пункта меню "MySettings" среди доступных действий
      const mySettings = ctx.menuActions.find(action => action.name === 'MySettings');

      // Проверяем, что страница настроек (_settingsPage) доступна (не null)
      // и что найден пункт меню "MySettings".
      // Только в этом случае добавляем новый пункт меню для перехода к настройкам приложения.
      if (this._settingsPage !== null && mySettings) {
        // Создание нового пункта меню для перехода к настройкам приложения
        const searchQueriesMenuAction = MenuAction.create({
          type: 'normal',
          name: 'AppSetting',
          caption: '$About_Mobile_AppSetting',
          icon: 'm-search',
          action: () => this._settingsPage!.open()
        });
        // Добавление нового пункта меню как дочернего к "MySettings"
        mySettings.children.push(searchQueriesMenuAction);
      }
    });
  }
}
