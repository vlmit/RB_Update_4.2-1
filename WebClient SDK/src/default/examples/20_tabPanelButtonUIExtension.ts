import { extension } from '@tessa/application';
import { showLoadingOverlay } from 'tessa/ui';
import {
  IAppPanelUIExtensionContext,
  AppPanelUIExtension
} from 'tessa/ui/appPanel/appPanelUIExtension';
import { AppPanelButtonViewModel } from 'tessa/ui/appPanel/appPanelButton/appPanelButtonViewModel';
import { openCard } from 'tessa/ui/uiHost';

/**
 * Добавляем дополнительную кнопку на панель с табами приложения.
 * При нажатии на кнопку открываем виртуальную карточку.
 *
 * Результат работы расширения:
 * Пример данного расширения добавляет дополнительную кнопку на панель с табами, при нажатии на которую
 * открывается виртуальная карточка "Мои замещения".
 */
@extension()
export class CustomTabPanelButtonUIExtension extends AppPanelUIExtension {
  override async initialize(context: IAppPanelUIExtensionContext): Promise<void> {
    // выводится слева (на мобильных устройствах выводится в контекстном меню)
    context.buttons.push(
      AppPanelButtonViewModel.create({
        name: 'RoleDeputiesManagement',
        caption: '$UI_Tiles_RoleDeputiesManagement',
        icon: 'icon-thin-285',
        buttonAction: async () => {
          // открываем виртуальную карточку "Мои замещения" для текущего пользователя
          await showLoadingOverlay(async splashResolve => {
            const editor = await openCard({
              cardTypeId: 'cb931209-2ad9-4370-bb3c-3172e61937ba', // RoleDeputiesManagementTypeID
              splashResolve
            });

            if (editor) {
              const workspaceInfo = '$UI_Tiles_Settings';
              if (editor.workspaceInfo !== workspaceInfo) {
                editor.workspaceInfo = workspaceInfo;
                editor.cardModelInitialized.add(async e => {
                  e.workspaceInfo = workspaceInfo;
                });
              }
            }
          });
        }
      })
    );
  }
}
