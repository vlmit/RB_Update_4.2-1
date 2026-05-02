import { AppPath } from '@tessa/application';
import {
  AttachmentsEditorHelper,
  RichAttachmentFile,
  RichAttachmentInnerItem
} from 'ui/richTextBox/modules';
import {
  AttachmentContentResolver,
  RichAttachment,
  RichAttachmentInfo,
  RichTextBoxAttachmentContainer
} from 'ui/richTextBox';

export class LargeContentRichAttachmentContainer extends RichTextBoxAttachmentContainer {
  //#region private static

  private static readonly _images = new Map([
    ['ab252bb2-74e2-4402-87fe-6580e220110e', 'panther.png'],
    ['da0ef68a-6d6e-479f-a7ad-f7bccdc83021', 'cat.png'],
    ['6271fc49-2889-44a3-9de8-62e437249e81', 'palms.jpg'],
    ['11cbaa31-5b7f-4250-99df-37b177388351', 'shore.png'],
    ['abb1e918-b398-4475-98e1-998bc7989776', 'logo.svg'],
    ['e8732438-ba2e-4a44-bd7b-4fad08db3ddd', 'font.png']
  ]);

  //#endregion

  //#region base overrides

  getAttachmentContentResolver(): AttachmentContentResolver {
    return async (attachment: RichAttachment): Promise<File | null> => {
      const { id } = attachment;

      let content =
        this._attachments.find(a => a.id === id)?.content ??
        this._prevAttachments.find(a => a.id === id)?.content;

      if (
        !content &&
        AttachmentsEditorHelper.isAttachmentType<RichAttachmentFile | RichAttachmentInnerItem>(
          attachment,
          RichAttachmentFile.type,
          RichAttachmentInnerItem.type
        )
      ) {
        content = LargeContentRichAttachmentContainer.fileLoader(attachment.caption);
      }

      if (!content) {
        return null;
      }

      return content instanceof File ? content : await content();
    };
  }

  getLink(attachment: RichAttachmentInfo | string): string | null {
    let id: string;
    if (typeof attachment === 'string') {
      id = attachment;
    } else {
      id = attachment.id;
    }

    const imageName = LargeContentRichAttachmentContainer._images.get(id);
    return imageName ? AppPath.publicPath(`images/playground/rich_image_${imageName}`) : null;
  }

  //#endregion

  //#region private methods

  private static fileLoader(name: string): () => Promise<File> {
    return async (): Promise<File> => {
      const path = AppPath.publicPath(`images/playground/rich_image_${name}`);
      const response = await fetch(path);
      const blob = await response.blob();
      return new File([blob], name, { type: blob.type });
    };
  }

  //#endregion
}
