import { localize } from 'tessa/localization';
import { MySettingsExtension } from 'tessa/ui/cards/mySettings/mySettingsExtension';
import { IMySettingsExtensionContext } from 'tessa/ui/cards/mySettings/mySettingsExtensionContext';
import { WorkspaceStorage } from 'tessa/workspaceStorage';
import { extension } from '@tessa/application';
import platform, { PlatformMode } from 'common/platform';
import { Themes } from 'tessa/ui/themes/themesHandler';
import { IUserSettings$ } from 'tessa/userSettings/userSettingsInjects';
import { IUserSettings } from 'tessa/userSettings/userSettingsTypes';
import { ThemeManager } from 'tessa/ui/themes/themeManager';

/**
 * Расширение для изменения режима отображения в открытых карточках,
 * при сохранении моих настроек.
 */
@extension({ name: 'MySettingsCompactModeExtension' })
export class MySettingsCompactModeExtension extends MySettingsExtension {
  constructor(@IUserSettings$() private readonly _userSettings: IUserSettings) {
    super();
  }

  override async saved(context: IMySettingsExtensionContext): Promise<void> {
    if (!context.validationResult.isSuccessful) {
      return;
    }

    const userSettingsVirtual = context.storeRequest?.card.sections.tryGet('UserSettingsVirtual');
    if (!userSettingsVirtual) {
      return;
    }

    // настройка компактного дизайна всего приложения
    const compactMode = userSettingsVirtual.fields.tryGetBoolean('CompactMode');
    if (compactMode !== null) {
      this._userSettings.compactMode = compactMode;
      platform.mode = compactMode ? PlatformMode.condensed : PlatformMode.normal;
      Themes.buildSettings(ThemeManager.instance.currentThemeName);
    }

    // пытаемся найти настройку "Компактный режим карточки"
    const cardCompactMode = userSettingsVirtual.fields.tryGetBoolean('CardCompactMode');
    if (cardCompactMode !== null) {
      this._userSettings.cardCompactMode = cardCompactMode;

      WorkspaceStorage.instance.cards.forEach(card => {
        const localizeCaptionButton = () =>
          localize(
            `$UI_Files_PreviewSwitchPreviewToMainTabCaption_With${
              card.editor.cardMode === 'full' ? 'Compact' : 'Full'
            }Mode`
          );

        card.editor.cardMode = cardCompactMode ? 'compact' : 'full';

        const previewArea = card.editor.cardModel?.tryGetPreviewArea();
        if (!previewArea) {
          return;
        }

        const buttonHideCardMode = previewArea.buttons.find(b => b.name === 'hideWithCardMode');
        if (buttonHideCardMode) {
          buttonHideCardMode.caption = localizeCaptionButton();
        }

        const buttonSwitchCardMode = previewArea.buttons.find(b => b.name === 'switchWithCardMode');
        if (buttonSwitchCardMode) {
          buttonSwitchCardMode.caption = localizeCaptionButton();
        }
      });
    }
  }
}
