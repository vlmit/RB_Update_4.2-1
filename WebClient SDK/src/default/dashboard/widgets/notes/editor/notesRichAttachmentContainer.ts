import { AppPath } from '@tessa/application';
import { Guid, IStorage } from '@tessa/core';
import { CardGetFileContentRequest, CardHelper, ICardService } from '@tessa/platform';
import { fileTypeId, fileTypeName } from 'tessa/cards/cardFileType';
import { getFileLink } from 'tessa/cards/fileLinkHelper';
import { DashboardWidgetFileState, DashboardWidgetFileStorage } from 'tessa/ui/dashboard';
import { RichAttachmentFile } from 'ui/richTextBox/modules/attachments';
import {
  AttachmentContentResolver,
  IRichTextBoxAttachmentContainer,
  RichAttachment,
  RichAttachmentContent,
  RichAttachmentInfo
} from 'ui/richTextBox';

export class NotesRichAttachmentContainer implements IRichTextBoxAttachmentContainer {
  //#region constructors

  constructor(
    private readonly _cardService: ICardService,
    private _cardId: string,
    private readonly _files: DashboardWidgetFileStorage[] = []
  ) {}

  //#endregion

  //#region fields

  private _attachments: RichAttachmentInfo[] = [];

  //#endregion

  //#region properties

  get files(): ReadonlyArray<DashboardWidgetFileStorage> {
    return this._files;
  }

  //#endregion

  //#region public methods

  setCardId(cardId: string): void {
    this._cardId = cardId;
  }

  //#endregion

  //#region IRichTextBoxAttachmentContainer

  get attachments(): ReadonlyArray<RichAttachmentInfo> {
    return this._attachments;
  }

  async add(attachment: RichAttachmentInfo): Promise<void> {
    let file = this._files.find(f => Guid.equals(f.id, attachment.id));
    if (!file) {
      file = new DashboardWidgetFileStorage();
      file.id = attachment.id;
      file.state = DashboardWidgetFileState.Inserted;
      file.name = this.getAttachmentDescription(attachment);
      file.content = await this.resolveAttachmentContent(attachment.content!);
      this._files.push(file);
      this._attachments.push(attachment);
    } else if (file.state === DashboardWidgetFileState.Deleted) {
      file.state = DashboardWidgetFileState.None;
    } else if (file.state === DashboardWidgetFileState.Inserted) {
      const existedAttachment = this._attachments.find(a => Guid.equals(a.id, attachment.id));
      existedAttachment && (existedAttachment.content = attachment.content);
    }
  }

  async remove(attachment: RichAttachmentInfo): Promise<boolean> {
    const fileChanged = this.deleteFile(attachment);

    const attachmentIndex = this._attachments.findIndex(a => Guid.equals(a.id, attachment.id));
    if (attachmentIndex < 0) {
      return false || fileChanged;
    }

    this._attachments.splice(attachmentIndex, 1);
    return true;
  }

  async clear(): Promise<void> {
    this._attachments.length = 0;
  }

  async reset(): Promise<void> {
    await this.clear();
    this._files.length = 0;
  }

  getAttachmentContentResolver(): AttachmentContentResolver {
    return async (
      attachment: RichAttachment,
      info?: IStorage,
      save = false
    ): Promise<File | null> => {
      const existedAttachment = this._attachments.find(a => Guid.equals(a.id, attachment.id));

      if (save) {
        await this.saveAttachmentContent(attachment, info);
        return null;
      }

      return existedAttachment
        ? await this.resolveAttachmentContent(existedAttachment.content!, info)
        : await this.getAttachmentContent(attachment, info);
    };
  }

  getLink(attachment: RichAttachmentInfo | string): string | null {
    let id: string;
    if (typeof attachment === 'string') {
      id = attachment;
    } else {
      id = attachment.id;
    }

    return AppPath.fullPath(
      getFileLink(
        this._cardId!,
        id,
        CardHelper.DashboardTypeID,
        CardHelper.DashboardTypeName,
        undefined,
        'File',
        id
      )
    );
  }

  //#endregion

  //#region private methods

  private deleteFile(attachment: RichAttachmentInfo): boolean {
    const fileIndex = this._files.findIndex(f => Guid.equals(f.id, attachment.id));
    let file = this._files[fileIndex];

    if (!file) {
      file = new DashboardWidgetFileStorage();
      file.id = attachment.id;
      file.state = DashboardWidgetFileState.Deleted;
      file.name = this.getAttachmentDescription(attachment);
      this._files.push(file);
      return true;
    } else if (file.state === DashboardWidgetFileState.Inserted) {
      this._files.splice(fileIndex, 1);
      return true;
    } else if (file.state !== DashboardWidgetFileState.Deleted) {
      file.state = DashboardWidgetFileState.Deleted;
      return true;
    }

    return false;
  }

  private async saveAttachmentContent(attachment: RichAttachment, info?: IStorage): Promise<void> {
    const getFileContentRequest = this.createCardGetFileContentRequest(attachment, true, info);
    const validationResult = await this._cardService.getAndSaveFileContent(getFileContentRequest);
    if (!validationResult.isSuccessful) {
      console.error(validationResult);
    }
  }

  private async getAttachmentContent(
    attachment: RichAttachment,
    info?: IStorage
  ): Promise<File | null> {
    const getFileContentRequest = this.createCardGetFileContentRequest(attachment, false, info);
    const getFileContentResponse = await this._cardService.getFileContent(getFileContentRequest);
    if (!getFileContentResponse.validationResult.isSuccessful || !getFileContentResponse.content) {
      console.error(getFileContentResponse.validationResult.build());
      return null;
    }

    const fileName = getFileContentResponse.fileName ?? 'unknown';
    return new File([getFileContentResponse.content], fileName, {
      lastModified: new Date().getTime()
    });
  }

  private async resolveAttachmentContent(
    content: RichAttachmentContent,
    info?: IStorage
  ): Promise<File | null> {
    return content instanceof File ? content : await content(info);
  }

  private getAttachmentDescription(attachment: RichAttachmentInfo): string {
    return attachment.name || attachment.caption || attachment.id;
  }

  private createCardGetFileContentRequest(
    attachment: RichAttachment,
    save: boolean,
    info?: IStorage
  ): CardGetFileContentRequest {
    const getFileContentRequest = new CardGetFileContentRequest();
    getFileContentRequest.suggestFileNameForVersion = true;
    getFileContentRequest.fileId = attachment.id;
    getFileContentRequest.fileName =
      attachment.type === RichAttachmentFile.type && attachment instanceof RichAttachmentFile
        ? attachment.caption
        : null;
    getFileContentRequest.versionRowId = attachment.id;
    getFileContentRequest.fileTypeId = fileTypeId;
    getFileContentRequest.fileTypeName = fileTypeName;
    getFileContentRequest.cardId = this._cardId;
    getFileContentRequest.cardTypeId = CardHelper.DashboardTypeID;
    getFileContentRequest.cardTypeName = CardHelper.DashboardTypeName;
    getFileContentRequest.supportContentToken = save;
    getFileContentRequest.info = info ?? {};

    return getFileContentRequest;
  }

  //#endregion
}
