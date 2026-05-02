import React from 'react';
import { Dialog, DialogContainer, DialogContent, DialogFooter } from 'ui';
import { Button } from 'ui/button/button';
import { SessionExpireDialogResultVariant } from './sessionExpireEnums';

// #region SessionExpireDialogViewModel

export class SessionExpireDialogViewModel {
  constructor(message: string) {
    this._message = message;
  }

  private _message: string;

  public get message(): string {
    return this._message;
  }
}

// #endregion

// #region SessionExpireDialog

interface SessionExpireDialogProps {
  viewModel: SessionExpireDialogViewModel;
  onClose: (variant: SessionExpireDialogResultVariant) => void;
}

export const SessionExpireDialog = ({
  viewModel,
  onClose
}: SessionExpireDialogProps): React.ReactElement => {
  const handleCloseDialog = (variant: SessionExpireDialogResultVariant) => () => {
    onClose(variant);
  };

  const renderButtons = () => {
    return (
      <>
        <Button
          caption="$UI_Common_Yes"
          onClick={handleCloseDialog(SessionExpireDialogResultVariant.Ok)}
          type="normal"
          theme="primary"
        />

        <Button
          caption="$UI_Common_No"
          onClick={handleCloseDialog(SessionExpireDialogResultVariant.Cancel)}
          type="normal"
          theme="secondary"
        />

        <Button
          caption="$UI_Misc_No_NoNotify"
          onClick={handleCloseDialog(SessionExpireDialogResultVariant.CancelNoNotify)}
          type="normal"
          theme="secondary"
        />
      </>
    );
  };

  return (
    <Dialog
      isOpened
      autoSizeWidth={true}
      autoSizeHeight={true}
      showChrome={true}
      onCloseRequest={handleCloseDialog(SessionExpireDialogResultVariant.Cancel)}
    >
      <DialogContainer>
        <DialogContent>{viewModel.message}</DialogContent>
        <DialogFooter className="default-footer">{renderButtons()}</DialogFooter>
      </DialogContainer>
    </Dialog>
  );
};

// #endregion
