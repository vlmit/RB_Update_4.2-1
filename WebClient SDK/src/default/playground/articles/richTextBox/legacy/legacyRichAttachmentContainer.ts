import { AppPath } from '@tessa/application';
import {
  AttachmentContentResolver,
  RichAttachment,
  RichTextBoxAttachmentContainer
} from 'ui/richTextBox';

export class LegacyRichAttachmentContainer extends RichTextBoxAttachmentContainer {
  //#region base overrides

  getAttachmentContentResolver(): AttachmentContentResolver {
    return async ({ id }: RichAttachment): Promise<File | null> => {
      const content =
        this._attachments.find(a => a.id === id)?.content ??
        this._prevAttachments.find(a => a.id === id)?.content ??
        LegacyRichAttachmentContainer.logoFileLoader;

      return content instanceof File ? content : await content();
    };
  }

  getLink(): string | null {
    return AppPath.publicPath('images/playground/rich_image_logo.svg');
  }

  //#endregion

  //#region private methods

  private static async logoFileLoader(): Promise<File> {
    const path = AppPath.publicPath(`images/playground/rich_image_logo.svg`);
    const response = await fetch(path);
    const blob = await response.blob();
    return new File([blob], 'rich_image_logo.svg', { type: blob.type });
  }

  //#endregion
}
