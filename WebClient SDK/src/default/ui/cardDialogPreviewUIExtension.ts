import { reaction } from 'mobx';
import { extension } from '@tessa/application';
import {
  CardEditorModelResizeArgs,
  CardUIExtension,
  ICardUIExtensionContext
} from 'tessa/ui/cards';
import { PlatformSize } from 'common/platform';

/**
 * Расширение для карточек, открывающихся в диалоге.
 *
 * Если карточка открывается в диалоговом окне
 * или основной предпросмотр карточки скрыт из-за ширины экрана,
 * предпросмотр файлов будет выводиться так же в диалоговом окне.
 */
@extension({ name: 'CardDialogPreviewUIExtension' })
export class CardDialogPreviewUIExtension extends CardUIExtension {
  private _disposes: Array<(() => void) | null> = [];
  private _isNeedUpdatePreviewCollapsed = true;

  async contextInitialized(context: ICardUIExtensionContext): Promise<void> {
    if (!context.model.previewManager.previewArea?.enabled) {
      return;
    }

    if (!!context.dialogName) {
      context.model.previewManager.showInDialogByDefault = true;
      this._isNeedUpdatePreviewCollapsed = false;
    }

    const { cardEditor } = context.uiContext;
    if (!cardEditor) {
      return;
    }
    const resize = cardEditor.resize;
    if (!resize.events.has(this.updatePreviewInDialogOnResize)) {
      this._disposes.push(
        resize.addWithDispose(this.updatePreviewInDialogOnResize),
        // при компактном режиме карточке + задания,
        // изменяя previewInDialog, меняется размер карточки
        reaction(
          () => [cardEditor.cardMode, context.model.previewManager.showInDialogByDefault],
          () => {
            this._isNeedUpdatePreviewCollapsed = false;
          }
        )
      );
    }
  }

  finalized(): void {
    for (const func of this._disposes) {
      if (func) {
        func();
      }
    }
    this._disposes.length = 0;
  }

  private updatePreviewInDialogOnResize = (e: CardEditorModelResizeArgs): void => {
    const { cardModel } = e.cardEditorModel;
    const { form, container } = e.sizes;

    if (!cardModel || !container || !form) {
      return;
    }

    const previewArea = cardModel.tryGetPreviewArea();
    if (!previewArea) {
      return;
    }

    if (form.width <= PlatformSize.md) {
      previewArea.previewButtonIsHidden = true;
    } else {
      previewArea.previewButtonIsHidden = false;
    }

    if (form.width <= PlatformSize.md) {
      previewArea.isCollapsed = true;
    } else if (!previewArea.alwaysHidden && this._isNeedUpdatePreviewCollapsed) {
      previewArea.isCollapsed = false;
    }

    if (!this._isNeedUpdatePreviewCollapsed) {
      this._isNeedUpdatePreviewCollapsed = true;
    }
  };
}
