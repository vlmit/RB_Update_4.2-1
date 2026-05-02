import { Button } from 'ui/button/button';
import { Dialog, DialogContainer, DialogContent, DialogFooter } from 'ui';
import { getTessaIcon } from 'common';
import { localize } from 'tessa/localization';
import { observer } from 'mobx-react-lite';
import { OcrProgressDialogOptions } from './ocrProgressDialogOptions';
import { OcrProgressDialogViewModel } from './ocrProgressDialogViewModel';
import { useProgressCloseDialog } from './useProgressCloseDialog';
import './ocrProgressDialogStyle.scss';

/** Пропсы диалога прогресса OCR. */
export interface OcrProgressDialogProps {
  /** Модель представления диалога прогресса OCR. */
  viewModel: OcrProgressDialogViewModel;
  /** Действие, выполняемое при закрытии диалога прогресса OCR.*/
  onClose: (option: OcrProgressDialogOptions) => void;
}

const handleCloseFormWithContinueInBackground =
  (onClose: (option: OcrProgressDialogOptions) => void) => () => {
    onClose(OcrProgressDialogOptions.ContinueInBackground);
  };

const handleCloseFormWithCancel = (onClose: (option: OcrProgressDialogOptions) => void) => () => {
  onClose(OcrProgressDialogOptions.Cancel);
};

/** Диалог отслеживания прогресса операции. */
export const OcrProgressDialog = observer<OcrProgressDialogProps>(
  function OcrProgressDialog(props) {
    useProgressCloseDialog(props);

    return (
      <Dialog
        isOpened={true}
        noPortal={true}
        autoSizeWidth={true}
        autoSizeHeight={true}
        className="progress-dialog"
      >
        <DialogContainer>
          <DialogContent className="progress-dialog-content">
            {localize('$UI_Common_Splash_TextRecognitionProgress', props.viewModel.progress)}
          </DialogContent>
          <DialogFooter className="default-footer">
            <Button
              key="continueInBackground"
              icon="m-solved"
              onClick={handleCloseFormWithContinueInBackground(props.onClose)}
              caption={localize('$UI_Common_ContinueInBackground')}
              captionPosition="after"
              theme="primary"
              type="normal"
            />
            <Button
              key="cancel"
              icon={getTessaIcon('Thin253')}
              isEnabled={props.viewModel.canCancel}
              onClick={handleCloseFormWithCancel(props.onClose)}
              caption={localize('$UI_Common_Cancel')}
              captionPosition="after"
              theme="secondary"
              type="normal"
            />
          </DialogFooter>
        </DialogContainer>
      </Dialog>
    );
  }
);
