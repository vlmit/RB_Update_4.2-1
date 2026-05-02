import { extension } from '@tessa/application';
import { MenuAction } from 'tessa/ui';
import { FileVersionState } from 'tessa/files';
import { FileExtension, FileExtensionContext } from 'tessa/ui/files';
import { MessageBoxButtons, show, showError, showNotEmpty } from 'tessa/ui/tessaDialog';
import Platform from 'common/platform';
import { ISignaturesVerifier$ } from 'tessa/cards/eds/edsInjects';
import { ISignaturesVerifier } from 'tessa/cards/eds/edsTypes';
import { DeskiMobileFile } from './deskiMobileFile';
import { DeskiMobileOperation, DeskiMobileService } from './deskiMobileService';
import { DeskiMobileInitOperationRequest } from './deskiMobileInitOperationRequest';
import { DeskiMobileCancelOperationResponse } from './deskiMobileCancelOperationResponse';
import { DeskiMobileVerifyOperationResponse } from './deskiMobileVerifyOperationResponse';
import { DeskiMobileHelper } from './deskiMobileHelper';

@extension({ name: 'DeskiMobileFileExtension' })
export class DeskiMobileFileExtension extends FileExtension {
  constructor(@ISignaturesVerifier$() private readonly _signatureVerifier: ISignaturesVerifier) {
    super();
  }

  override openingMenu(context: FileExtensionContext): void {
    if (!DeskiMobileService.instance.deskiMobileEnabled || !Platform.isMobile()) {
      return;
    }

    const control = context.control;
    const signIndex = context.actions.findIndex(x => x.name === 'SignEDS');
    if (signIndex >= 0) {
      const signDSCollapsed = context.files.some(
        x =>
          !x.model.permissions.canUseSignatures ||
          !x.model.permissions.canSign ||
          x.model.lastVersion.state !== FileVersionState.Success
      );
      context.actions.splice(
        signIndex,
        1,
        new MenuAction(
          'SignEDS',
          '$UI_Controls_FilesControl_SignDS',
          'icon-thin-005',
          async () => {
            const files: DeskiMobileFile[] = [];
            for (const file of context.files) {
              const fileRequest = file.model.source.getLinkData(file.model);
              if (!fileRequest) {
                await showError('FileResult is null.');
                return;
              }

              files.push(new DeskiMobileFile(fileRequest));
            }

            const requestBody = new DeskiMobileInitOperationRequest(files);
            const operationResult = await DeskiMobileHelper.startOperation(requestBody, 'sign');
            if (!operationResult) {
              return;
            }

            const token = await DeskiMobileHelper.handleTrackingDeskiMobile(
              operationResult.link,
              operationResult.token
            );

            if (!token) {
              return;
            }
            // если токен валиден, значит ассистент был запущен, обновляем время успешного запуска.
            DeskiMobileHelper.setTimeSuccessfulStartAssistant();

            const [response, validationResult] = await DeskiMobileHelper.trackingOperation(
              DeskiMobileOperation.sign,
              token
            );

            if (!validationResult.isSuccessful) {
              await showNotEmpty(validationResult);
              return;
            }

            if (!response) {
              await showError('Operation response is null.');
              return;
            }

            // операция отменена
            if (response instanceof DeskiMobileCancelOperationResponse) {
              return;
            }

            if (response instanceof DeskiMobileVerifyOperationResponse) {
              await showError('Operation response has operation verify result.');
              return;
            }

            for (const file of files) {
              const result = response.info.get(file.cacheID);
              if (!result) {
                await showError(`Can't find response for file with cacheID=${file.cacheID}.`);
                return;
              }

              const currentFile = context.files.find(e => e.model.id === file.fileID);
              if (!currentFile) {
                await showError(`Can't find file in context with fileID=${file.fileID}.`);
                return;
              }

              await control.fileContainer.addSignature(
                currentFile.model.lastVersion,
                result.certData,
                result.signedData,
                ''
              );
            }
          },
          null,
          signDSCollapsed
        )
      );
    }

    const checkIndex = context.actions.findIndex(x => x.name === 'CheckEDS');
    if (checkIndex >= 0) {
      const fileVM = context.file;
      const file = fileVM.model;
      const hasSignatures = file.lastVersion.signatures.length > 0;
      const canUseSignatures = file.permissions.canUseSignatures;
      const singleMode = context.files.length === 1;
      const hasValidState =
        file.lastVersion.state === FileVersionState.Success ||
        file.lastVersion.state === FileVersionState.Created;
      const checkDSCollapsed = !(singleMode && canUseSignatures && hasSignatures && hasValidState);
      context.actions.splice(
        checkIndex,
        1,
        new MenuAction(
          'CheckEDS',
          '$UI_Controls_FilesControl_CheckDS',
          'icon-thin-091',
          async () => {
            if (
              file.lastVersion.state === FileVersionState.Created ||
              file.lastVersion.signaturesAdded.length > 0
            ) {
              // 1. файл добавлен и не сохранен
              // 2. после подписи файл не был сохранен
              await show({
                text: '$UI_Cards_CannotPerformEDSOperation',
                buttons: MessageBoxButtons.OK
              });
              return;
            }

            await this._signatureVerifier.verify({
              file,
              fileContainer: control.fileContainer
            });
          },
          null,
          checkDSCollapsed
        )
      );
    }
  }
}
