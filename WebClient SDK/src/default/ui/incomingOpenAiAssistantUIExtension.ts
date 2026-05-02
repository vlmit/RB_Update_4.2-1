import { extension, localize } from '@tessa/application';
import { Guid } from '@tessa/core';
import { FileHelper } from '@tessa/platform';
import { getTessaIcon } from 'common';
import { IFileVersion } from 'tessa/files';
import {
  AiAssistantControlDialog,
  AiAttachmentGroupSettings,
  AiContext,
  AiContextType,
  AiFileInfoAdapter,
  AiFileMessagePart,
  AiFileRequestOperation,
  AiFileSettings,
  AiHelper,
  IAiOptions,
  IAiOptions$,
  IAiSettingsProvider,
  IAiSettingsProvider$
} from 'tessa/ui/ai';
import { CardToolbarAction, CardUIExtension, ICardUIExtensionContext } from 'tessa/ui/cards';

@extension({ name: 'IncomingOpenAiAssistantUIExtension' })
export class IncomingOpenAiAssistantUIExtension extends CardUIExtension {
  //#region constants

  private static readonly _aiAssistantButtonName = 'AiAssistantButton';

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

  override async initialized(context: ICardUIExtensionContext): Promise<void> {
    IncomingOpenAiAssistantUIExtension.removeToolbarAction(context);

    const aiSettings = await this._aiSettingsProvider.getCommonSettings();
    context.validationResult.add(aiSettings.validationResult);

    const aiFileSettings = await this._aiSettingsProvider.getFileSettings();
    context.validationResult.add(aiFileSettings.validationResult);

    if (
      !aiSettings.result ||
      !aiSettings.result.model?.id ||
      !aiSettings.validationResult.isSuccessful ||
      !aiFileSettings.result ||
      aiFileSettings.result.maxFileCount === 0 ||
      !aiFileSettings.validationResult.isSuccessful ||
      !this._aiOptions.isEnabled ||
      !AiHelper.hasAiLicense()
    ) {
      return;
    }

    const attachmentsSettings = aiFileSettings.result.getAttachmentsSettings();
    const maxFileSize =
      aiFileSettings.result.maxFileSizeKb != null
        ? aiFileSettings.result.maxFileSizeKb * 1000
        : Number.MAX_VALUE;

    if (!this.firstFileLastVersionOrDefault(context, attachmentsSettings, maxFileSize)) {
      return;
    }

    this.addToolbarAction(context, attachmentsSettings, maxFileSize);

    const disposer = context.fileContainer.containerFileChanged.addWithDispose(i => {
      if (
        i.removed &&
        !this.firstFileLastVersionOrDefault(context, attachmentsSettings, maxFileSize)
      ) {
        IncomingOpenAiAssistantUIExtension.removeToolbarAction(context);
      }
    });

    if (disposer) {
      this.disposeList.add(disposer);
    }
  }

  //#endregion

  //#region private methods

  private firstFileLastVersionOrDefault(
    context: ICardUIExtensionContext,
    attachmentsSettings: ReadonlyMap<string, AiAttachmentGroupSettings>,
    maxFileSize: number
  ): IFileVersion | undefined {
    const sortedCardFiles = context.card
      .tryGetFiles()
      ?.filter(i => {
        return (
          !i.isVirtual &&
          i.size <= maxFileSize &&
          AiFileSettings.getAttachmentGroupByExtension(
            attachmentsSettings,
            FileHelper.getFileExtension(i.name)
          )
        );
      })
      .sort((a, b) => a.name.localeCompare(b.name));

    if (!sortedCardFiles?.length) {
      return;
    }

    for (const sortedCardFile of sortedCardFiles) {
      const sortedCardFileRowId = sortedCardFile.rowId;
      const file = context.fileContainer.files.find(i => Guid.equals(i.id, sortedCardFileRowId));

      if (!file || file.origin) {
        continue;
      }

      const lastVersion = file.lastVersion;
      if (!AiFileInfoAdapter.isSupported(lastVersion)) {
        continue;
      }

      return lastVersion;
    }

    return;
  }

  private addToolbarAction(
    context: ICardUIExtensionContext,
    attachmentsSettings: ReadonlyMap<string, AiAttachmentGroupSettings>,
    maxFileSize: number
  ) {
    context.toolbar.addItem(
      new CardToolbarAction({
        name: IncomingOpenAiAssistantUIExtension._aiAssistantButtonName,
        caption: '$Ai_OutgoingWriterAiAgentPlugin_TileName',
        icon: getTessaIcon('Thin218'),
        command: async () => {
          const firstFileLastVersion = this.firstFileLastVersionOrDefault(
            context,
            attachmentsSettings,
            maxFileSize
          );

          // Такого быть не должно при нормальной работе.
          if (!firstFileLastVersion) {
            return;
          }

          const filePart = new AiFileMessagePart(Guid.empty, AiFileRequestOperation.Text);

          const dialog = new AiAssistantControlDialog({
            options: {
              tool: 'outgoing_write',
              shadowMessageParts: [filePart],
              context: new AiContext(AiContextType.Card, context.card.id, context.card.typeId),
              startupMessage: localize('$Ai_OutgoingWriterAiAgentPlugin_DialogStartupMessage')
            }
          });

          try {
            await dialog.initialize();

            // Ожидание асинхронного вызова не выполняется, так как блокируется UI.
            // Файлы, которые не прошли валидацию не будут загружены, но по ним будут метаданные,
            // поэтому такие файлы тоже добавляются, чтобы для них сработала единая валидация контрола.
            dialog.control
              .addAttachment(new AiFileInfoAdapter(firstFileLastVersion), true)
              .then(attachment => {
                if (attachment?.fileId) {
                  const newFilePart = new AiFileMessagePart(
                    attachment.fileId,
                    dialog.control.configuration.getAttachmentOperation(attachment.group),
                    attachment.fileToken
                  );
                  dialog.control.shadowMessage!.content?.splice(0, 1, newFilePart);
                }
              });

            await dialog.show({ chromeSettings: { title: localize('$UI_Tiles_AiAssistant') } });
          } finally {
            dialog.dispose();
          }
        }
      })
    );
  }

  private static removeToolbarAction(context: ICardUIExtensionContext) {
    context.toolbar.removeItemIfExists(IncomingOpenAiAssistantUIExtension._aiAssistantButtonName);
  }

  //#endregion
}
