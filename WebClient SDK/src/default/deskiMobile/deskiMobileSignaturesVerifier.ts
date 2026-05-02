import { injectable } from '@tessa/application';
import { ICardModel } from 'tessa/ui/cards';
import { showFileSigns } from 'tessa/ui/cards/controls';
import { UIContext, showError, showNotEmpty } from 'tessa/ui';
import { FileContainer, FileSignatureState, IFileVersion } from 'tessa/files';
import { SignatureValidationInfo } from 'tessa/cards/eds/signatureValidationInfo';
import Platform from 'common/platform';
import { DeskiMobileFile } from './deskiMobileFile';
import { DeskiMobileHelper } from './deskiMobileHelper';
import { DeskiMobileOperation, DeskiMobileService } from './deskiMobileService';
import { DeskiMobileInitOperationRequest } from './deskiMobileInitOperationRequest';
import { DeskiMobileCancelOperationResponse } from './deskiMobileCancelOperationResponse';
import { DeskiMobileVerifyOperationResponse } from './deskiMobileVerifyOperationResponse';
import { DeskiMobileEnhanceOperationResponse } from './deskiMobileEnhanceOperationResponse';
import { ISignaturesVerifier, SignaturesVerifyParams } from 'tessa/cards/eds/edsTypes';

@injectable()
export class DeskiMobileSignaturesVerifier implements ISignaturesVerifier {
  //#region ApplicationExtension

  async verify(props: SignaturesVerifyParams): Promise<void> {
    if (!DeskiMobileService.instance.deskiMobileEnabled || !Platform.isMobile()) {
      return;
    }

    const fileRequest = props.file.source.getLinkData(props.file);
    if (!fileRequest) {
      await showError('FileResult is null.');
      return;
    }

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
