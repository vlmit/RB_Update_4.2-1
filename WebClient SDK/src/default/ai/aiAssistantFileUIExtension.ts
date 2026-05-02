import { extension } from '@tessa/application';
import {
  IAiSettingsProvider$,
  IAiSettingsProvider,
  AiHelper,
  IAiOptions$,
  IAiOptions
} from 'tessa/ui/ai';
import { CardUIExtension, ICardUIExtensionContext } from 'tessa/ui/cards';
import { FileListViewModel } from 'tessa/ui/cards/controls';

@extension({ name: 'AiAssistantFileUIExtension' })
/** Extension companion for 'AiAssistantFileMenuExtension' that stores allowed for AI Assistant file extensions in `Card.Info`. */
export class AiAssistantFileUIExtension extends CardUIExtension {
  //#region constructors

  constructor(
    @IAiOptions$() protected readonly _aiOptions: IAiOptions,
    @IAiSettingsProvider$() private readonly _aiSettingsProvider: IAiSettingsProvider
  ) {
    super();
  }

  //#endregion

  //#region base overrides

  override async initialized(context: ICardUIExtensionContext): Promise<void> {
    if (!context.model.controlsBag.some(p => p instanceof FileListViewModel)) {
      return;
    }

    if (!this._aiOptions.isEnabled) {
      return;
    }

    const aiSettings = await this._aiSettingsProvider.getCommonSettings();
    if (
      !aiSettings.validationResult.isSuccessful ||
      !aiSettings.result ||
      !aiSettings.result.enabled
    ) {
      return;
    }

    const aiFileSettings = await this._aiSettingsProvider.getFileSettings();
    if (!aiFileSettings.validationResult.isSuccessful || !aiFileSettings.result) {
      return;
    }
    const extensions = new Array<string>();
    aiFileSettings.result
      .getAttachmentsSettings()
      .forEach(s => extensions.push(...s.fileExtensions));
    context.model.info[AiHelper.AiSupportedFileExtensions] = extensions;
  }

  //#endregion
}
