import { IApiClient, IApiClient$, extension, inject } from '@tessa/application';
import { getTessaIcon } from 'common';
import { MenuAction, UIContext } from 'tessa/ui';
import { FileExtension, IFileExtensionContext } from 'tessa/ui/files';
import { TempLinkDialogs } from './tempLinkCreationDialogs';
import { FileVersionState } from 'tessa/files/fileVersion';
import {
  ICardSingletonCache,
  ICardSingletonCache$,
  IContentService,
  IContentService$
} from '@tessa/platform';

/** File extension that adds menu action "Temp link". */
@extension({ name: 'TempLinkFileExtension' })
export class TempLinkFileExtension extends FileExtension {
  //#region ctor

  constructor(
    @inject(ICardSingletonCache$) private readonly cardsCache: ICardSingletonCache,
    @inject(IContentService$) private readonly contentService: IContentService,
    @inject(IApiClient$) private readonly apiClient: IApiClient
  ) {
    super();
  }

  //#endregion

  //#region base overrides

  public openingMenu(context: IFileExtensionContext): void {
    const file = context.file.model;
    const editor = UIContext.current.cardEditor;
    const cardModel = editor!.cardModel!;
    const card = cardModel.card;
    const cardFile = card.files.find(x => x.rowId == file.id);

    const isCollapsed =
      file.lastVersion.state !== FileVersionState.Success || file.lastVersion === file.versionAdded;

    let insertIndex = context.actions.findIndex(x => x.name === 'CopyLink');
    if (insertIndex < 0) {
      insertIndex = context.actions.length - 1;
    }
    // adding action
    context.actions.splice(
      insertIndex + 1,
      0,
      new MenuAction(
        'CreateTempLink',
        '$UI_Controls_FilesControl_TempLink',
        getTessaIcon('Thin117'),
        async () => {
          // action
          await TempLinkDialogs.createAndShowTempLink(
            this.cardsCache,
            this.contentService,
            this.apiClient,
            card.id,
            file.id,
            null,
            cardFile?.typeName ?? '',
            cardFile?.isVirtual ?? false,
            false
          );
        },
        null,
        isCollapsed
      )
    );
  }

  //#endregion
}
