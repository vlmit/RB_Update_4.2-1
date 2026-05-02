import Platform, { PlatformSize } from 'common/platform';
import { runInAction } from 'mobx';
import { localize } from 'tessa/localization';
import { CardUIExtension, ICardUIExtensionContext } from 'tessa/ui/cards';
import { UIButton } from 'tessa/ui/uiButton';
import { extension } from '@tessa/application';
import { Visibility } from 'tessa/platform';
import { DefaultContentPadding, MobileContentPadding } from 'tessa/ui/themes/variables';

/**
 * Расширение добаляет кнопки в меню предпросмотра для
 * изменения режима отображения карточки при скрытии\отображении предпросмотра.
 */
@extension()
export class CardModePreviewButtonsUIExtension extends CardUIExtension {
  contextInitialized(context: ICardUIExtensionContext): void {
    if (!context.model.previewManager.previewArea?.enabled) {
      return;
    }

    const { cardEditor } = context.uiContext;
    if (!cardEditor) {
      return;
    }

    const previewArea = context.model.tryGetPreviewArea();
    if (!previewArea) {
      return;
    }

    const contentPadding = Platform.isMobile() ? MobileContentPadding : DefaultContentPadding;

    runInAction(() => {
      const localizeCaptionButton = () =>
        localize(
          `$UI_Files_PreviewSwitchPreviewToMainTabCaption_With${
            cardEditor.cardMode === 'full' ? 'Compact' : 'Full'
          }Mode`
        );

      const getButtonVisibility = (dialogMode: boolean): Visibility =>
        previewArea.getVisibilityPreviewButton(dialogMode) == Visibility.Visible &&
        !cardEditor.dialogName &&
        Platform.viewportWidth >= PlatformSize.xxl + 2 * contentPadding
          ? Visibility.Visible
          : Visibility.Collapsed;

      previewArea.buttons.push(
        UIButton.create({
          caption: localizeCaptionButton(),
          buttonAction: button => {
            cardEditor.cardMode = cardEditor.cardMode === 'full' ? 'compact' : 'full';

            button.caption = localizeCaptionButton();
            const buttonswitchCardMode = previewArea.buttons.find(
              b => b.name === 'switchWithCardMode'
            );
            if (buttonswitchCardMode) {
              buttonswitchCardMode.caption = localizeCaptionButton();
            }
          },
          visibility: () => getButtonVisibility(false),
          name: 'hideWithCardMode'
        }),

        UIButton.create({
          caption: localizeCaptionButton(),
          buttonAction: button => {
            cardEditor.cardMode = cardEditor.cardMode === 'full' ? 'compact' : 'full';

            button.caption = localizeCaptionButton();
            const buttonHideCardMode = previewArea.buttons.find(b => b.name === 'hideWithCardMode');
            if (buttonHideCardMode) {
              buttonHideCardMode.caption = localizeCaptionButton();
            }
          },
          visibility: () => getButtonVisibility(true),
          name: 'switchWithCardMode'
        })
      );
    });
  }

  finalized(context: ICardUIExtensionContext): void {
    const previewArea = context.model.tryGetPreviewArea();
    if (!previewArea) {
      return;
    }

    const removeIndexHideWithModeButton = previewArea.buttons.findIndex(
      button => button.name === 'hideWithCardMode'
    );
    const removeIndexSwitchWithModeButton = previewArea.buttons.findIndex(
      button => button.name === 'switchWithCardMode'
    );

    if (removeIndexHideWithModeButton > 0 && removeIndexSwitchWithModeButton > 0) {
      runInAction(() => {
        previewArea.buttons.splice(removeIndexHideWithModeButton, 1);
        previewArea.buttons.splice(removeIndexSwitchWithModeButton, 1);
      });
    }
  }
}
