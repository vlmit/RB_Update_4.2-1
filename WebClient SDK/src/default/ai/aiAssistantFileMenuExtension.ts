import { extension, localize } from '@tessa/application';
import { Guid, StorageHelper, StringHelper } from '@tessa/core';
import { FileHelper } from '@tessa/platform';
import { getTessaIcon } from 'common';
import { IFile } from 'tessa/files';
import { MenuAction, showError, showNotEmpty, UIContext } from 'tessa/ui';
import {
  AiAssistantAttachmentViewModel,
  AiAssistantControlDialog,
  AiAssistantOptions,
  AiAssistantViewModel,
  AiContext,
  AiContextType,
  AiFileInfoAdapter,
  AiFileMessagePart,
  AiFileRequestOperation,
  AiHelper,
  IAiFileInfo,
  IAiOptions,
  IAiOptions$,
  IAiSettingsProvider,
  IAiSettingsProvider$
} from 'tessa/ui/ai';
import { FileExtension, IFileExtensionContext } from 'tessa/ui/files';

@extension({ name: 'AiAssistantFileMenuExtension' })
export class AiAssistantFileMenuExtension extends FileExtension {
  //#region constants

  private readonly fileNameMaxDisplayChars: number = 50;
  private readonly fileDiscussionToolName = 'file_discussion';

  //#endregion

  //#region ctor

  constructor(
    @IAiOptions$() protected readonly _aiOptions: IAiOptions,
    @IAiSettingsProvider$() protected readonly _aiSettingsProvider: IAiSettingsProvider
  ) {
    super();
  }

  //#endregion

  //#region base overrides

  override openingMenu(context: IFileExtensionContext): void {
    if (!this._aiOptions.isEnabled || !AiHelper.hasAiLicense()) {
      return;
    }

    // AI cache supports only non-virtual files attached to the card, or virtual files with special flag in "info"
    const file = context.file.model;
    const cardModel = UIContext.current.cardEditor?.cardModel;
    const card = cardModel?.card;
    const cardFile = card?.tryGetFiles()?.find(x => x.rowId === file.id);
    if (!cardFile) {
      return;
    }

    if (cardFile.isVirtual) {
      const info = cardFile.tryGetInfo();
      if (
        !info ||
        !StorageHelper.tryGet<boolean>(info, AiHelper.AllowAiFileDiscussionActionKey) ||
        !AiFileInfoAdapter.isSupported(file.lastVersion)
      ) {
        return;
      }
    }

    const fileExtension = FileHelper.getFileExtension(file.name).toLowerCase();
    const supportedFileExtensions = StorageHelper.tryGet<Array<string>>(
      cardModel?.info,
      AiHelper.AiSupportedFileExtensions
    );
    if (!supportedFileExtensions?.includes(fileExtension)) {
      return;
    }

    let insertIndex = context.actions.length - 2;
    if (insertIndex < 0) {
      insertIndex = context.actions.length;
    }

    context.actions.splice(
      insertIndex,
      0,
      MenuAction.create({
        name: 'AiFileDiscussionAction',
        caption: '$Ai_FileDiscussionPlugin_MenuItemCaption',
        icon: getTessaIcon('Thin219'),
        action: async () => {
          await this.openAiAgentDialog(file);
        }
      })
    );
  }

  //#endregion

  //#region private methods

  private async openAiAgentDialog(file: IFile): Promise<void> {
    const aiFileSettings = await this._aiSettingsProvider.getFileSettings();
    if (!aiFileSettings.validationResult.isSuccessful || !aiFileSettings.result) {
      await showError(aiFileSettings.validationResult.toString());
      return;
    }

    const fileValidationResult = AiHelper.validateAttachment(file, aiFileSettings.result);
    if (!fileValidationResult.isSuccessful) {
      await showError(fileValidationResult.toString());
      return;
    }

    const card = UIContext.current.cardEditor!.cardModel!.card;

    const options: AiAssistantOptions = {
      context: new AiContext(AiContextType.Card, card.id, card.typeId),
      tool: this.fileDiscussionToolName,
      startupMessage: localize('$Ai_FileDiscussionPlugin_StartupMessage', file.name),
      shadowMessageParts: [new AiFileMessagePart(Guid.empty, AiFileRequestOperation.Text)]
    };

    const dialog = new AiAssistantControlDialog({ options });

    try {
      await dialog.initialize();

      this.addAttachmentFromCardOrLocally(dialog.control, file).then(attachment => {
        if (attachment?.fileId) {
          const kind = dialog.control.configuration.getAttachmentOperation(attachment.group);
          const newFilePart = new AiFileMessagePart(attachment.fileId, kind, attachment.fileToken);
          dialog.control.shadowMessage?.content?.splice(0, 1, newFilePart);
        }
      });

      await dialog.show({
        chromeSettings: {
          title: localize(
            '$Ai_FileDiscussionPlugin_DialogTitle',
            StringHelper.limit(file.name, this.fileNameMaxDisplayChars)
          )
        }
      });
    } finally {
      dialog.dispose();
    }
  }

  private async addAttachmentFromCardOrLocally(
    assistant: AiAssistantViewModel,
    file: IFile
  ): Promise<AiAssistantAttachmentViewModel | null> {
    let initialFile: File | IAiFileInfo;

    // Если файл ещё не был сохранен в карточку - загружаем его контент и кешируем как внешний файл
    const lastVersion = file.lastVersion;
    if (file.isLocal) {
      const getContentResult = await lastVersion.ensureContentDownloaded();
      if (!getContentResult.isSuccessful || !lastVersion.content) {
        console.error(getContentResult);
        await showNotEmpty(getContentResult);
        return null;
      }

      initialFile = lastVersion.content;
    } else {
      if (!AiFileInfoAdapter.isSupported(lastVersion)) {
        return null;
      }

      initialFile = new AiFileInfoAdapter(lastVersion);
    }

    return await assistant.addAttachment(initialFile, true);
  }

  //#endregion
}
