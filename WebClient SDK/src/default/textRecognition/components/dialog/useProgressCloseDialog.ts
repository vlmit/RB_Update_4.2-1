import { useEffect } from 'react';
import { OcrProgressDialogOptions } from './ocrProgressDialogOptions';
import { OcrProgressDialogProps } from './ocrProgressDialog';

export const useProgressCloseDialog = (props: OcrProgressDialogProps): void => {
  const { onClose } = props;
  const { progress } = props.viewModel;

  useEffect(() => {
    if (progress === 100) {
      onClose(OcrProgressDialogOptions.Completed);
    }
  }, [progress, onClose]);
};
