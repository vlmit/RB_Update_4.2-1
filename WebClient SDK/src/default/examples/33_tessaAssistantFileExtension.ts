import { extension } from '@tessa/application';
import { FileContainer, FileSignatureState, FileVersionState, IFileVersion } from 'tessa/files';
import Platform from 'common/platform';
import { ICardModel } from 'tessa/ui/cards';
import { MenuAction, UIContext } from 'tessa/ui';
import { showFileSigns } from 'tessa/ui/cards/controls';
import { FileExtension, FileExtensionContext } from 'tessa/ui/files';
import { MessageBoxButtons, show, showError, showNotEmpty } from 'tessa/ui/tessaDialog';
import { SignatureValidationInfo } from 'tessa/cards/eds/signatureValidationInfo';
import { DeskiMobileOperation, DeskiMobileService } from '../deskiMobile/deskiMobileService';
import { DeskiMobileHelper } from '../deskiMobile/deskiMobileHelper';
import { DeskiMobileFile } from '../deskiMobile/deskiMobileFile';
import { DeskiMobileInitOperationRequest } from '../deskiMobile/deskiMobileInitOperationRequest';
import { DeskiMobileCancelOperationResponse } from '../deskiMobile/deskiMobileCancelOperationResponse';
import { DeskiMobileVerifyOperationResponse } from '../deskiMobile/deskiMobileVerifyOperationResponse';
import { DeskiMobileEnhanceOperationResponse } from '../deskiMobile/deskiMobileEnhanceOperationResponse';
import { SignaturesVerifyParams } from 'tessa/cards/eds/edsTypes';

/**
 * Пример переопределения поведения кнопок "Подписать" и "Проверить ЭП" в контекстном меню
 * файлового контрола для мобильных браузеров для возможности работы с мобильным приложением TESSA Assistant.
 */
@extension()
export class TessaAssistantFileExtension extends FileExtension {
  public openingMenu(context: FileExtensionContext): void {
    if (!DeskiMobileService.instance.deskiMobileEnabled || !Platform.isMobile()) {
      return;
    }

    const control = context.control;
    const fileVM = context.file;
    const file = fileVM.model;
    const singleMode = context.files.length === 1;
    const canUseSignatures = file.permissions.canUseSignatures;
    const hasSignatures = file.lastVersion.signatures.length > 0;
    const hasValidState =
      file.lastVersion.state === FileVersionState.Success ||
      file.lastVersion.state === FileVersionState.Created;

    const signIndex = context.actions.findIndex(x => x.name === 'SignEDS');
    if (signIndex >= 0) {
      const signDSCollapsed = !(
        singleMode &&
        canUseSignatures &&
        file.permissions.canSign &&
        hasValidState
      );
      context.actions.splice(
        signIndex,
        1,
        new MenuAction(
          'SignEDS',
          '$UI_Controls_FilesControl_SignDS',
          'icon-thin-005',
          async () => {
            // Для выполнения операция файл должен присуствовать на сервере.
            if (file.lastVersion.state === FileVersionState.Created) {
              await show({
                text: '$UI_Cards_CannotPerformEDSOperation',
                buttons: MessageBoxButtons.OK
              });
              return;
            }

            const fileRequest = file.source.getLinkData(file);
            if (!fileRequest) {
              await showError('FileResult is null.');
              return;
            }

            // Генерация ссылки на сервере для передачи в мобильное приложение TESSA Assistant.
            const dmFile = new DeskiMobileFile(fileRequest);
            const requestBody = new DeskiMobileInitOperationRequest([dmFile]);
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

            // Мобильное приложение запустилось. Ожидаем завершения операции подписания переданного файла.
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

            const result = response.info.get(dmFile.cacheID);
            if (!result) {
              await showError(`Can't find response for file with cacheID=${dmFile.cacheID}.`);
              return;
            }

            // Добавление подписи к файлу.
            await control.fileContainer.addSignature(
              file.lastVersion,
              result.certData,
              result.signedData,
              ''
            );
          },
          null,
          signDSCollapsed
        )
      );
    }

    const checkIndex = context.actions.findIndex(x => x.name === 'CheckEDS');
    if (checkIndex >= 0) {
      const checkDSCollapsed = !(singleMode && canUseSignatures && hasSignatures && hasValidState);
      context.actions.splice(
        checkIndex,
        1,
        new MenuAction(
          'CheckEDS',
          '$UI_Controls_FilesControl_CheckDS',
          'icon-thin-091',
          async () => {
            // Для выполнения операция файл должен присуствовать на сервере.
            if (
              file.lastVersion.state === FileVersionState.Created ||
              file.lastVersion.signaturesAdded.length > 0
            ) {
              await show({
                text: '$UI_Cards_CannotPerformEDSOperation',
                buttons: MessageBoxButtons.OK
              });
              return;
            }

            await this.verifyInDeskiMobile({
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

  //#region private methods

  private async verifyInDeskiMobile(props: SignaturesVerifyParams): Promise<void> {
    const fileRequest = props.file.source.getLinkData(props.file);
    if (!fileRequest) {
      await showError('FileResult is null.');
      return;
    }

    // Генерация ссылки на сервере для передачи в мобильное приложение TESSA Assistant.
    const dmFile = new DeskiMobileFile(fileRequest);
    const requestBody = new DeskiMobileInitOperationRequest([dmFile]);
    const operationResult = await DeskiMobileHelper.startOperation(requestBody, 'verify');
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

    // Мобильное приложение запустилось. Ожидаем завершения операции проверки подписи переданного файла.
    const [response, validationResult] = await DeskiMobileHelper.trackingOperation(
      DeskiMobileOperation.verify,
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

    if (response instanceof DeskiMobileEnhanceOperationResponse) {
      await showError('Operation response has operation sign result.');
      return;
    }

    const cardModel = UIContext.current.cardEditor?.cardModel;

    if (props.onClose) {
      props.onClose();
    }

    await this.getActualVerifyOperationResult(
      cardModel,
      props.fileContainer,
      props.file.lastVersion,
      dmFile.cacheID,
      response
    );
  }

  private async getActualVerifyOperationResult(
    cardModel: ICardModel | null | undefined,
    fileContainer: FileContainer,
    lastVersion: IFileVersion,
    cacheID: string,
    response: DeskiMobileVerifyOperationResponse
  ): Promise<void> {
    try {
      const result = response.info.get(cacheID);
      if (!result) {
        await showError(`Can't find response for file with cacheID=${cacheID}.`);
        return;
      }

      for (const signRow of lastVersion.signatures) {
        let state: FileSignatureState = FileSignatureState.Checked;
        let error = '';

        const item = result.signatureValidationInfo.find(e => e.id === signRow.id);
        if (!item) {
          await showError('SignatureInfo in server not contain SignatureInfo from web client.');
          return;
        }

        const validationInfos = item.validationInfo.map(vi => new SignatureValidationInfo(vi));
        if (validationInfos.some(x => x.state === FileSignatureState.Failed)) {
          state = FileSignatureState.Failed;
          const firstInfo = validationInfos.find(x => x.state === FileSignatureState.Failed)!;
          error = firstInfo.signingCertificateValidityDesc;
        } else if (validationInfos.some(x => x.state !== FileSignatureState.Checked)) {
          state = FileSignatureState.CheckedWithWarning;
        }

        signRow.updateState(state, validationInfos, error);
      }

      if (result.signatureValidationData.showValidationDialog && cardModel) {
        await showFileSigns(fileContainer, cardModel.edsProvider, lastVersion);
      }
    } catch (err) {
      await showError(err);
    }
  }

  // #endregion
}
