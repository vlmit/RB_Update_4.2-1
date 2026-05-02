import { Guid, TypedField } from '@tessa/core';
import { Card, CardFile, CardFileState } from '@tessa/platform';
import { fileTypeCaption, fileTypeId, fileTypeName } from 'tessa/cards';
import {
  AttachmentContentResolver,
  IRichTextBoxAttachmentContainer,
  RichAttachment,
  RichAttachmentInfo
} from 'ui/richTextBox';
import { FakeCardService } from './fakeCardService';

export class FakeCardArticleAttachmentContainer implements IRichTextBoxAttachmentContainer {
  //#region ctor

  constructor(
    private readonly _getCard: () => Card,
    private readonly _cardService: FakeCardService
  ) {}

  //#endregion

  //#region fields

  private _attachments: RichAttachmentInfo[] = [];

  //#endregion

  //#region props

  get attachments(): readonly RichAttachmentInfo[] {
    return this._attachments;
  }

  //#endregion

  //#region methods

  async add(attachment: RichAttachmentInfo): Promise<void> {
    const card = this._getCard();

    let cardFile = card.files.find(f => Guid.equals(f.rowId, attachment.id));
    if (!cardFile) {
      cardFile = new CardFile();
      cardFile.state = CardFileState.Inserted;
      cardFile.rowId = attachment.id;
      cardFile.versionRowId = attachment.id;
      cardFile.name = attachment.name;
      cardFile.typeId = fileTypeId;
      cardFile.typeCaption = fileTypeCaption;
      cardFile.typeName = fileTypeName;
      cardFile.info['.FileSatelliteFile'] = TypedField.trueBoolean;

      card.files.add(cardFile);
      this._attachments.push(attachment);
    } else if (cardFile.state === CardFileState.Deleted) {
      cardFile.state = CardFileState.None;
    } else if (cardFile.state === CardFileState.Inserted) {
      const existedAttachment = this._attachments.find(a => Guid.equals(a.id, attachment.id));
      existedAttachment && (existedAttachment.content = attachment.content);
    }
  }

  async remove(attachment: RichAttachmentInfo): Promise<boolean> {
    const fileStateChanged = this.removeFile(attachment);

    const existedFileIndex = this._attachments.findIndex(a => Guid.equals(a.id, attachment.id));
    if (existedFileIndex > -1) {
      this._attachments.splice(existedFileIndex, 1);
      return true;
    }
    return fileStateChanged;
  }

  async clear(): Promise<void> {
    this._attachments.length = 0;
  }

  getAttachmentContentResolver(): AttachmentContentResolver {
    return async (attachment: RichAttachment) => {
      const savedContent = this._attachments.find(a => Guid.equals(a.id, attachment.id))
        ?.content as File;
      if (savedContent) {
        return savedContent;
      }
      return await this._cardService.getFileContent(attachment.id);
    };
  }

  getLink(_attachment: RichAttachmentInfo | string): string | null {
    return null;
  }

  //#endregion

  //#region private methods

  private removeFile(attachment: RichAttachmentInfo): boolean {
    const card = this._getCard();

    const cardFile = card.files.find(f => Guid.equals(f.rowId, attachment.id));
    if (!cardFile) {
      return false;
    }

    if (cardFile.state === CardFileState.Inserted) {
      card.files.remove(cardFile);
      return true;
    }

    if (cardFile.state !== CardFileState.Deleted) {
      cardFile.state = CardFileState.Deleted;
      return true;
    }

    return false;
  }

  //#endregion
}
