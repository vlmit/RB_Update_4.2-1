import { ICardModel } from 'tessa/ui/cards';
import { ISignFilesProvider } from './signFilesTypes';
import { TaskViewModel } from 'tessa/ui/cards/tasks';
import {
  checkSigns,
  FileViewModel,
  isCanceledError,
  SelectFileCertResult,
  selectFileCerts,
  setSign,
  usingCachedKey
} from 'tessa/ui/cards/controls';
import { LoadingOverlay, showError, showNotEmpty } from 'tessa/ui';
import { TableFileRowViewModel } from '../../cardFiles/tableFileRowViewModel';
import { ThemeManager } from 'tessa/ui/themes';
import { ICardSingletonCache, ICardSingletonCache$ } from '@tessa/platform';
import { inject, injectable } from '@tessa/application';
import { CertificateData, createSignature, FileSignatureState } from 'tessa/files';
import { ValidationResult } from '@tessa/core';
import { SignFileHelper } from './signFileHelper';

@injectable()
export class SignFilesProviderWeb implements ISignFilesProvider {
  //#region constructors
  constructor(
    @inject(ICardSingletonCache$) private readonly _cardSingletonCache: ICardSingletonCache
  ) {}

  //#endregion

  //#region public methods

  async signFilesAction(cardModel: ICardModel, taskViewModel: TaskViewModel): Promise<boolean> {
    if (!(await cardModel.edsProvider.isAvailable(false))) {
      await showError('$KrProcess_ErrorMessage_CryptoProPluginIsDisabled');
      return false;
    }

    return await SignFileHelper.handleSignFiles(
      cardModel,
      taskViewModel,
      async (shownFilesVMs, selectedFilesVMs, cardModel, noCommentDialog, noSignFileDialog) => {
        // Если не требуется показывать диалог — подписываем выбранные или все показанные файлы
        if (noSignFileDialog) {
          return await this.trySignFiles(
            selectedFilesVMs ?? shownFilesVMs,
            cardModel,
            noCommentDialog
          );
        }

        if (shownFilesVMs.length <= 1) {
          return await this.trySignFiles(shownFilesVMs, cardModel, noCommentDialog);
        }

        return await SignFileHelper.showSignFilesDialog(
          shownFilesVMs,
          selectedFilesVMs,
          cardModel,
          async (selectedRows, cardModel) => {
            return await this.webSignDialogButtonAction(selectedRows, cardModel, noCommentDialog);
          }
        );
      }
    );
  }

  //#endregion

  //#region private methods

  private async webSignDialogButtonAction(
    selectedRows: TableFileRowViewModel[],
    mainCardModel: ICardModel,
    noCommentDialog: boolean
  ): Promise<boolean> {
    const certResult = await this.getCert(mainCardModel, noCommentDialog);
    if (!certResult || certResult.cancel || !certResult.cert) {
      return false;
    }

    const saveSignatureWithTemporaryErrors = await this.shouldSaveSignatureWithTemporaryErrors();
    const theme = ThemeManager.instance.currentTheme.settings.files;
    return await LoadingOverlay.instance.show(async () => {
      return await usingCachedKey(mainCardModel.edsProvider, certResult.cert!, async () => {
        for (const row of selectedRows) {
          let background = theme.fileBusyBackground;
          row.style = Object.assign({}, row.style, {
            background
          });
          const isSuccessful = await this.innerSignFile(
            row.fileViewModel,
            mainCardModel,
            certResult.cert!,
            certResult.comment ?? '',
            saveSignatureWithTemporaryErrors
          );
          if (!isSuccessful) {
            return false;
          }

          background = theme.fileSignatureChecked;
          row.style = Object.assign({}, row.style, {
            background
          });
        }
        return true;
      });
    });
  }

  private async innerSignFile(
    fileVm: FileViewModel,
    cardModel: ICardModel,
    cert: CertificateData,
    certComment: string,
    saveSignatureWithTemporaryErrors: boolean
  ): Promise<boolean> {
    try {
      fileVm.isLoading = true;
      const version = fileVm.model.lastVersion;

      let validationResult = await version.ensureContentDownloaded();
      if (!validationResult.isSuccessful) {
        await showNotEmpty(validationResult);
        return false;
      }
      validationResult = await version.ensureSignaturesLoaded();
      if (!validationResult.isSuccessful) {
        await showNotEmpty(validationResult);
        return false;
      }

      const signedData = await setSign(cardModel.edsProvider, version.content!, cert, version.id);
      const addedSignature = createSignature(version, cert, signedData, certComment);

      const newSignsArray = [...version.signatures, addedSignature];

      if (signedData.signatureValidation) {
        await checkSigns(
          cardModel.edsProvider,
          version.content!,
          newSignsArray,
          {
            signature: addedSignature,
            signatureValidation: signedData.signatureValidation
          },
          signedData.fileHash
        );
      }

      if (
        addedSignature.state === FileSignatureState.Checked ||
        (saveSignatureWithTemporaryErrors &&
          addedSignature.state === FileSignatureState.CheckedWithWarning)
      ) {
        await cardModel.fileContainer.addCreatedSignature(version, addedSignature);
      } else {
        await showError(
          addedSignature.state === FileSignatureState.CheckedWithWarning
            ? '$UI_Controls_FilesControl_WarningSignaturesVerify'
            : '$UI_Controls_FilesControl_ErrorsSignaturesVerify'
        );
        return false;
      }

      return true;
    } catch (err) {
      if (!isCanceledError(err)) {
        await showNotEmpty(ValidationResult.fromError(err));
      }
      return false;
    } finally {
      fileVm.isLoading = false;
    }
  }

  private async trySignFiles(
    files: FileViewModel[],
    cardModel: ICardModel,
    noCommentDialog: boolean
  ): Promise<boolean> {
    if (files.length === 0) {
      return true;
    }

    const certResult = await this.getCert(cardModel, noCommentDialog);
    if (!certResult) {
      return false;
    }

    if (certResult.cancel || !certResult.cert) {
      return false;
    }

    const saveSignatureWithTemporaryErrors = await this.shouldSaveSignatureWithTemporaryErrors();
    return await LoadingOverlay.instance.show(async () => {
      return await usingCachedKey(cardModel.edsProvider, certResult.cert!, async () => {
        for (const file of files) {
          const isSuccessful = await this.innerSignFile(
            file,
            cardModel,
            certResult.cert!,
            certResult.comment ?? '',
            saveSignatureWithTemporaryErrors
          );
          if (!isSuccessful) {
            return false;
          }
        }
        return true;
      });
    });
  }
  private async getCert(
    cardModel: ICardModel,
    noCommentDialog: boolean
  ): Promise<SelectFileCertResult | null> {
    const isAvailable = await cardModel.edsProvider.isAvailable(true);
    if (!isAvailable) {
      return null;
    }
    return await selectFileCerts(cardModel.edsProvider, noCommentDialog, false);
  }

  private async shouldSaveSignatureWithTemporaryErrors(): Promise<boolean> {
    const signatureSettings = await this._cardSingletonCache.getCard('SignatureSettings');
    return (
      signatureSettings?.sections
        .tryGet('SignatureSettings')
        ?.fields.tryGetBoolean('SaveSignatureWithTemporaryErrors') ?? false
    );
  }

  //#endregion
}
