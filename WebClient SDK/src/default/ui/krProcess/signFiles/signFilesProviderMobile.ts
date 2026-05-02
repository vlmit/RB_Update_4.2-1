import { ICardModel } from 'tessa/ui/cards';
import { ISignFilesProvider } from './signFilesTypes';
import { TaskViewModel } from 'tessa/ui/cards/tasks';
import { LoadingOverlay, showError, showNotEmpty } from 'tessa/ui';
import { FileListViewModel, FileViewModel, isCanceledError } from 'tessa/ui/cards/controls';
import {
  DeskiMobileCancelOperationResponse,
  DeskiMobileFile,
  DeskiMobileHelper,
  DeskiMobileInitOperationRequest,
  DeskiMobileOperation,
  DeskiMobileService,
  DeskiMobileVerifyOperationResponse
} from '../../../deskiMobile';
import { ValidationResult } from '@tessa/core';
import { TableFileRowViewModel } from '../../cardFiles/tableFileRowViewModel';
import { SignFileHelper } from './signFileHelper';
import { injectable } from '@tessa/application';

@injectable()
export class SignFilesProviderMobile implements ISignFilesProvider {
  //#region public methods

  async signFilesAction(cardModel: ICardModel, taskViewModel: TaskViewModel): Promise<boolean> {
    if (!DeskiMobileService.instance.deskiMobileEnabled) {
      await showError('$KrProcess_ErrorMessage_TessaAssistantIsDisabled');
      return false;
    }

    return await SignFileHelper.handleSignFiles(
      cardModel,
      taskViewModel,
      async (shownFilesVMs, selectedFilesVMs, cardModel, _noCommentDialog, noSignFileDialog) => {
        // Если не требуется показывать диалог — подписываем выбранные или все показанные файлы
        if (noSignFileDialog) {
          return await this.trySignFiles(selectedFilesVMs ?? shownFilesVMs, cardModel);
        }
        // Если файлов один или меньше — подписываем их без диалога
        if (shownFilesVMs.length <= 1) {
          return await this.trySignFiles(shownFilesVMs, cardModel);
        }
        // Иначе открываем диалог выбора файлов для подписи
        return await SignFileHelper.showSignFilesDialog(
          shownFilesVMs,
          selectedFilesVMs,
          cardModel,
          async (selectedRows, cardModel) => {
            return await this.mobileSignDialogButtonAction(selectedRows, cardModel);
          }
        );
      }
    );
  }

  //#endregion

  //#region private methods

  private async trySignFiles(files: FileViewModel[], cardModel: ICardModel): Promise<boolean> {
    if (files.length === 0) {
      return true;
    }

    const fileControlModel = cardModel.controls.get('Files') as FileListViewModel;

    return await LoadingOverlay.instance.show(async () => {
      return await this.innerSignFiles(files, fileControlModel);
    });
  }

  private async mobileSignDialogButtonAction(
    selectedRows: TableFileRowViewModel[],
    cardModel: ICardModel
  ): Promise<boolean> {
    return await LoadingOverlay.instance.show(async () => {
      const fileViewModels = selectedRows.map(row => row.fileViewModel);
      const fileControlModel = cardModel.controls.get('Files') as FileListViewModel;
      return await this.innerSignFiles(fileViewModels, fileControlModel);
    });
  }

  private async innerSignFiles(
    filesVm: FileViewModel[],
    fileControlModel: FileListViewModel
  ): Promise<boolean> {
    try {
      const files: DeskiMobileFile[] = [];
      for (const file of filesVm) {
        const fileRequest = file.model.source.getLinkData(file.model);
        const fileState = file.getFileState();

        if (fileState !== '') {
          await showError('$KrProcess_ErrorMessage_SaveFileBeforeSignInMobileAssistant');
          return false;
        }

        if (!fileRequest) {
          await showError('FileResult is null.');
          return false;
        }

        files.push(new DeskiMobileFile(fileRequest));
      }

      const requestBody = new DeskiMobileInitOperationRequest(files);

      const operationResult = await DeskiMobileHelper.startOperation(requestBody, 'sign');
      if (!operationResult) {
        return false;
      }

      const token = await DeskiMobileHelper.handleTrackingDeskiMobile(
        operationResult.link,
        operationResult.token
      );

      if (!token) {
        return false;
      }
      // если токен валиден, значит ассистент был запущен, обновляем время успешного запуска.
      DeskiMobileHelper.setTimeSuccessfulStartAssistant();

      const [response, validationResult] = await DeskiMobileHelper.trackingOperation(
        DeskiMobileOperation.sign,
        token
      );

      if (!validationResult.isSuccessful) {
        await showNotEmpty(validationResult);
        return false;
      }

      if (!response) {
        await showError('Operation response is null.');
        return false;
      }

      // операция отменена
      if (response instanceof DeskiMobileCancelOperationResponse) {
        return false;
      }

      if (response instanceof DeskiMobileVerifyOperationResponse) {
        await showError('Operation response has operation verify result.');
        return false;
      }

      for (const file of files) {
        const result = response.info.get(file.cacheID);
        if (!result) {
          await showError(`Can't find response for file with cacheID=${file.cacheID}.`);
          return false;
        }

        const currentFile = fileControlModel.files.find(e => e.model.id === file.fileID);
        if (!currentFile) {
          await showError(`Can't find file in fileControlModel with fileID=${file.fileID}.`);
          return false;
        }

        await fileControlModel.fileContainer.addSignature(
          currentFile.model.lastVersion,
          result.certData,
          result.signedData,
          ''
        );
      }

      return true;
    } catch (err) {
      if (!isCanceledError(err)) {
        await showNotEmpty(ValidationResult.fromError(err));
      }
      return false;
    }
  }

  //#endregion
}
