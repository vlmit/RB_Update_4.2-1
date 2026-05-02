import { extension, inject, ISession, ISession$, IUser, localize } from '@tessa/application';
import {
  CardFileDeletedInfo,
  CardHelper,
  IViewRepository,
  IViewRepository$,
  KrPermissionFlagDescriptors,
  KrToken,
  ViewCriteriaOperators,
  ViewRequestParameter,
  ViewRequestParameterBuilder
} from '@tessa/platform';
import { getTessaIcon } from 'common/utility/uiHelpers';
import { CardSingletonCache, ServerInstanceTypeName } from 'tessa/cards';
import { showConfirm } from 'tessa/ui/tessaDialog/show';
import { CardSavingMode, CardSavingRequest, ICardModel } from 'tessa/ui/cards';
import { FileControlExtension, IFileControl, IFileControlExtensionContext } from 'tessa/ui/files';
import { MenuAction } from 'tessa/ui/menuAction';
import { UIContext } from 'tessa/ui/uiContext';
import { showViewsDialog } from 'tessa/ui/uiHost/showViewsDialog';

/**
 * Расширение, добавляющее в меню файлового контрола пункт для восстановления файла из корзины.
 */
@extension({ name: 'RestoreFileControlExtension' })
export class RestoreFileControlExtension extends FileControlExtension {
  //#region constructors

  constructor(
    @inject(IViewRepository$) private readonly _viewRepository: IViewRepository,
    @inject(ISession$) private readonly _session: ISession
  ) {
    super();
  }

  //#endregion

  //#region base overrides

  public openingMenu(context: IFileControlExtensionContext): void {
    const cardModel = UIContext.current.cardEditor?.cardModel;
    if (
      !cardModel ||
      cardModel?.inSpecialMode ||
      RestoreFileControlExtension.isCardTaskFileControl(context.control)
    ) {
      return;
    }

    const serverInstance = CardSingletonCache.instance.cards.get(ServerInstanceTypeName);
    if (!serverInstance) {
      throw new Error(
        `Can not find card with name "${ServerInstanceTypeName}" at singleton cache.`
      );
    }

    const deleteFilesWithBackup = serverInstance.sections
      .get('ServerInstances')!
      .fields.tryGetBoolean('DeleteFilesWithBackup');

    if (!deleteFilesWithBackup) {
      return;
    }

    const uploadAction = context.actions.find(a => a.name === 'Upload');
    if (!uploadAction || uploadAction.isCollapsed) {
      return;
    }

    const uploadActionIndex = context.actions.indexOf(uploadAction);

    context.actions.splice(
      uploadActionIndex + 1,
      0,
      new MenuAction(
        'Restore',
        '$UI_Controls_FilesControl_RestoreFile',
        getTessaIcon('Thin119'),
        () => this.restoreAction(),
        null,
        !context.control.fileContainer.permissions.canAdd
      )
    );
  }

  //#endregion

  //#region private methods

  private static isCardTaskFileControl(fileControl: IFileControl): boolean {
    return !!fileControl.model.cardTask;
  }

  private static canRestoreAllDeletedFiles(user: IUser, cardModel: ICardModel): boolean {
    if (user.isAdmin) {
      return true;
    }
    const cardInfo = cardModel.card.tryGetInfo();
    return !!(
      cardInfo &&
      KrToken.tryGet(cardInfo)?.hasPermission(KrPermissionFlagDescriptors.RestoreAllDeletedFiles)
    );
  }

  private async restoreAction(): Promise<void> {
    const uiContext = UIContext.current;
    const cardEditor = uiContext.cardEditor;
    const cardModel = cardEditor?.cardModel;
    if (!cardModel) {
      return;
    }

    const viewName = 'DeletedFiles';
    const viewParameters: ViewRequestParameter[] = [];

    const view = await this._viewRepository.getByName(viewName);
    if (!view) {
      throw new Error(`Can not find view with alias "${viewName}" at view service.`);
    }

    const viewMetadata = await view.getMetadata();
    if (!viewMetadata) {
      throw new Error(`Can not find metadata for view "${viewName}".`);
    }

    const cardParameter = viewMetadata.parameters.tryGet('Card');
    if (!cardParameter) {
      throw new Error('Can not find parameter "Card" at view metadata.');
    }

    if (RestoreFileControlExtension.canRestoreAllDeletedFiles(this._session.user, cardModel)) {
      const showAllParameter = viewMetadata.parameters.tryGet('ShowAll');
      if (!showAllParameter) {
        throw new Error('Can not find parameter "ShowAll" at view metadata.');
      }

      viewParameters.push(
        new ViewRequestParameterBuilder()
          .withMetadata(showAllParameter)
          .addCriteria(ViewCriteriaOperators.IsTrue)
          .asRequestParameter()
      );
    }

    viewParameters.push(
      new ViewRequestParameterBuilder()
        .withMetadata(cardParameter)
        .addCriteria(ViewCriteriaOperators.EqualsTo, '', cardModel.card.id)
        .asRequestParameter()
    );

    await showViewsDialog(
      [viewName],
      async ({ selectedRow }) => {
        if (!selectedRow) {
          return;
        }

        const confirmation = localize(
          '$UI_Controls_FilesControl_RestoreSelectedFileMessage',
          selectedRow.get('DeletedName')
        );

        if (!(await showConfirm(confirmation))) {
          return;
        }

        const fileInfo = new CardFileDeletedInfo();
        fileInfo.rowId = selectedRow.get('DeletedRowID');
        fileInfo.deleted = selectedRow.get('DeletedDate');
        fileInfo.deletedById = selectedRow.get('DeletedByID');
        fileInfo.deletedByName = selectedRow.get('DeletedByName');

        await cardEditor.saveCard(
          uiContext,
          undefined,
          new CardSavingRequest(CardSavingMode.RefreshOnSuccess, card => {
            card.info[CardHelper.filesToRestoreKey] = [fileInfo.serializeToStorage()];
          })
        );
      },
      viewParameters
    );
  }

  //#endregion
}
